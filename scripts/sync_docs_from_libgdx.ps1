param(
    [string]$SourceRoot = "e:\game_dev\top_dog",
    [string]$DestRoot = "e:\game_dev\top_dog_unity\docs"
)

$ErrorActionPreference = "Stop"
$src = Join-Path $SourceRoot "docs"
Write-Host "Sync docs: $src -> $DestRoot"
if (Test-Path $DestRoot) { Remove-Item -Recurse -Force $DestRoot }
Copy-Item -Recurse -Force $src $DestRoot
Write-Host "Done. Remember to re-apply Unity-specific doc edits."
