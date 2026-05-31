namespace MyWebApp.Models
{
    public class ProjectListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Client { get; set; } = "";
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "OnTrack";
        public int TaskCount { get; set; }
    }
}
