$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\Words.Windows\Words.Windows.csproj"

Write-Host "Running Words Xbox-facing graphical demo shell..." -ForegroundColor Cyan
dotnet run --project $projectPath
