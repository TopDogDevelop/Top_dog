param(
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$SourceRoot,
    [string]$DestRoot
)

if (-not $SourceRoot) {
    $SourceRoot = Join-Path $RepoRoot "src\TopDog.Core"
}
if (-not $DestRoot) {
    $DestRoot = Join-Path $RepoRoot "TopDog.Unity\Assets\Scripts\Core"
}

$ErrorActionPreference = "Stop"
$srcFiles = Get-ChildItem -Path $SourceRoot -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' }
$missing = @()
$stale = @()

foreach ($src in $srcFiles) {
    $rel = $src.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    $dest = Join-Path $DestRoot $rel
    if (-not (Test-Path $dest)) {
        $missing += $rel
        continue
    }
    $srcHash = (Get-FileHash $src.FullName -Algorithm SHA256).Hash
    $destHash = (Get-FileHash $dest -Algorithm SHA256).Hash
    if ($srcHash -ne $destHash) {
        $stale += $rel
    }
}

if ($missing.Count -gt 0 -or $stale.Count -gt 0) {
    Write-Host "Core mirror drift detected."
    if ($missing.Count -gt 0) {
        Write-Host "Missing in Unity mirror ($($missing.Count)):"
        $missing | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
    }
    if ($stale.Count -gt 0) {
        Write-Host "Out of sync ($($stale.Count)):"
        $stale | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
    }
    Write-Host "Run: scripts\sync_core_to_unity.ps1"
    exit 1
}

Write-Host "Core mirror OK ($($srcFiles.Count) files)."
exit 0
