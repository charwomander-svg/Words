param(
    [string]$DemoPath = ".\artifacts\WordsXboxDemo\Words.Xbox.exe"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$exePath = Join-Path $repoRoot $DemoPath

if (!(Test-Path $exePath)) {
    Write-Host "Published Xbox-facing demo not found. Building it now..." -ForegroundColor Yellow
    & (Join-Path $PSScriptRoot "publish-xbox-demo.ps1")
}

$resolvedFolder = Split-Path $exePath -Parent
$latestBuilt = Get-ChildItem -Path $resolvedFolder -Filter "Words.Xbox*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($latestBuilt -ne $null) {
    $exePath = $latestBuilt.FullName
}

$exePath = Resolve-Path $exePath
Write-Host "Launching Words Xbox-facing graphical demo:" -ForegroundColor Cyan
Write-Host $exePath
Start-Process $exePath
