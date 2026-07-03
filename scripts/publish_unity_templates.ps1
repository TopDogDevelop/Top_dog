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
    Get-ChildItem -Path $tplSrc -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($tplSrc.Length).TrimStart([char[]]@('\', '/'))
        $dest = Join-Path $tplDst $rel
        $destDir = Split-Path $dest -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        }
        Copy-Item -Force $_.FullName $dest
    }
    $copied = (Get-ChildItem $tplDst -Recurse -File | Where-Object { $_.Extension -eq ".csv" }).Count
    Write-Host "starting_templates -> StreamingAssets ($copied csv files, recursive)"
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

$portraitSrc = Join-Path $Src "member_portrait_templates"
$portraitDst = Join-Path $Dst "member_portrait_templates"
if (Test-Path $portraitSrc) {
    New-Item -ItemType Directory -Force -Path $portraitDst | Out-Null
    Get-ChildItem -Path $portraitSrc -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($portraitSrc.Length).TrimStart([char[]]@('\', '/'))
        $dest = Join-Path $portraitDst $rel
        $destDir = Split-Path $dest -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        }
        Copy-Item -Force $_.FullName $dest
    }
    $imgCount = (Get-ChildItem $portraitDst -Recurse -File | Where-Object {
        $_.Extension -match '^\.(png|jpg|jpeg|webp|bmp|tga)$'
    }).Count
    Write-Host "member_portrait_templates -> StreamingAssets ($imgCount images, recursive)"
}

Write-Host "Unity template overlay done."
