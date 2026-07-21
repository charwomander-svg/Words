param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\artifacts\WordsXboxDemo"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\Words.Windows\Words.Windows.csproj"
$resolvedOutput = Join-Path $repoRoot $OutputPath
$publishOutput = Join-Path $resolvedOutput ("publish-" + (Get-Date -Format "yyyyMMdd-HHmmss"))

Write-Host "Publishing Words Xbox-facing graphical demo..." -ForegroundColor Cyan
Write-Host "Project: $projectPath"
Write-Host "Output:  $resolvedOutput"
Write-Host "Staging: $publishOutput"

New-Item -ItemType Directory -Path $publishOutput -Force | Out-Null

dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishOutput

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$sourceExePath = Join-Path $publishOutput "Words.Windows.exe"
if (!(Test-Path $sourceExePath)) {
    throw "Publish completed but executable was not found at $sourceExePath"
}

$exePath = Join-Path $resolvedOutput "Words.Xbox.exe"
try {
    Copy-Item $sourceExePath $exePath -Force
}
catch [System.IO.IOException] {
    $fallbackName = "Words.Xbox.{0:yyyyMMdd-HHmmss}.exe" -f (Get-Date)
    $exePath = Join-Path $resolvedOutput $fallbackName
    Copy-Item $sourceExePath $exePath -Force
}

Write-Host ""
Write-Host "Xbox-facing graphical demo ready:" -ForegroundColor Green
Write-Host $exePath
Write-Host ""
Write-Host "Launch it with:"
Write-Host ".\scripts\launch-xbox-demo.ps1"
