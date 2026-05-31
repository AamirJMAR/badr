using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
public async Task<IActionResult> GetTasks([FromQuery] int? projectId)
{
    var query = _context.Tasks.AsQueryable();

    if (projectId.HasValue)
    {
        query = query.Where(t => t.ProjectId == projectId);
    }

    return Ok(await query.ToListAsync());
}

        [HttpPost]
        public async Task<IActionResult> AddTask(TaskItem task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return Ok(task);
        }

        // PUT: api/tasks/{id}
[HttpPut("{id}")]
public async Task<IActionResult> UpdateTask(int id, TaskItem updatedTask)
{
    var task = await _context.Tasks.FindAsync(id);
    if (task == null)
        return NotFound();

    task.Title = updatedTask.Title;
    task.Deadline = updatedTask.Deadline;
    task.Status = updatedTask.Status;

    await _context.SaveChangesAsync();
    return Ok(task);
}
        // DELETE: api/tasks/{id}
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteTask(int id)
{
    var task = await _context.Tasks.FindAsync(id);
    if (task == null)
    {
        return NotFound();
    }

    _context.Tasks.Remove(task);
    await _context.SaveChangesAsync();

    return NoContent();
}
    }
}