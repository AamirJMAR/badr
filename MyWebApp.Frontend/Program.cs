using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyWebApp.Frontend;
using MyWebApp.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ✅ Composants racine
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ✅ HttpClient

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5115/")
    });


// ✅ Services applicatifs
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<DashboardService>();

// ✅ Build de l'application
var app = builder.Build();

// ✅ Initialisation asynchrone AVANT Run (parfait)
var authService = app.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

// ✅ Lancer l'application
await app.RunAsync();