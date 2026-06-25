param(
    [string]$SourceRoot = "e:\game_dev\icon_for_entity",
    [string]$DestRoot = "e:\game_dev\top_dog_unity\TopDog.Unity\Assets\Art\TacticalIcons"
)

$ErrorActionPreference = "Stop"
$names = @(
    "frigate_32.png", "battleship_32.png", "dreadnought_32.png", "carrier_32.png",
    "superCarrier_32.png", "titan_32.png", "structure.png",
    "sun.png", "planet.png", "stargate_32.png", "asteroidBelt.png",
    "combatSite_16.png", "beacon.png",
    "badge_friendly_plus.png", "badge_hostile_minus.png"
)
New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null

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

foreach ($n in $names) {
    $dest = Join-Path $DestRoot $n
    $src = Get-ChildItem -Path $SourceRoot -Recurse -Filter $n -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($src) {
        Copy-Item -Force $src.FullName $dest
        Write-Host "Copied $n"
    } elseif ($n -eq "badge_friendly_plus.png") {
        Write-PlaceholderPng $dest 80 160 255
        Write-Host "Placeholder $n (blue)"
    } elseif ($n -eq "badge_hostile_minus.png") {
        Write-PlaceholderPng $dest 220 70 70
        Write-Host "Placeholder $n (red)"
    } else {
        Write-Warning "Missing $n under $SourceRoot"
    }
}
Write-Host "Done -> $DestRoot"
