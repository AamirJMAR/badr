using Microsoft.AspNetCore.Mvc;
using MyWebApp.Services;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatTestController : ControllerBase
    {
        private readonly GenerativeEngineChatService _ai;

        public ChatTestController(GenerativeEngineChatService ai)
        {
            _ai = ai;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var response = await _ai.GetChatCompletionAsync(request.Message);
            return Ok(new { response });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }
}
