# 第二银河主宇宙天空盒 → TopDog 实时交战背景（Main 随机 / Reserve 备用）
param(
    [string]$DestRoot = "e:\game_dev\top_dog_unity\TopDog.Unity\Assets\Art\CombatBackgrounds"
)

$ErrorActionPreference = "Stop"
python (Join-Path $PSScriptRoot "build_combat_backgrounds.py")
if ($LASTEXITCODE -ne 0) {
    throw "build_combat_backgrounds.py failed with exit code $LASTEXITCODE"
}
Write-Host "Combat backgrounds ready at $DestRoot"
