using Microsoft.AspNetCore.Mvc;
using MyWebApp.Services;
using System.Net.Http;
using System.Text;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/sync")]
    public class SyncController : ControllerBase
    {
        private static bool isEmailSyncRunning;
        private static bool isCalendarSyncRunning;

        private readonly IConfiguration _configuration;
        private readonly DemoDataService _demoData;
        private readonly ILogger<SyncController> _logger;

        private const string CloudFlowUrl =
            "https://d371d778f74de3e898d9942b5a1786.40.environment.api.powerplatform.com:443/powerautomate/automations/direct/workflows/0e1bded534994adebc16b76ad0808330/triggers/manual/paths/invoke?api-version=1&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=HrE26zGba7_bustdgPncw6n3YEtLRB51KPNrwrrrW4o";

        public SyncController(IConfiguration configuration, DemoDataService demoData, ILogger<SyncController> logger)
        {
            _configuration = configuration;
            _demoData = demoData;
            _logger = logger;
        }

        private bool IsDemoMode =>
            _configuration.GetValue<bool>("Sync:DemoMode");

        [HttpPost("emails")]
        public async Task<IActionResult> SyncEmails(CancellationToken cancellationToken)
        {
            if (isEmailSyncRunning)
            {
                return Ok(new
                {
                    Status = "AlreadyRunning",
                    Message = "Synchronisation déjà en cours."
                });
            }

            isEmailSyncRunning = true;

            try
            {
                if (IsDemoMode)
                {
                    var result = await _demoData.ImportDemoEmailAsync(cancellationToken: cancellationToken);
                    return Ok(new
                    {
                        Status = "DemoSuccess",
                        DemoMode = true,
                        result.WasUpdated,
                        result.Id,
                        Message = result.Message
                    });
                }

                using var httpClient = new HttpClient();
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(CloudFlowUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(500, new
                    {
                        Status = "Error",
                        Message = $"Échec du déclenchement Power Automate (HTTP {(int)response.StatusCode})."
                    });
                }

                return Ok(new
                {
                    Status = "Started",
                    DemoMode = false,
                    Message = "Flux cloud déclenché. Le flux bureau importera vos emails Outlook."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sync failed");
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
            finally
            {
                isEmailSyncRunning = false;
            }
        }

        [HttpPost("calendar")]
        public async Task<IActionResult> SyncCalendar(CancellationToken cancellationToken)
        {
            if (isCalendarSyncRunning)
            {
                return Ok(new
                {
                    Status = "AlreadyRunning",
                    Message = "Synchronisation déjà en cours."
                });
            }

            isCalendarSyncRunning = true;

            try
            {
                if (IsDemoMode)
                {
                    var result = await _demoData.ImportDemoCalendarEventAsync(cancellationToken: cancellationToken);
                    return Ok(new
                    {
                        Status = "DemoSuccess",
                        DemoMode = true,
                        result.WasUpdated,
                        result.Id,
                        Message = result.Message
                    });
                }

                return Ok(new
                {
                    Status = "NotConfigured",
                    DemoMode = false,
                    Message = "Synchronisation calendrier réelle : configurez Power Automate sur le PC de production. Activez Sync:DemoMode pour tester."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Calendar sync failed");
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
            finally
            {
                isCalendarSyncRunning = false;
            }
        }
    }
}
