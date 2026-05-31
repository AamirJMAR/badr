namespace MyWebApp.Models
{
    public class CalendarEvent
    {
        public int Id { get; set; }
        public string Subject { get; set; } = "";

        // Valeurs RAW reçues depuis Power Automate
        public string? StartTimeRaw { get; set; }
        public string? EndTimeRaw { get; set; }

        // Valeurs converties et stockées
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // ✅ NOUVEAUX CHAMPS POUR LA PAGE DETAILS
        public string? Organizer { get; set; }
        public string? RequiredAttendees { get; set; }
        public string? OptionalAttendees { get; set; }

        // ✅ AI ANALYSIS FIELDS
        public string? AiProjectName { get; set; }
        public string? AiClient { get; set; }
        public string? AiStatus { get; set; } = "OnTrack";
        public string? AiCategory { get; set; }
        public string? AiSummary { get; set; }
        public string? AiRecommendations { get; set; }
        public bool AiAnalyzed { get; set; } = false;
    }
}
