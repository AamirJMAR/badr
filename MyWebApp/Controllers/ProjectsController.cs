using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Constants;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/projects")]
    public class ProjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectById(int id)
        {
            var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
                return NotFound();

            var taskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id);

            return Ok(new ProjectListItemDto
            {
                Id = project.Id,
                Name = project.Name,
                Client = project.Client,
                Deadline = project.Deadline,
                Status = ProjectStatusHelper.FromDeadline(project.Deadline),
                TaskCount = taskCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            var today = DateTime.Today;
            var fixedNames = CategoryConstants.FixedCategories.ToHashSet(StringComparer.Ordinal);

            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p => fixedNames.Contains(p.Name))
                .ToListAsync();

            var taskCounts = await _context.Tasks
                .Where(t => t.ProjectId != null)
                .GroupBy(t => t.ProjectId!.Value)
                .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ProjectId, g => g.Count);

            var result = CategoryConstants.FixedCategories
                .Select(name =>
                {
                    var p = projects.FirstOrDefault(x => x.Name == name);
                    if (p == null)
                        return null;

                    return new ProjectListItemDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Client = p.Client,
                        Deadline = p.Deadline,
                        Status = ProjectStatusHelper.FromDeadline(p.Deadline, today),
                        TaskCount = taskCounts.GetValueOrDefault(p.Id)
                    };
                })
                .Where(p => p != null)
                .Cast<ProjectListItemDto>()
                .ToList();

            return Ok(result);
        }

        [HttpPost]
        public IActionResult AddProject([FromBody] Project project)
        {
            if (!CategoryConstants.IsValid(project.Name))
                return BadRequest($"Category must be one of: {string.Join(", ", CategoryConstants.FixedCategories)}");

            return BadRequest("Categories are fixed and cannot be created manually.");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProject(int id)
        {
            return BadRequest("Categories are fixed and cannot be deleted.");
        }
    }
}
