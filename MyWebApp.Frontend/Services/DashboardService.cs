using System.Net.Http.Json;
using MyWebApp.Frontend.Models;

namespace MyWebApp.Frontend.Services
{
    public class DashboardService
    {
        private readonly HttpClient _http;

        public DashboardService(HttpClient http)
        {
            _http = http;
        }

        public async Task<DashboardStats?> GetStatsAsync()
        {
            return await _http.GetFromJsonAsync<DashboardStats>("api/dashboard/stats");
        }
    }
}