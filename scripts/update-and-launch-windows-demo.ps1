param(
    [switch]$SkipPull
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if (!$SkipPull) {
    Write-Host "Updating repository..." -ForegroundColor Cyan
    git -C $repoRoot pull --ff-only
}

& (Join-Path $PSScriptRoot "publish-windows-demo.ps1")
& (Join-Path $PSScriptRoot "launch-windows-demo.ps1")
