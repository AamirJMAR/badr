using System.Net.Http.Json;

namespace MyWebApp.Frontend.Services
{
    public class TaskService
    {
        private readonly HttpClient _http;

        public TaskService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<TaskItem>> GetTasksAsync()
        {
            return await _http.GetFromJsonAsync<List<TaskItem>>("api/tasks")
                   ?? new List<TaskItem>();
        }
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "";
    }
}