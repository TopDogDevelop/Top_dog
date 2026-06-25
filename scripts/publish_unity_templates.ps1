# Unity 独占开局模版 + VIP 词条 -> StreamingAssets（libGDX 同步后亦可单独执行）
$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$Src = Join-Path $Root "content"
$Dst = Join-Path $Root "TopDog.Unity\Assets\StreamingAssets\content"
$tplSrc = Join-Path $Src "starting_templates"
$tplDst = Join-Path $Dst "starting_templates"

if (Test-Path $tplSrc) {
    $fixBom = Join-Path $PSScriptRoot "fix_csv_utf8_bom.py"
    if (Test-Path $fixBom) {
        python $fixBom | Out-Null
    }
    New-Item -ItemType Directory -Force -Path $tplDst | Out-Null
    Copy-Item -Force (Join-Path $tplSrc "*") $tplDst
    Write-Host "starting_templates -> StreamingAssets ($((Get-ChildItem $tplSrc -File).Count) files)"
}

$traitPatterns = @(
    "trait_aliases.json",
    "trait_duck_*.json",
    "trait_board_*.json",
    "trait_commander_*.json",
    "trait_discord_*.json",
    "trait_planning_*.json",
    "trait_fool_*.json",
    "trait_devotion.json",
    "trait_recruit_*.json",
    "trait_rookie_*.json",
    "trait_immovable.json"
)
$traitDst = Join-Path $Dst "traits"
New-Item -ItemType Directory -Force -Path $traitDst | Out-Null
foreach ($pattern in $traitPatterns) {
    Get-ChildItem (Join-Path $Src "traits\$pattern") -ErrorAction SilentlyContinue |
        Copy-Item -Destination $traitDst -Force
}

Write-Host "Unity template overlay done."
