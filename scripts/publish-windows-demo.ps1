param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\artifacts\WordsDemo"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\Words.Windows\Words.Windows.csproj"
$resolvedOutput = Join-Path $repoRoot $OutputPath

Write-Host "Publishing Words Windows demo..." -ForegroundColor Cyan
Write-Host "Project: $projectPath"
Write-Host "Output:  $resolvedOutput"

dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $resolvedOutput

$exePath = Join-Path $resolvedOutput "Words.Windows.exe"
if (!(Test-Path $exePath)) {
    throw "Publish completed but executable was not found at $exePath"
}

Write-Host ""
Write-Host "Demo ready:" -ForegroundColor Green
Write-Host $exePath
Write-Host ""
Write-Host "Launch it with:"
Write-Host ".\scripts\launch-windows-demo.ps1"
