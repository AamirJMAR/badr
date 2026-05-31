namespace MyWebApp.Frontend.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "Open";
        public DateTime Deadline { get; set; }
        public string Description { get; set; } = "";
    }
}