# One-time (or refresh) EVE BUG6-X -> Unity StreamingAssets. Runtime never reads EEVVEE.
$ErrorActionPreference = "Stop"

$ImportScript = "H:\game_dev\top_dog\tools\import_eve_constellation.py"
$OutputMap = "H:\game_dev\top_dog_unity\TopDog.Unity\Assets\StreamingAssets\maps\eve_bug6-x.topdog-map"

if (-not (Test-Path $ImportScript)) {
    Write-Error "Import script not found: $ImportScript"
}

Write-Host "Importing BUG6-X into Unity StreamingAssets..."
python $ImportScript BUG6-X --output $OutputMap
Write-Host "Done. Map packaged at: $OutputMap"
Write-Host "Unity Play Mode reads StreamingAssets only — no EVE data dir required at runtime."
