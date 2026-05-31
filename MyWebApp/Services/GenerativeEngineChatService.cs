using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace MyWebApp.Services
{
    public class GenerativeEngineChatService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<GenerativeEngineChatService> _logger;
        private ChatClient? _chatClient;
        private readonly object _clientLock = new();

        public GenerativeEngineChatService(IConfiguration config, ILogger<GenerativeEngineChatService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private ChatClient GetChatClient()
        {
            if (_chatClient is not null)
                return _chatClient;

            lock (_clientLock)
            {
                if (_chatClient is not null)
                    return _chatClient;

                var apiKey = _config["GenerativeEngine:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException(
                        "GenerativeEngine:ApiKey is not configured. Set User Secrets, appsettings.Local.json, or environment variable GenerativeEngine__ApiKey.");
                }

                var model = _config["GenerativeEngine:Model"] ?? "openai.gpt-5-mini";
                var baseUrl = _config["GenerativeEngine:BaseUrl"]
                    ?? "https://generative.engine.capgemini.com/v1";

                _chatClient = new ChatClient(
                    model,
                    new ApiKeyCredential(apiKey),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri(baseUrl)
                    });

                return _chatClient;
            }
        }

        public async Task<string> GetChatCompletionAsync(string prompt)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are an AI assistant specialized in PMO document analysis."),
                new UserChatMessage(prompt)
            };

            try
            {
                ClientResult<ChatCompletion> completion = await GetChatClient().CompleteChatAsync(messages);
                return completion.Value.Content[0].Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generative Engine chat completion request failed.");
                throw;
            }
        }

        public Task<string> AnalyzeSubjectAsync(string subject)
        {
            var prompt = $"""
Tu es un assistant qui classe les emails.
Analyse UNIQUEMENT le sujet suivant et répond par UNE seule catégorie parmi :
- Demande de tâche
- Information
- Réunion / échange
- Urgent / action rapide
- Aucune action claire

Sujet :
"{subject}"
""";

            return GetChatCompletionAsync(prompt);
        }
    }
}
