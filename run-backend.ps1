# Lance l'API MyWebApp (port 5115)
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    Write-Host "ERREUR: .NET SDK introuvable. Installez .NET 10 SDK:" -ForegroundColor Red
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Yellow
    exit 1
}

$dataDir = "C:\MyWebApp\data"
if (-not (Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    Write-Host "Dossier cree: $dataDir" -ForegroundColor Green
}

Set-Location "$PSScriptRoot\MyWebApp"
Write-Host "Backend -> http://localhost:5115" -ForegroundColor Cyan
& $dotnet run
