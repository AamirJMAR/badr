using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Services
{
    /// <summary>
    /// Données de démonstration pour tester Emails / Calendrier sans Outlook ni Power Automate.
    /// </summary>
    public class DemoDataService
    {
        public const string DemoEmailMarker = "[DÉMO]";
        public const string DemoCalendarMarker = "[DÉMO]";

        private readonly ApplicationDbContext _context;

        public DemoDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DemoImportResult> ImportDemoEmailAsync(bool simulateDelay = true, CancellationToken cancellationToken = default)
        {
            if (simulateDelay)
                await Task.Delay(900, cancellationToken);

            var subject = $"{DemoEmailMarker} Revue projet – actions à valider";
            var existing = await _context.EmailLogs
                .FirstOrDefaultAsync(e => e.Subject == subject, cancellationToken);

            var now = DateTime.Now;

            if (existing != null)
            {
                existing.ReceivedDate = now;
                existing.From = "marie.dupont@capgemini.com";
                existing.Body = DemoEmailBody;
                _context.EmailLogs.Update(existing);
                await _context.SaveChangesAsync(cancellationToken);

                return new DemoImportResult(true, existing.Id, "Email de démonstration mis à jour (comme une resynchronisation Outlook).");
            }

            var email = new EmailLog
            {
                Subject = subject,
                From = "marie.dupont@capgemini.com",
                Body = DemoEmailBody,
                ReceivedDate = now,
                AiAnalyzed = false
            };

            _context.EmailLogs.Add(email);
            await _context.SaveChangesAsync(cancellationToken);

            return new DemoImportResult(false, email.Id, "Email de démonstration importé depuis la boîte Outlook (simulation).");
        }

        public async Task<DemoImportResult> ImportDemoCalendarEventAsync(bool simulateDelay = true, CancellationToken cancellationToken = default)
        {
            if (simulateDelay)
                await Task.Delay(900, cancellationToken);

            var subject = $"{DemoCalendarMarker} Comité de pilotage – point d'avancement";
            var start = DateTime.Today.AddDays(2).Date.AddHours(10);
            var end = start.AddHours(1);
            var startRaw = start.ToString("dd/MM/yyyy HH:mm:ss");
            var endRaw = end.ToString("dd/MM/yyyy HH:mm:ss");

            var existing = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.Subject == subject, cancellationToken);

            if (existing != null)
            {
                existing.StartTime = start;
                existing.EndTime = end;
                existing.StartTimeRaw = startRaw;
                existing.EndTimeRaw = endRaw;
                existing.Organizer = "vous@capgemini.com";
                existing.RequiredAttendees = "equipe.projet@capgemini.com";
                _context.CalendarEvents.Update(existing);
                await _context.SaveChangesAsync(cancellationToken);

                return new DemoImportResult(true, existing.Id, "Événement de démonstration mis à jour (comme une resynchronisation Outlook).");
            }

            var ev = new CalendarEvent
            {
                Subject = subject,
                StartTime = start,
                EndTime = end,
                StartTimeRaw = startRaw,
                EndTimeRaw = endRaw,
                Organizer = "vous@capgemini.com",
                RequiredAttendees = "equipe.projet@capgemini.com",
                OptionalAttendees = "client@example.com",
                AiAnalyzed = false
            };

            _context.CalendarEvents.Add(ev);
            await _context.SaveChangesAsync(cancellationToken);

            return new DemoImportResult(false, ev.Id, "Événement de démonstration importé depuis le calendrier Outlook (simulation).");
        }

        public async Task SeedIfEmptyAsync(CancellationToken cancellationToken = default)
        {
            if (!await _context.EmailLogs.AnyAsync(e => e.Subject.StartsWith(DemoEmailMarker), cancellationToken))
                await ImportDemoEmailAsync(simulateDelay: false, cancellationToken);

            if (!await _context.CalendarEvents.AnyAsync(e => e.Subject.StartsWith(DemoCalendarMarker), cancellationToken))
                await ImportDemoCalendarEventAsync(simulateDelay: false, cancellationToken);
        }

        private const string DemoEmailBody =
            "Bonjour,\n\n" +
            "Suite à notre réunion de ce matin, merci de valider les points suivants avant vendredi :\n\n" +
            "1. Mettre à jour le planning des livrables Q2\n" +
            "2. Envoyer le compte-rendu au client\n" +
            "3. Confirmer la date de la revue technique\n\n" +
            "Cordialement,\nMarie Dupont\n\n" +
            "---\n" +
            "Cet email est une donnée de DÉMONSTRATION pour tester l'application sans connexion Outlook réelle.";

        public record DemoImportResult(bool WasUpdated, int Id, string Message);
    }
}
