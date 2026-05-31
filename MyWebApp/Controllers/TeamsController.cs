using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/teams")]
    public class TeamsController : ControllerBase
    {
        // Stockage temporaire (suffisant pour test / démo)
        private static readonly List<TeamsMessage> Messages = new();

        // ✅ POST : recevoir un message Teams
        [HttpPost("messages")]
        public IActionResult ReceiveMessage([FromBody] TeamsMessage message)
        {
            Messages.Add(message);
            return Ok();
        }

        // ✅ GET : récupérer tous les messages Team
        [HttpGet("messages")]
        public IActionResult GetMessages()
        {
            return Ok(Messages.OrderByDescending(m => m.SentAt));
        }
    }

    // Modèle de donnée
    public class TeamsMessage
    {
        public string Author { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; }
    }
}