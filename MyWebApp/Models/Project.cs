namespace MyWebApp.Models
{
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Client { get; set; } = "";

        // Possible values: OnTrack, AtRisk, Delayed
        public string Status { get; set; } = "OnTrack";

        public DateTime Deadline { get; set; } = DateTime.Today;
    }
}
