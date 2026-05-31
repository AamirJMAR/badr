namespace MyWebApp.Models
{
    public class AutomationMessage
    {
        public string Source { get; set; } = "";     // Teams, PMO, Flow
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}