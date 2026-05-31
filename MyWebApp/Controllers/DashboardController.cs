using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.Today;
            var urgentCutoff = today.AddDays(7);

            var totalUsers = await _context.Users.CountAsync();
            var totalProjects = await _context.Projects.CountAsync();

            var openTasks = await _context.Tasks
                .Where(t => t.Status != "Done")
                .CountAsync();

            var urgentTasks = await _context.Tasks
                .Where(t => t.Status != "Done" && t.Deadline.Date <= urgentCutoff)
                .CountAsync();

            var highRiskProjects = await _context.Projects
                .AsNoTracking()
                .CountAsync(p => p.Deadline.Date <= urgentCutoff);

            return Ok(new
            {
                totalUsers,
                totalProjects,
                openTasks,
                urgentTasks,
                highRiskCount = highRiskProjects,
                backendStatus = "Running",
                database = "SQLite"
            });
        }
    }
}