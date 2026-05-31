using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var projects = await _context.Projects.AsNoTracking().ToListAsync();

            var taskCounts = await _context.Tasks
                .Where(t => t.ProjectId != null)
                .GroupBy(t => t.ProjectId!.Value)
                .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ProjectId, g => g.Count);

            var result = projects
                .Select(p => new ProjectListItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Client = p.Client,
                    Deadline = p.Deadline,
                    Status = ProjectStatusHelper.FromDeadline(p.Deadline, today),
                    TaskCount = taskCounts.GetValueOrDefault(p.Id)
                })
                .OrderBy(p => p.Deadline)
                .ToList();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddProject(Project project)
        {
            project.Status = ProjectStatusHelper.FromDeadline(project.Deadline);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var taskCount = await _context.Tasks.CountAsync(t => t.ProjectId == project.Id);

            return Ok(new ProjectListItemDto
            {
                Id = project.Id,
                Name = project.Name,
                Client = project.Client,
                Deadline = project.Deadline,
                Status = project.Status,
                TaskCount = taskCount
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
