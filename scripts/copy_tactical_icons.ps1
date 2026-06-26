param(
    [string]$SourceRoot = "e:\game_dev\icon_for_entity",
    [string]$DestRoot = "e:\game_dev\top_dog_unity\TopDog.Unity\Assets\Art\TacticalIcons"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null

# EVE 源文件名 → TopDog Assets/Art/TacticalIcons 权威文件名
$copyMap = [ordered]@{
    # 舰型（优先 *_32；护卫用 ship 三角标）
    "ship.png"              = "frigate_32.png"
    "frigate_32.png"        = "frigate_32.png"
    "destroyer_32.png"      = "destroyer_32.png"
    "cruiser_32.png"        = "cruiser_32.png"
    "battleCruiser_32.png"  = "battleCruiser_32.png"
    "battleship_32.png"     = "battleship_32.png"
    "dreadnought_32.png"    = "dreadnought_32.png"
    "carrier_32.png"        = "carrier_32.png"
    "superCarrier_32.png"   = "superCarrier_32.png"
    "titan_32.png"          = "titan_32.png"
    "shuttle_32.png"        = "shuttle_32.png"
    # 无人单位
    "droneAttack_16.png"    = "drone_16.png"
    "fighterSquad_16.png"   = "strike_craft_16.png"
    "bomb.png"              = "missile_16.png"
    # 建筑 / 复合体
    "structure.png"         = "structure.png"
    "combatSite_16.png"     = "combatSite_16.png"
    # 地标 / 天体（planet.png 优先于 agentInSpace 占位）
    "sun.png"               = "sun.png"
    "planet.png"            = "planet.png"
    "moon.png"              = "moon.png"
    "stargate_32.png"       = "stargate_32.png"
    "station_32.png"        = "station_32.png"
    "station.png"           = "station_32.png"
    "asteroidBelt.png"      = "asteroidBelt.png"
    "cynosuralSystemJammer.png" = "pirateRally_16.png"
    "beacon.png"            = "beacon.png"
    # 角标 / 特殊
    "badge_friendly_plus.png" = "badge_friendly_plus.png"
    "badge_hostile_minus.png" = "badge_hostile_minus.png"
}

function Find-SourceFile([string]$fileName) {
    $direct = Join-Path $SourceRoot $fileName
    if (Test-Path $direct) { return $direct }
    $hit = Get-ChildItem -Path $SourceRoot -Recurse -Filter $fileName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($hit) { return $hit.FullName }
    return $null
}

function Write-PlaceholderPng([string]$path, [byte]$r, [byte]$g, [byte]$b) {
    Add-Type -AssemblyName System.Drawing
    $bmp = New-Object System.Drawing.Bitmap 8, 8
    for ($x = 0; $x -lt 8; $x++) {
        for ($y = 0; $y -lt 8; $y++) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $r, $g, $b))
        }
    }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$destWritten = @{}
foreach ($entry in $copyMap.GetEnumerator()) {
    $srcName = $entry.Key
    $destName = $entry.Value
    if ($destWritten.ContainsKey($destName)) {
        continue
    }
    $dest = Join-Path $DestRoot $destName
    $src = Find-SourceFile $srcName
    if ($src) {
        Copy-Item -Force $src $dest
        Write-Host "OK  $srcName -> $destName"
        $destWritten[$destName] = $true
    }
}

# 角标占位（源缺失时）
foreach ($badge in @(
        @{ Name = "badge_friendly_plus.png"; R = 80; G = 160; B = 255 },
        @{ Name = "badge_hostile_minus.png"; R = 220; G = 70; B = 70 }
    )) {
    $dest = Join-Path $DestRoot $badge.Name
    if (-not (Test-Path $dest)) {
        Write-PlaceholderPng $dest $badge.R $badge.G $badge.B
        Write-Host "Placeholder $($badge.Name)"
    }
}

# 董事会增援翼（若源无则跳过；Client 有 board_dread_wing_32 引用）
$boardSrc = Find-SourceFile "dreadnought_32.png"
$boardDest = Join-Path $DestRoot "board_dread_wing_32.png"
if ($boardSrc -and -not (Test-Path $boardDest)) {
    Copy-Item -Force $boardSrc $boardDest
    Write-Host "OK  dreadnought_32.png -> board_dread_wing_32.png (fallback)"
}

Write-Host "Done -> $DestRoot ($($destWritten.Count) icons)"
