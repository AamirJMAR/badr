using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/automation")]
    public class AutomationController : ControllerBase
    {
        // POST: api/automation/message
        [HttpPost("message")]
        public IActionResult ReceiveMessage([FromBody] AutomationMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
                return BadRequest("Message content is required");

            // Ici tu peux :
            // - Sauvegarder en base
            // - Déclencher une logique PMO
            // - L'afficher dans le dashboard

            Console.WriteLine($"Message reçu de {message.Source}: {message.Title}");

            return Ok(new { Status = "Message reçu avec succès" });
        }
    }
}