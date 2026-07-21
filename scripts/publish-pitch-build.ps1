param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing pitch package (Windows + Xbox host)..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "publish-windows-demo.ps1") -Configuration $Configuration
& (Join-Path $PSScriptRoot "publish-xbox-demo.ps1") -Configuration $Configuration

Write-Host ""
Write-Host "Pitch package complete." -ForegroundColor Green
Write-Host "Windows demo: .\artifacts\WordsDemo\Words.Windows.exe"
Write-Host "Xbox host demo: .\artifacts\WordsXboxDemo\Words.Xbox.exe"
