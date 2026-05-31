namespace MyWebApp.Services
{
    public static class ProjectStatusHelper
    {
        /// <summary>
        /// OnTrack: échéance dans plus de 7 jours.
        /// AtRisk: échéance dans les 7 prochains jours (aujourd'hui inclus).
        /// Delayed: échéance dépassée.
        /// </summary>
        public static string FromDeadline(DateTime deadline, DateTime? referenceDate = null)
        {
            var today = (referenceDate ?? DateTime.Today).Date;
            var due = deadline.Date;

            if (due < today)
                return "Delayed";

            if (due <= today.AddDays(7))
                return "AtRisk";

            return "OnTrack";
        }
    }
}
