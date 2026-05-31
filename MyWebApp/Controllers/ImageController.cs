using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/image")]
    public class ImageController : ControllerBase
    {
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] ImagePrompt request)
        {
            // Ici tu pourrais appeler Power Automate Cloud
            // ou simplement retourner une image résultat (démo)

            // Simulation (pour PFE)
            await Task.Delay(5000);

            return Ok(new
            {
                ImageUrl = "https://via.placeholder.com/512x512.png?text=Generated+Image"
            });
        }
    }

    public class ImagePrompt
    {
        public string Prompt { get; set; } = "";
    }
}