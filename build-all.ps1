# Compile backend + frontend (sans dependre du PATH)
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    Write-Host "ERREUR: Installez .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Red
    exit 1
}

$root = $PSScriptRoot
Write-Host "Build backend..." -ForegroundColor Cyan
& $dotnet build "$root\MyWebApp\MyWebApp.csproj"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Build frontend..." -ForegroundColor Cyan
& $dotnet build "$root\MyWebApp.Frontend\MyWebApp.Frontend.csproj"
exit $LASTEXITCODE
