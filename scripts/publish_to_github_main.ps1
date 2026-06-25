param(
    [string]$DevelopRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$MainRoot = "e:\game_dev\top_dog-main"
)

$publish = Join-Path $MainRoot "scripts\publish_from_develop.ps1"
if (-not (Test-Path $publish)) {
    throw "Archive not found: $publish"
}

& $publish -DevelopRoot $DevelopRoot -MainRoot $MainRoot
