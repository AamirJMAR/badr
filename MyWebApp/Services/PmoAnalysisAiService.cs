using System.Text.Json;

namespace MyWebApp.Services
{
    /// <summary>
    /// Analyse PMO unifiée (email, calendrier) via Capgemini Generative Engine, avec repli développement comme api/ai.
    /// Les livrables (/api/deliverables/analyze) utilisent encore une simulation locale séparée.
    /// </summary>
    public class PmoAnalysisAiService
    {
        private readonly GenerativeEngineChatService _aiClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PmoAnalysisAiService> _logger;

        public PmoAnalysisAiService(
            GenerativeEngineChatService aiClient,
            IConfiguration configuration,
            ILogger<PmoAnalysisAiService> logger)
        {
            _aiClient = aiClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PmoAnalysisResult> AnalyzeAsync(string sourceLabel, string content, CancellationToken cancellationToken = default)
        {
            var prompt = BuildPrompt(sourceLabel, content);
            string responseBody;

            try
            {
                responseBody = await _aiClient.GetChatCompletionAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generative Engine call failed for {SourceLabel}", sourceLabel);

                if (!IsDevFallbackEnabled())
                    throw;

                _logger.LogWarning("Using PMO AI dev fallback for {SourceLabel}", sourceLabel);
                responseBody = BuildDevFallbackJson(sourceLabel, content);
            }

            try
            {
                return ParseAnalysisJson(responseBody);
            }
            catch (Exception ex) when (IsDevFallbackEnabled())
            {
                _logger.LogWarning(ex, "Invalid AI JSON for {SourceLabel}, using dev fallback", sourceLabel);
                return ParseAnalysisJson(BuildDevFallbackJson(sourceLabel, content));
            }
        }

        private bool IsDevFallbackEnabled() =>
            string.Equals(_configuration["GenerativeEngine:UseDevFallback"], "true", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("USE_AI_DEV_FALLBACK"));

        private static string BuildPrompt(string sourceLabel, string content) =>
            $"Analyze the following {sourceLabel} and return only valid JSON in this exact format:\n"
            + "{\n"
            + "  \"ProjectName\": \"...\",\n"
            + "  \"Client\": \"...\",\n"
            + "  \"Status\": \"OnTrack|AtRisk|Delayed\",\n"
            + "  \"Category\": \"...\",\n"
            + "  \"Summary\": \"...\",\n"
            + "  \"Tasks\": [\n"
            + "    { \"Title\": \"...\", \"Deadline\": \"yyyy-MM-dd|TBD\" }\n"
            + "  ]\n"
            + "}\n"
            + "Do not include any extra explanation or markdown.\n\n"
            + content;

        private static string BuildDevFallbackJson(string sourceLabel, string content)
        {
            var isDemo = content.Contains(DemoDataService.DemoEmailMarker, StringComparison.Ordinal)
                || content.Contains(DemoDataService.DemoCalendarMarker, StringComparison.Ordinal);

            if (isDemo && sourceLabel.Contains("calendar", StringComparison.OrdinalIgnoreCase))
            {
                return """
{
  "ProjectName": "Comité de pilotage Q2",
  "Client": "Client démo",
  "Status": "AtRisk",
  "Category": "Réunion / échange",
  "Summary": "Réunion de pilotage pour valider l'avancement, les risques et les actions avant la livraison Q2.",
  "Tasks": [
    { "Title": "Préparer le support de comité de pilotage", "Deadline": "2026-05-21" },
    { "Title": "Consolider le tableau de bord des risques", "Deadline": "2026-05-22" },
    { "Title": "Envoyer le compte-rendu aux participants", "Deadline": "2026-05-23" }
  ]
}
""";
            }

            if (isDemo && sourceLabel.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                return """
{
  "ProjectName": "Revue projet Q2",
  "Client": "Client démo",
  "Status": "AtRisk",
  "Category": "Action Required",
  "Summary": "Email de suivi demandant la mise à jour du planning, l'envoi du compte-rendu client et la confirmation de la revue technique.",
  "Tasks": [
    { "Title": "Mettre à jour le planning des livrables Q2", "Deadline": "2026-05-23" },
    { "Title": "Envoyer le compte-rendu au client", "Deadline": "2026-05-22" },
    { "Title": "Confirmer la date de la revue technique", "Deadline": "2026-05-21" }
  ]
}
""";
            }

            return """
{
  "ProjectName": "Projet PMO (mode démo)",
  "Client": "Client interne",
  "Status": "OnTrack",
  "Category": "Information",
  "Summary": "Analyse générée en mode démonstration (Generative Engine indisponible). Activez une clé API valide pour une analyse réelle.",
  "Tasks": [
    { "Title": "Vérifier les actions identifiées", "Deadline": "TBD" },
    { "Title": "Mettre à jour le suivi projet", "Deadline": "TBD" }
  ]
}
""";
        }

        public static PmoAnalysisResult ParseAnalysisJson(string responseBody)
        {
            var jsonPayload = ExtractFirstJsonObject(responseBody);

            using var json = JsonDocument.Parse(jsonPayload);
            var root = json.RootElement;

            var tasks = new List<PmoAnalysisTask>();
            if (root.TryGetProperty("Tasks", out var tasksElement) && tasksElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var taskElement in tasksElement.EnumerateArray())
                {
                    tasks.Add(new PmoAnalysisTask
                    {
                        Title = taskElement.TryGetProperty("Title", out var t) ? t.GetString() ?? "" : "",
                        Deadline = taskElement.TryGetProperty("Deadline", out var d) ? d.GetString() ?? "TBD" : "TBD"
                    });
                }
            }

            return new PmoAnalysisResult
            {
                ProjectName = root.TryGetProperty("ProjectName", out var pn) ? pn.GetString() ?? "" : "",
                Client = root.TryGetProperty("Client", out var cl) ? cl.GetString() ?? "" : "",
                Status = root.TryGetProperty("Status", out var st) ? st.GetString() ?? "OnTrack" : "OnTrack",
                Category = root.TryGetProperty("Category", out var cat) ? cat.GetString() ?? "Other" : "Other",
                Summary = root.TryGetProperty("Summary", out var sum) ? sum.GetString() ?? "" : "",
                Tasks = tasks
            };
        }

        private static string ExtractFirstJsonObject(string text)
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            return (start >= 0 && end > start) ? text[start..(end + 1)] : text;
        }
    }

    public class PmoAnalysisResult
    {
        public string ProjectName { get; set; } = "";
        public string Client { get; set; } = "";
        public string Status { get; set; } = "OnTrack";
        public string Category { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<PmoAnalysisTask> Tasks { get; set; } = new();
    }

    public class PmoAnalysisTask
    {
        public string Title { get; set; } = "";
        public string Deadline { get; set; } = "TBD";
    }
}
