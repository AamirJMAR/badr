using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.Services;
using System.Globalization;
using System.Text.Json;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PmoAnalysisAiService _pmoAi;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(ApplicationDbContext context, PmoAnalysisAiService pmoAi, ILogger<CalendarController> logger)
        {
            _context = context;
            _pmoAi = pmoAi;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CalendarEvent evt)
        {
            if (evt == null || string.IsNullOrWhiteSpace(evt.Subject))
                return BadRequest("Subject is required");

            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrWhiteSpace(evt.StartTimeRaw))
            {
                start = DateTime.ParseExact(
                    evt.StartTimeRaw,
                    "dd/MM/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture
                );
            }

            if (!string.IsNullOrWhiteSpace(evt.EndTimeRaw))
            {
                end = DateTime.ParseExact(
                    evt.EndTimeRaw,
                    "dd/MM/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture
                );
            }

            var duplicates = _context.CalendarEvents
                .Where(e =>
                    e.Subject == evt.Subject &&
                    e.StartTime == start
                )
                .ToList();

            if (duplicates.Any())
            {
                _context.CalendarEvents.RemoveRange(duplicates);
                await _context.SaveChangesAsync();
            }

            var entity = new CalendarEvent
            {
                Subject = evt.Subject,
                StartTimeRaw = evt.StartTimeRaw,
                EndTimeRaw = evt.EndTimeRaw,
                StartTime = start,
                EndTime = end,
                Organizer = evt.Organizer,
                RequiredAttendees = evt.RequiredAttendees,
                OptionalAttendees = evt.OptionalAttendees
            };

            _context.CalendarEvents.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var now = DateTime.Now;

            var upcomingEvents = _context.CalendarEvents
                .Where(e => e.StartTime != null && e.StartTime >= now)
                .OrderBy(e => e.StartTime)
                .ToList();

            return Ok(upcomingEvents);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var ev = await _context.CalendarEvents.FindAsync(id);

            if (ev == null)
                return NotFound();

            return Ok(ev);
        }

        [HttpPost("analyze-preview/{id}")]
        public async Task<IActionResult> AnalyzePreview(int id)
        {
            var ev = await _context.CalendarEvents.FindAsync(id);
            if (ev == null)
                return NotFound("Event not found");

            try
            {
                var preview = await AnalyzeEventCoreAsync(ev);
                return Ok(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze calendar event {EventId}", id);
                return StatusCode(500, new { Error = "Failed to analyze calendar event", Details = ex.Message });
            }
        }

        /// <summary>Déprécié : ne persiste plus en base. Utiliser analyze-preview puis create-project-tasks.</summary>
        [HttpPost("analyze/{id}")]
        public Task<IActionResult> Analyze(int id) => AnalyzePreview(id);

        [HttpPost("analyze-bulk")]
        public async Task<IActionResult> AnalyzeBulk([FromBody] BulkIdsRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest("No ids provided");

            var results = new List<BulkAnalyzeItemDto>();

            foreach (var id in request.Ids.Distinct())
            {
                var ev = await _context.CalendarEvents.FindAsync(id);
                if (ev == null)
                {
                    results.Add(new BulkAnalyzeItemDto { Id = id, Label = $"#{id}", Error = "Event not found" });
                    continue;
                }

                try
                {
                    var preview = await AnalyzeEventCoreAsync(ev);
                    results.Add(new BulkAnalyzeItemDto
                    {
                        Id = id,
                        Label = ev.Subject,
                        Analysis = preview
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to analyze calendar event {EventId} in bulk", id);
                    results.Add(new BulkAnalyzeItemDto
                    {
                        Id = id,
                        Label = ev.Subject,
                        Error = ex.Message
                    });
                }
            }

            return Ok(results);
        }

        [HttpPost("check-project")]
        public IActionResult CheckProject([FromBody] string projectName)
        {
            var exists = _context.Projects.Any(p => p.Name == projectName);
            return Ok(new { Exists = exists });
        }

        [HttpPost("create-project-tasks/{id}")]
        public async Task<IActionResult> CreateProjectTasks(int id, [FromBody] CreateProjectTasksRequest request)
        {
            var ev = await _context.CalendarEvents.FindAsync(id);
            if (ev == null)
                return NotFound("Event not found");

            var projectName = string.IsNullOrWhiteSpace(request.ProjectName)
                ? ev.Subject
                : request.ProjectName;

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Name == projectName);
            if (project == null)
            {
                project = new Project
                {
                    Name = projectName,
                    Deadline = request.Tasks.Any() ? request.Tasks.Min(t => t.Deadline) : DateTime.Today,
                    Status = string.IsNullOrWhiteSpace(request.Status) ? "Open" : request.Status
                };
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
            }
            else if (!string.IsNullOrWhiteSpace(request.Status) && project.Status != request.Status)
            {
                project.Status = request.Status;
                _context.Projects.Update(project);
                await _context.SaveChangesAsync();
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
                Deadline = t.Deadline.ToString("yyyy-MM-dd")
            }).ToList();

            ev.AiProjectName = projectName;
            ev.AiClient = request.Client;
            ev.AiStatus = request.Status;
            ev.AiCategory = request.Category;
            ev.AiSummary = request.Summary;
            ev.AiRecommendations = JsonSerializer.Serialize(recommendationTasks);
            ev.AiAnalyzed = true;

            _context.CalendarEvents.Update(ev);
            await _context.SaveChangesAsync();

            return Ok(new CreateResult { ProjectId = project.Id });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.CalendarEvents.FindAsync(id);

            if (ev == null)
                return NotFound();

            _context.CalendarEvents.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("delete-multiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("No ids provided");

            var events = _context.CalendarEvents.Where(e => ids.Contains(e.Id)).ToList();

            if (!events.Any())
                return NotFound("No events found");

            _context.CalendarEvents.RemoveRange(events);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<PmoAnalysisPreviewDto> AnalyzeEventCoreAsync(CalendarEvent ev)
        {
            _logger.LogInformation("Analyzing calendar event {EventId} with AI (preview)", ev.Id);

            var content = BuildEventContent(ev);
            var parsed = await _pmoAi.AnalyzeAsync("calendar event", content);
            return MapToPreviewDto(parsed);
        }

        private static string BuildEventContent(CalendarEvent ev) =>
            string.Join("\n", new[]
            {
                $"Subject: {ev.Subject}",
                $"Organizer: {ev.Organizer ?? "N/A"}",
                $"StartTime: {ev.StartTime?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}",
                $"EndTime: {ev.EndTime?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}",
                $"RequiredAttendees: {ev.RequiredAttendees ?? "N/A"}",
                $"OptionalAttendees: {ev.OptionalAttendees ?? "N/A"}"
            });

        private static PmoAnalysisPreviewDto MapToPreviewDto(PmoAnalysisResult parsed) =>
            new()
            {
                ProjectName = parsed.ProjectName,
                Client = parsed.Client,
                Status = parsed.Status,
                Category = parsed.Category,
                Summary = parsed.Summary,
                Tasks = parsed.Tasks.Select(t => new PmoAnalysisTaskDto
                {
                    Title = t.Title,
                    Deadline = t.Deadline
                }).ToList()
            };

        public class CreateProjectTasksRequest
        {
            public string ProjectName { get; set; } = string.Empty;
            public string Client { get; set; } = string.Empty;
            public string Status { get; set; } = "OnTrack";
            public string Category { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
            public List<CalendarTaskDto> Tasks { get; set; } = new();
        }

        public class CalendarTaskDto
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
