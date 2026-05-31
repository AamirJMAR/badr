namespace MyWebApp.Frontend.Models
{
    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int OpenTasks { get; set; }
        public int UrgentTasks { get; set; }
        public int HighRiskCount { get; set; }
        public string BackendStatus { get; set; } = "Running";
        public string Database { get; set; } = "SQLite";
    }
}
