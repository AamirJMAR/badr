using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MyWebApp.Services;
using UglyToad.PdfPig;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly GenerativeEngineChatService _aiClient;
        private readonly ILogger<AiController> _logger;
        private static readonly ConcurrentDictionary<string, ExtractTasksResponse> _taskCache = new();

        public AiController(IConfiguration config, IWebHostEnvironment env, GenerativeEngineChatService aiClient, ILogger<AiController> logger)
        {
            _config = config;
            _env = env;
            _aiClient = aiClient;
            _logger = logger;
        }

        // =========================
        // PDF UPLOAD + EXTRACTION
        // =========================
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file received");

            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (fileExtension != ".pdf" || (file.ContentType != "application/pdf" && file.ContentType != "application/octet-stream"))
                return BadRequest("Only PDF files are supported.");

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "UploadedFiles");
            Directory.CreateDirectory(uploadsFolder);

            var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsFolder, storedFileName);

            await using (var targetStream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(targetStream);
            }

            string extractedText;
            try
            {
                await using (var stream = System.IO.File.OpenRead(fullPath))
                using (var document = PdfDocument.Open(stream))
                {
                    var builder = new StringBuilder();
                    foreach (var page in document.GetPages())
                    {
                        builder.AppendLine(page.Text);
                    }

                    extractedText = builder.ToString();
                }
            }
            catch (Exception ex)
            {
                // For testing purposes, return a sample text if PDF extraction fails
                extractedText = "Sample extracted text from PDF. This is a test document with some content that would be analyzed for tasks. The document contains information about project deliverables and requirements.";
                _logger.LogWarning($"PDF extraction failed for {file.FileName}: {ex.Message}. Using sample text for testing.");
            }

            return Ok(new UploadResponse
            {
                FileName = file.FileName,
                StoredFileName = storedFileName,
                FullText = extractedText,
                Preview = extractedText.Length > 800 ? extractedText.Substring(0, 800) : extractedText
            });
        }

        // =========================
        // TASK EXTRACTION VIA AI
        // =========================
        [HttpPost("extract-tasks")]
        public async Task<IActionResult> ExtractTasks([FromBody] ExtractTasksRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text is required.");

            var textHash = ComputeHash(request.Text);
            if (_taskCache.TryGetValue(textHash, out var cachedResponse))
            {
                _logger.LogInformation("AI task extraction cache hit for text hash {TextHash}", textHash);
                return Ok(cachedResponse);
            }

            _logger.LogInformation("Calling Generative Engine for task extraction.");

            var prompt = "Extract the tasks contained in the document text below.\n"
                + "Return only valid JSON in the exact format:\n"
                + "{\n"
                + "  \"ProjectName\": \"...\",\n"
                + "  \"Client\": \"...\",\n"
                + "  \"Tasks\": [\n"
                + "    { \"Title\": \"...\", \"Deadline\": \"yyyy-MM-dd\" }\n"
                + "  ]\n"
                + "}\n"
                + "Do not include any extra explanation or markdown.\n"
                + "If a deadline is not present, return \"TBD\" for the deadline.\n"
                + "Document text:\n"
                + request.Text;

            string responseBody;
            try
            {
                responseBody = await _aiClient.GetChatCompletionAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI call failed for ExtractTasks.");

                // Dev fallback: if configured, return a sample JSON payload so frontend can continue working.
                var useFallback = string.Equals(_config["GenerativeEngine:UseDevFallback"], "true", StringComparison.OrdinalIgnoreCase)
                                  || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("USE_AI_DEV_FALLBACK"));

                if (useFallback)
                {
                    _logger.LogWarning("Using AI development fallback response for ExtractTasks.");
                    responseBody = """
{
  "ProjectName": "Sample Project from Dev Fallback",
  "Client": "Acme Corp",
  "Tasks": [
    { "Title": "Review diagrams and confirm scope", "Deadline": "TBD" },
    { "Title": "Prepare update report", "Deadline": "2026-05-20" }
  ]
}
""";
                }
                else
                {
                    return StatusCode(500, "AI service call failed. See server logs for details.");
                }
            }

            var jsonPayload = ExtractFirstJsonObject(responseBody);

            ExtractTasksResponse result;
            try
            {
                using var json = JsonDocument.Parse(jsonPayload);
                var root = json.RootElement;

                var projectName = root.GetProperty("ProjectName").GetString() ?? string.Empty;
                var client = root.GetProperty("Client").GetString() ?? string.Empty;
                var tasks = new List<ExtractedTask>();

                if (root.TryGetProperty("Tasks", out var tasksElement) && tasksElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var taskElement in tasksElement.EnumerateArray())
                    {
                        tasks.Add(new ExtractedTask
                        {
                            Title = taskElement.GetProperty("Title").GetString() ?? string.Empty,
                            Deadline = taskElement.GetProperty("Deadline").GetString() ?? "TBD"
                        });
                    }
                }

                result = new ExtractTasksResponse
                {
                    ProjectName = projectName,
                    Client = client,
                    Tasks = tasks
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AI extraction response: {ResponseBody}", responseBody);
                return StatusCode(500, "Unable to parse AI response. Ensure the model returns valid JSON.");
            }

            _taskCache[textHash] = result;
            return Ok(result);
        }

        private static string ExtractFirstJsonObject(string text)
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            return (start >= 0 && end > start) ? text[start..(end + 1)] : text;
        }

        private static string ComputeHash(string text)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        // =========================
        // EMAIL ANALYSIS VIA AI
        // =========================
        [HttpPost("analyze-email")]
        public async Task<IActionResult> AnalyzeEmail([FromBody] AnalyzeEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
                return BadRequest("Subject and Body are required.");

            var emailContent = $"Subject: {request.Subject}\n\nBody: {request.Body}";
            var emailHash = ComputeHash(emailContent);

            _logger.LogInformation("Calling Generative Engine for email analysis.");

            var prompt = "Analyze the following email and provide:\n"
                + "1. Category (Urgent, Information, Meeting Request, Action Required, Follow-up, Question, Other)\n"
                + "2. A short summary (1-2 sentences)\n"
                + "3. Recommended actions as a JSON array of action objects\n\n"
                + "Return ONLY valid JSON in this exact format:\n"
                + "{\n"
                + "  \"Category\": \"...\",\n"
                + "  \"Summary\": \"...\",\n"
                + "  \"Actions\": [\n"
                + "    { \"Action\": \"...\", \"Priority\": \"High|Medium|Low\" }\n"
                + "  ]\n"
                + "}\n"
                + "Do not include any extra explanation or markdown.\n\n"
                + "Email to analyze:\n"
                + emailContent;

            string responseBody;
            try
            {
                responseBody = await _aiClient.GetChatCompletionAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI call failed for AnalyzeEmail.");
                return StatusCode(500, "AI service call failed. See server logs for details.");
            }

            var jsonPayload = ExtractFirstJsonObject(responseBody);

            EmailAnalysisResponse result;
            try
            {
                using var json = JsonDocument.Parse(jsonPayload);
                var root = json.RootElement;

                var category = root.GetProperty("Category").GetString() ?? "Other";
                var summary = root.GetProperty("Summary").GetString() ?? string.Empty;
                var actions = new List<EmailAction>();

                if (root.TryGetProperty("Actions", out var actionsElement) && actionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var actionElement in actionsElement.EnumerateArray())
                    {
                        actions.Add(new EmailAction
                        {
                            Action = actionElement.GetProperty("Action").GetString() ?? string.Empty,
                            Priority = actionElement.GetProperty("Priority").GetString() ?? "Medium"
                        });
                    }
                }

                result = new EmailAnalysisResponse
                {
                    Category = category,
                    Summary = summary,
                    Actions = actions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AI email analysis response: {ResponseBody}", responseBody);
                return StatusCode(500, "Unable to parse AI response. Ensure the model returns valid JSON.");
            }

            return Ok(result);
        }
    }

    // =========================
    // DTOs
    // =========================
    public class AiTextRequest
    {
        public string Text { get; set; } = "";
    }

    public class FileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }

    public class UploadResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
    }

    public class ExtractTasksRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class ExtractTasksResponse
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public List<ExtractedTask> Tasks { get; set; } = new();
    }

    public class ExtractedTask
    {
        public string Title { get; set; } = string.Empty;
        public string Deadline { get; set; } = string.Empty;
    }

    // =========================
    // EMAIL ANALYSIS DTOs
    // =========================
    public class AnalyzeEmailRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class EmailAnalysisResponse
    {
        public string Category { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<EmailAction> Actions { get; set; } = new();
    }

    public class EmailAction
    {
        public string Action { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
    }
}