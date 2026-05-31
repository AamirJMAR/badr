namespace MyWebApp.Frontend.Models
{
    public class EmailLog
    {
        public int Id { get; set; }
        public string Subject { get; set; } = "";
        public string? From { get; set; }
        public string Body { get; set; } = "";
        public DateTime ReceivedDate { get; set; }
        public string? AiCategory { get; set; }
        public string? AiSummary { get; set; }
        public string? AiRecommendations { get; set; }
        public bool AiAnalyzed { get; set; }
        public bool IsSelected { get; set; }
    }
}
