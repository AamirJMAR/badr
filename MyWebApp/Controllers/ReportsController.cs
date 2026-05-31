using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetTasksReport()
        {
            var today = DateTime.Today;

            var total = await _context.Tasks.CountAsync();
            var done = await _context.Tasks.CountAsync(t => t.Status == "Done");
            var open = await _context.Tasks.CountAsync(t => t.Status == "Open");
            var inProgress = await _context.Tasks.CountAsync(t => t.Status == "InProgress");
            var overdue = await _context.Tasks.CountAsync(t => t.Deadline < today && t.Status != "Done");

            var completionRate = total == 0 ? 0 : (done * 100) / total;

            return Ok(new
            {
                TotalTasks = total,
                DoneTasks = done,
                OpenTasks = open,
                InProgressTasks = inProgress,
                OverdueTasks = overdue,
                CompletionRate = completionRate
            });
        }
        [HttpGet("project/{projectId}")]
public async Task<IActionResult> GetProjectOverview(int projectId)
{
    var tasks = await _context.Tasks
        .Where(t => t.ProjectId == projectId)
        .ToListAsync();

    var total = tasks.Count;
    var done = tasks.Count(t => t.Status == "Done");
    var inProgress = tasks.Count(t => t.Status == "InProgress");
    var open = tasks.Count(t => t.Status == "Open");

    var completionRate = total == 0 ? 0 : (done * 100) / total;

    return Ok(new
    {
        Total = total,
        Done = done,
        InProgress = inProgress,
        Open = open,
        CompletionRate = completionRate
    });
}
    }
}
