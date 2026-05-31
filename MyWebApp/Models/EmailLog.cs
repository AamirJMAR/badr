namespace MyWebApp.Models
{
    public class EmailLog
    {
        public int Id { get; set; }

        public string Subject { get; set; } = "";
        public string? From { get; set; }

        // ✅ Body texte only
        public string Body { get; set; } = "";

        // ✅ Date unique et claire
        public DateTime ReceivedDate { get; set; }

        // ✅ Champs IA pour l'analyse des emails
        public string? AiCategory { get; set; }      // Ex: "Urgent", "Information", "Meeting", "Action Required"
        public string? AiSummary { get; set; }       // Résumé court de l'email
        public string? AiRecommendations { get; set; } // Recommandations d'actions (JSON)
        public bool AiAnalyzed { get; set; } = false;  // Flag pour savoir si email a été analysé
    }
}
