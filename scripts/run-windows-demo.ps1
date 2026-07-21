$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\Words.Windows\Words.Windows.csproj"

Write-Host "Running Words Windows demo from source..." -ForegroundColor Cyan
dotnet run --project $projectPath
