namespace MyWebApp.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "Open"; // Open, InProgress, Done
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
        public int? DeliverableId { get; set; }
        public Deliverable? Deliverable { get; set; }

    }
}