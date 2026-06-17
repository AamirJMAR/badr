namespace MyWebApp.Frontend.Models
{
    public class PmoAnalysisPreview
    {
        public string ProjectName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public string Status { get; set; } = "OnTrack";
        public string Category { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<PmoAnalysisTaskRow> Tasks { get; set; } = new();
    }

    public class PmoAnalysisTaskRow
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Deadline { get; set; } = DateTime.Today.AddDays(7);
    }

    public class AiTasksConfirmPayload
    {
        public string CategoryName { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public string Status { get; set; } = "OnTrack";
        public string Category { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<PmoAnalysisTaskRow> Tasks { get; set; } = new();
    }

    public class BulkAnalyzeItem
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public PmoAnalysisPreview? Analysis { get; set; }
        public string? Error { get; set; }
    }

    public class BulkIdsRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
