# Lance le frontend Blazor (port 5058)
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    Write-Host "ERREUR: .NET SDK introuvable. Installez .NET 10 SDK:" -ForegroundColor Red
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Yellow
    exit 1
}

Set-Location "$PSScriptRoot\MyWebApp.Frontend"
Write-Host "Frontend -> http://localhost:5058" -ForegroundColor Cyan
& $dotnet run
