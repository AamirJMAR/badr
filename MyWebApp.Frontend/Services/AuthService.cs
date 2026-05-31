using Microsoft.JSInterop;
using System.Text.Json;

namespace MyWebApp.Frontend.Services
{
    public class AuthService
    {
        private const string UsersKey = "pmo_users";
        private const string CurrentUserKey = "current_user";

        private readonly IJSRuntime _js;

        private Dictionary<string, string> _users = new();
        private string? _currentUser;

        public AuthService(IJSRuntime js)
        {
            _js = js;
        }

        // ✅ Propriétés SAFE (plus de JS ici)
        public bool IsAuthenticated => !string.IsNullOrEmpty(_currentUser);
        public string? CurrentUser => _currentUser;

        // =====================
        // Initialisation
        // =====================
        public async Task InitializeAsync()
        {
            var usersJson = await _js.InvokeAsync<string>("localStorage.getItem", UsersKey);
            if (!string.IsNullOrEmpty(usersJson))
            {
                _users = JsonSerializer.Deserialize<Dictionary<string, string>>(usersJson)
                         ?? new Dictionary<string, string>();
            }

            _currentUser = await _js.InvokeAsync<string>("localStorage.getItem", CurrentUserKey);
        }

        // =====================
        // Auth
        // =====================
        public async Task<bool> LoginAsync(string username, string password)
        {
            if (_users.TryGetValue(username, out var storedPwd)
                && storedPwd == password)
            {
                _currentUser = username;
                await _js.InvokeVoidAsync("localStorage.setItem", CurrentUserKey, username);
                return true;
            }
            return false;
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            if (_users.ContainsKey(username))
                return false;

            _users.Add(username, password);
            var json = JsonSerializer.Serialize(_users);
            await _js.InvokeVoidAsync("localStorage.setItem", UsersKey, json);
            return true;
        }

        public async Task LogoutAsync()
        {
            _currentUser = null;
            await _js.InvokeVoidAsync("localStorage.removeItem", CurrentUserKey);
        }
    }
}   