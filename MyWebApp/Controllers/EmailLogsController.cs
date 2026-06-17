using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Constants;
using MyWebApp.Data;
using MyWebApp.Helpers;
using MyWebApp.Models;
using MyWebApp.Services;
using System.Text.Json;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/emaillogs")]
    public class EmailLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PmoAnalysisAiService _pmoAi;
        private readonly CategorySeedService _categorySeed;
        private readonly ILogger<EmailLogsController> _logger;

        public EmailLogsController(
            ApplicationDbContext context,
            PmoAnalysisAiService pmoAi,
            CategorySeedService categorySeed,
            ILogger<EmailLogsController> logger)
        {
            _context = context;
            _pmoAi = pmoAi;
            _categorySeed = categorySeed;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmailLog email)
        {
            if (email == null || string.IsNullOrWhiteSpace(email.Subject))
                return BadRequest("Subject is required");

            var receivedDate = email.ReceivedDate == default
                ? DateTime.UtcNow
                : email.ReceivedDate;

            bool exists = _context.EmailLogs.Any(e =>
                e.Subject == email.Subject &&
                e.ReceivedDate == receivedDate &&
                e.Body == email.Body
            );

            if (exists)
            {
                return Ok(new
                {
                    Status = "Duplicate",
                    Message = "Email already exists"
                });
            }

            var entity = new EmailLog
            {
                Subject = email.Subject,
                From = email.From,
                Body = email.Body,
                ReceivedDate = receivedDate
            };

            _context.EmailLogs.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.EmailLogs.ToList());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var email = await _context.EmailLogs.FindAsync(id);

            if (email == null)
                return NotFound();

            _context.EmailLogs.Remove(email);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var email = await _context.EmailLogs.FindAsync(id);

            if (email == null)
                return NotFound();

            return Ok(email);
        }

        /// <summary>Prévisualisation IA sans persistance (préféré par le frontend).</summary>
        [HttpPost("analyze-preview/{id}")]
        public async Task<IActionResult> AnalyzePreview(int id)
        {
            var email = await _context.EmailLogs.FindAsync(id);

            if (email == null)
                return NotFound("Email not found");

            if (string.IsNullOrWhiteSpace(email.Subject) || string.IsNullOrWhiteSpace(email.Body))
                return BadRequest("Email Subject or Body is empty");

            try
            {
                var preview = await AnalyzeEmailCoreAsync(email);
                return Ok(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze email {EmailId}", id);
                return StatusCode(500, new { Error = "Failed to analyze email", Details = ex.Message });
            }
        }

        /// <summary>Déprécié : ne persiste plus en base. Utiliser analyze-preview puis create-project-tasks.</summary>
        [HttpPost("analyze/{id}")]
        public Task<IActionResult> AnalyzeEmail(int id) => AnalyzePreview(id);

        [HttpPost("analyze-bulk")]
        public async Task<IActionResult> AnalyzeBulk([FromBody] BulkIdsRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest("No ids provided");

            var results = new List<BulkAnalyzeItemDto>();

            foreach (var id in request.Ids.Distinct())
            {
                var email = await _context.EmailLogs.FindAsync(id);
                if (email == null)
                {
                    results.Add(new BulkAnalyzeItemDto { Id = id, Label = $"#{id}", Error = "Email not found" });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email.Subject) || string.IsNullOrWhiteSpace(email.Body))
                {
                    results.Add(new BulkAnalyzeItemDto { Id = id, Label = email.Subject ?? $"#{id}", Error = "Subject or Body is empty" });
                    continue;
                }

                try
                {
                    var preview = await AnalyzeEmailCoreAsync(email);
                    results.Add(new BulkAnalyzeItemDto
                    {
                        Id = id,
                        Label = email.Subject,
                        Analysis = preview
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to analyze email {EmailId} in bulk", id);
                    results.Add(new BulkAnalyzeItemDto
                    {
                        Id = id,
                        Label = email.Subject,
                        Error = ex.Message
                    });
                }
            }

            return Ok(results);
        }

        [HttpPost("check-project")]
        public IActionResult CheckProject([FromBody] string categoryName)
        {
            var name = CategoryConstants.ResolveName(categoryName);
            var exists = _context.Projects.Any(p => p.Name == name);
            return Ok(new { Exists = exists });
        }

        [HttpPost("create-project-tasks/{id}")]
        public async Task<IActionResult> CreateProjectTasks(int id, [FromBody] CreateProjectTasksRequest request)
        {
            var email = await _context.EmailLogs.FindAsync(id);
            if (email == null)
                return NotFound("Email not found");

            var categoryName = CategoryConstants.ResolveName(
                !string.IsNullOrWhiteSpace(request.CategoryName)
                    ? request.CategoryName
                    : request.ProjectName);

            if (!CategoryConstants.IsValid(categoryName))
                return BadRequest($"Category must be one of: {string.Join(", ", CategoryConstants.FixedCategories)}");

            var project = await _categorySeed.ResolveCategoryAsync(categoryName);

            if (!string.IsNullOrWhiteSpace(request.Client))
                project.Client = request.Client;

            if (!string.IsNullOrWhiteSpace(request.Status) && project.Status != request.Status)
            {
                project.Status = request.Status;
                _context.Projects.Update(project);
            }

            foreach (var task in request.Tasks)
            {
                _context.Tasks.Add(new TaskItem
                {
                    Title = task.Title,
                    Deadline = task.Deadline,
                    Status = "Open",
                    ProjectId = project.Id
                });
            }

            var recommendationTasks = request.Tasks.Select(t => new PmoAnalysisTaskDto
            {
                Title = t.Title,
                Deadline = t.Deadline
            }).ToList();

            email.AiCategory = request.Category;
            email.AiSummary = request.Summary;
            email.AiRecommendations = JsonSerializer.Serialize(recommendationTasks);
            email.AiAnalyzed = true;

            _context.EmailLogs.Update(email);
            await _context.SaveChangesAsync();

            return Ok(new CreateResult { ProjectId = project.Id });
        }

        [HttpPost("delete-multiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("No ids provided");

            var emails = _context.EmailLogs.Where(e => ids.Contains(e.Id)).ToList();

            if (!emails.Any())
                return NotFound("No emails found");

            _context.EmailLogs.RemoveRange(emails);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<PmoAnalysisPreviewDto> AnalyzeEmailCoreAsync(EmailLog email)
        {
            _logger.LogInformation("Analyzing email {EmailId} with AI (preview)", email.Id);

            var content = BuildEmailContent(email);
            var parsed = await _pmoAi.AnalyzeAsync("email", content);
            return MapToPreviewDto(parsed);
        }

        private static string BuildEmailContent(EmailLog email) =>
            $"From: {email.From}\nSubject: {email.Subject}\n\nBody: {email.Body}";

        private static PmoAnalysisPreviewDto MapToPreviewDto(PmoAnalysisResult parsed) =>
            new()
            {
                ProjectName = parsed.ProjectName,
                CategoryName = CategoryConstants.ResolveName(parsed.ProjectName),
                Client = parsed.Client,
                Status = parsed.Status,
                Category = parsed.Category,
                Summary = parsed.Summary,
                Tasks = parsed.Tasks.Select(t => new PmoAnalysisTaskDto
                {
                    Title = t.Title,
                    Deadline = DeadlineHelper.ParseDeadline(t.Deadline)
                }).ToList()
            };

        public class CreateProjectTasksRequest
        {
            public string ProjectName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public string Client { get; set; } = string.Empty;
            public string Status { get; set; } = "OnTrack";
            public string Category { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
            public List<EmailTaskDto> Tasks { get; set; } = new();
        }

        public class EmailTaskDto
        {
            public string Title { get; set; } = string.Empty;
            public DateTime Deadline { get; set; }
        }

        private class CreateResult
        {
            public int ProjectId { get; set; }
        }
    }
}
