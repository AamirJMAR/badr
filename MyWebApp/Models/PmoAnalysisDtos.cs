namespace MyWebApp.Models
{
    public class BulkIdsRequest
    {
        public List<int> Ids { get; set; } = new();
    }

    public class PmoAnalysisTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string Deadline { get; set; } = "TBD";
    }

    public class PmoAnalysisPreviewDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public string Status { get; set; } = "OnTrack";
        public string Category { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<PmoAnalysisTaskDto> Tasks { get; set; } = new();
    }

    public class BulkAnalyzeItemDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public PmoAnalysisPreviewDto? Analysis { get; set; }
        public string? Error { get; set; }
    }
}
