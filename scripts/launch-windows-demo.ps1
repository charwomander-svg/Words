param(
    [string]$DemoPath = ".\artifacts\WordsDemo\Words.Windows.exe"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$exePath = Join-Path $repoRoot $DemoPath

if (!(Test-Path $exePath)) {
    Write-Host "Published demo not found. Building it now..." -ForegroundColor Yellow
    & (Join-Path $PSScriptRoot "publish-windows-demo.ps1")
}

$exePath = Resolve-Path $exePath
Write-Host "Launching Words demo:" -ForegroundColor Cyan
Write-Host $exePath
Start-Process $exePath
