using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
using MyWebApp.Services;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/ai-analysis")]
    public class AiAnalysisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly GenerativeEngineChatService _ai;

        public AiAnalysisController(
            ApplicationDbContext context,
            GenerativeEngineChatService ai)
        {
            _context = context;
            _ai = ai;
        }

        [HttpGet("analyze-emails")]
        public async Task<IActionResult> AnalyzeEmails()
        {
            var emails = _context.EmailLogs.ToList();

            var results = new List<object>();

            foreach (var email in emails)
            {
                var aiResponse = await _ai.AnalyzeSubjectAsync(email.Subject);

                results.Add(new
                {
                    EmailId = email.Id,
                    Subject = email.Subject,
                    AiAnalysis = aiResponse
                });
            }

            return Ok(results);
        }
    }
}