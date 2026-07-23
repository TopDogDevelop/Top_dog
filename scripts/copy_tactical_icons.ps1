param(
    [string]$SourceRoot = "H:\game_dev\icon_for_entity",
    [string]$DestRoot = "H:\game_dev\top_dog_unity\TopDog.Unity\Assets\Art\TacticalIcons"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null

# EVE source name -> TopDog Assets/Art/TacticalIcons name
$copyMap = [ordered]@{
    "ship.png"                   = "frigate_32.png"
    "frigate_32.png"             = "frigate_32.png"
    "destroyer_32.png"           = "destroyer_32.png"
    "cruiser_32.png"             = "cruiser_32.png"
    "battleCruiser_32.png"       = "battleCruiser_32.png"
    "battleship_32.png"          = "battleship_32.png"
    "dreadnought_32.png"         = "dreadnought_32.png"
    "carrier_32.png"             = "carrier_32.png"
    "superCarrier_32.png"        = "superCarrier_32.png"
    "titan_32.png"               = "titan_32.png"
    "shuttle_32.png"             = "shuttle_32.png"
    "droneAttack_16.png"         = "drone_16.png"
    "fighterSquad_16.png"        = "strike_craft_16.png"
    "bomb.png"                   = "missile_16.png"
    "structure.png"              = "structure.png"
    "combatSite_16.png"          = "combatSite_16.png"
    "sun.png"                    = "sun.png"
    "planet.png"                 = "planet.png"
    "moon.png"                   = "moon.png"
    "stargate_32.png"            = "stargate_32.png"
    "station_32.png"             = "station_32.png"
    "station.png"                = "station_32.png"
    "asteroidBelt.png"           = "asteroidBelt.png"
    "cynosuralSystemJammer.png"  = "pirateRally_16.png"
    "beacon.png"                 = "beacon.png"
    "badge_friendly_plus.png"    = "badge_friendly_plus.png"
    "badge_hostile_minus.png"    = "badge_hostile_minus.png"
}

function Find-SourceFile([string]$fileName) {
    $direct = Join-Path $SourceRoot $fileName
    if (Test-Path $direct) { return $direct }
    $hit = Get-ChildItem -Path $SourceRoot -Recurse -Filter $fileName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($hit) { return $hit.FullName }
    return $null
}

function Write-GlyphBadgePng([string]$path, [string]$glyph, [int]$r, [int]$g, [int]$b) {
    Add-Type -AssemblyName System.Drawing
    $size = 16
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    $gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $gfx.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $font = New-Object System.Drawing.Font "Segoe UI", 12, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, $r, $g, $b))
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = New-Object System.Drawing.RectangleF 0, -1, $size, $size
    $gfx.DrawString($glyph, $font, $brush, $rect, $sf)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $gfx.Dispose(); $font.Dispose(); $brush.Dispose(); $sf.Dispose(); $bmp.Dispose()
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

# Standing badges: transparent + / - glyphs (never solid color blocks)
foreach ($badge in @(
        @{ Name = "badge_friendly_plus.png"; Glyph = "+"; R = 80; G = 160; B = 255 },
        @{ Name = "badge_hostile_minus.png"; Glyph = "-"; R = 220; G = 70; B = 70 }
    )) {
    $dest = Join-Path $DestRoot $badge.Name
    if (-not (Test-Path $dest) -or ((Get-Item $dest).Length -lt 200)) {
        Write-GlyphBadgePng $dest $badge.Glyph $badge.R $badge.G $badge.B
        Write-Host "Glyph badge $($badge.Name)"
    }
}

$boardSrc = Find-SourceFile "dreadnought_32.png"
$boardDest = Join-Path $DestRoot "board_dread_wing_32.png"
if ($boardSrc -and -not (Test-Path $boardDest)) {
    Copy-Item -Force $boardSrc $boardDest
    Write-Host "OK  dreadnought_32.png -> board_dread_wing_32.png (fallback)"
}

Write-Host "Done -> $DestRoot ($($destWritten.Count) icons)"
