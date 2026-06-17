using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using MyWebApp.Constants;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/deliverables")]
    public class DeliverablesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly CategorySeedService _categorySeed;

        public DeliverablesController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            CategorySeedService categorySeed)
        {
            _context = context;
            _env = env;
            _categorySeed = categorySeed;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDeliverable(IFormFile file, [FromForm] string? projectName, [FromForm] string? categoryName)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "UploadedFiles");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsFolder, storedFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var categoryToUse = CategoryConstants.ResolveName(
                !string.IsNullOrWhiteSpace(categoryName) ? categoryName : projectName);

            var project = await _categorySeed.ResolveCategoryAsync(categoryToUse);

            var deliverable = new Deliverable
            {
                FileName = file.FileName,
                FilePath = Path.Combine("UploadedFiles", storedFileName),
                UploadDate = DateTime.UtcNow,
                ProjectId = project.Id
            };

            _context.Deliverables.Add(deliverable);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                ProjectId = project.Id,
                DeliverableId = deliverable.Id,
                FileName = deliverable.FileName,
                ProjectName = project.Name,
                CategoryName = project.Name
            });
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "UploadedFiles");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var tempFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsFolder, tempFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await Task.Delay(1500);

            return Ok(new
            {
                ProjectName = CategoryConstants.DefaultCategory,
                CategoryName = CategoryConstants.DefaultCategory,
                Client = "airbus",
                FileName = file.FileName,
                StoredFileName = tempFileName,
                TempFileName = tempFileName,
                Tasks = new[]
                {
                    new { Title = "Prepare PMO report", Deadline = "2026-04-25" },
                    new { Title = "Update risk register", Deadline = "2026-04-30" },
                    new { Title = "Steering committee preparation", Deadline = "2026-05-05" }
                }
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeliverable(int id)
        {
            var deliverable = await _context.Deliverables
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deliverable == null)
                return NotFound();

            var tasks = _context.Tasks.Where(t => t.DeliverableId == id);
            _context.Tasks.RemoveRange(tasks);

            var fullPath = Path.Combine(_env.ContentRootPath, deliverable.FilePath);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.Deliverables.Remove(deliverable);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("check-project")]
        public IActionResult CheckProject([FromBody] string categoryName)
        {
            var exists = CategoryConstants.IsValid(categoryName) &&
                         _context.Projects.Any(p => p.Name == categoryName.Trim());
            return Ok(new { Exists = exists });
        }

        [HttpPost("create-tasks")]
        public async Task<IActionResult> CreateTasks([FromBody] CreateTasksRequest request)
        {
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
                project.Status = request.Status;

            _context.Projects.Update(project);

            var storedFileName = !string.IsNullOrWhiteSpace(request.StoredFileName)
                ? request.StoredFileName
                : request.FilePath;

            if (string.IsNullOrWhiteSpace(storedFileName))
                return BadRequest("Stored file name is required.");

            var deliverable = new Deliverable
            {
                FileName = request.FileName,
                FilePath = Path.Combine("UploadedFiles", storedFileName),
                UploadDate = DateTime.UtcNow,
                ProjectId = project.Id
            };

            _context.Deliverables.Add(deliverable);
            await _context.SaveChangesAsync();

            foreach (var task in request.Tasks)
            {
                _context.Tasks.Add(new TaskItem
                {
                    Title = task.Title,
                    Deadline = task.Deadline,
                    Status = "Open",
                    ProjectId = project.Id,
                    DeliverableId = deliverable.Id
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ProjectId = project.Id,
                DeliverableId = deliverable.Id
            });
        }

        [HttpPost("{deliverableId}/add-tasks")]
        public async Task<IActionResult> AddTasksToDeliverable(int deliverableId, [FromBody] AddTasksRequest request)
        {
            var deliverable = await _context.Deliverables
                .Include(d => d.Project)
                .FirstOrDefaultAsync(d => d.Id == deliverableId);

            if (deliverable == null)
                return NotFound("Deliverable not found");

            var categoryName = CategoryConstants.ResolveName(request.CategoryName);
            if (!CategoryConstants.IsValid(categoryName))
                return BadRequest($"Category must be one of: {string.Join(", ", CategoryConstants.FixedCategories)}");

            var project = await _categorySeed.ResolveCategoryAsync(categoryName);

            if (!string.IsNullOrWhiteSpace(request.Client))
                project.Client = request.Client;

            if (!string.IsNullOrWhiteSpace(request.Status) && project.Status != request.Status)
                project.Status = request.Status;

            deliverable.ProjectId = project.Id;
            _context.Deliverables.Update(deliverable);
            _context.Projects.Update(project);

            foreach (var task in request.Tasks)
            {
                _context.Tasks.Add(new TaskItem
                {
                    Title = task.Title,
                    Deadline = task.Deadline,
                    Status = "Open",
                    ProjectId = project.Id,
                    DeliverableId = deliverable.Id
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ProjectId = project.Id,
                DeliverableId = deliverable.Id,
                TaskCount = await _context.Tasks.CountAsync(t => t.DeliverableId == deliverableId)
            });
        }

        [HttpGet("view/{id}")]
        public IActionResult ViewFile(int id)
        {
            var deliverable = _context.Deliverables.Find(id);
            if (deliverable == null)
                return NotFound();

            string fullPath = deliverable.FilePath;

            if (!Path.IsPathRooted(fullPath))
                fullPath = Path.Combine(_env.ContentRootPath, fullPath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(fullPath, out var contentType))
                contentType = "application/octet-stream";

            Response.Headers["Content-Disposition"] = "inline";

            return PhysicalFile(fullPath, contentType);
        }

        [HttpGet("register")]
        public IActionResult GetDeliverablesRegister()
        {
            var result = _context.Deliverables
                .Include(d => d.Project)
                .Select(d => new
                {
                    d.Id,
                    d.FileName,
                    d.UploadDate,
                    ProjectName = d.Project.Name,
                    CategoryName = d.Project.Name,
                    TaskCount = _context.Tasks.Count(t => t.DeliverableId == d.Id)
                })
                .OrderByDescending(d => d.UploadDate)
                .ToList();

            return Ok(result);
        }

        [HttpGet("download/{id}")]
        public IActionResult Download(int id)
        {
            var deliverable = _context.Deliverables.Find(id);
            if (deliverable == null)
                return NotFound();

            string fullPath = deliverable.FilePath;

            if (!Path.IsPathRooted(fullPath))
                fullPath = Path.Combine(_env.ContentRootPath, fullPath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(fullPath, "application/pdf", deliverable.FileName);
        }

        [HttpGet("bulk-view")]
        public IActionResult BulkView([FromQuery] string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return BadRequest("No deliverable ids provided.");

            var idList = ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var parsed) ? parsed : 0)
                .Where(id => id > 0)
                .ToList();

            var deliverables = _context.Deliverables
                .Where(d => idList.Contains(d.Id))
                .ToList();

            var htmlItems = deliverables.Select(d => $"<li><a href='/api/deliverables/view/{d.Id}' target='_blank'>{System.Net.WebUtility.HtmlEncode(d.FileName)}</a></li>");
            var viewUrls = deliverables.Select(d => $"/api/deliverables/view/{d.Id}").ToArray();
            var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>View selected deliverables</title>
</head>
<body>
    <h1>View selected deliverables</h1>
    <p>The selected files are opening in new tabs. If a file did not open, click its link.</p>
    <ul>
        {string.Join("\n", htmlItems)}
    </ul>
    <script>
        window.onload = function() {{
            var urls = [{string.Join(",", viewUrls.Select(u => "\"" + u + "\"").ToArray())}];
            urls.forEach(function(url) {{ window.open(url, '_blank'); }});
        }};
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }

        [HttpGet("bulk-download")]
        public IActionResult BulkDownload([FromQuery] string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return BadRequest("No deliverable ids provided.");

            var idList = ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var parsed) ? parsed : 0)
                .Where(id => id > 0)
                .ToList();

            var deliverables = _context.Deliverables
                .Where(d => idList.Contains(d.Id))
                .ToList();

            var htmlItems = deliverables.Select(d => $"<li><a href='/api/deliverables/download/{d.Id}' target='_blank'>{System.Net.WebUtility.HtmlEncode(d.FileName)}</a></li>");
            var downloadUrls = deliverables.Select(d => $"/api/deliverables/download/{d.Id}").ToArray();
            var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Download selected deliverables</title>
</head>
<body>
    <h1>Download selected deliverables</h1>
    <p>The selected files are starting to download. If a download did not start, click the link.</p>
    <ul>
        {string.Join("\n", htmlItems)}
    </ul>
    <script>
        window.onload = function() {{
            var urls = [{string.Join(",", downloadUrls.Select(u => "\"" + u + "\"").ToArray())}];
            urls.forEach(function(url) {{ window.open(url, '_blank'); }});
        }};
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }
    }

    public class CreateTasksRequest
    {
        public string ProjectName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string Client { get; set; } = "";
        public string Status { get; set; } = "OnTrack";
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string StoredFileName { get; set; } = "";
        public List<DeliverableTaskDto> Tasks { get; set; } = new();
    }

    public class AddTasksRequest
    {
        public string CategoryName { get; set; } = "";
        public string Client { get; set; } = "";
        public string Status { get; set; } = "Open";
        public List<DeliverableTaskDto> Tasks { get; set; } = new();
    }

    public class DeliverableTaskDto
    {
        public string Title { get; set; } = "";
        public DateTime Deadline { get; set; }
    }
}
