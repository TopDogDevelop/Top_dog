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
Write-Host "Sync Core C# -> Unity Assets"
New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null

Get-ChildItem -Path $SourceRoot -Recurse -Filter "*.cs" | ForEach-Object {
    $rel = $_.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    if ($rel -match '^(obj|bin)[\\/]' -or $rel -match '[\\/](obj|bin)[\\/]') {
        return
    }
    $target = Join-Path $DestRoot $rel
    $dir = Split-Path $target -Parent
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Copy-Item -Force $_.FullName $target
}

Get-ChildItem -Path $DestRoot -Recurse -Filter "*.cs" | ForEach-Object {
    $rel = $_.FullName.Substring($DestRoot.Length).TrimStart('\', '/')
    $source = Join-Path $SourceRoot $rel
    if (-not (Test-Path $source)) {
        Remove-Item -Force $_.FullName
        $meta = $_.FullName + ".meta"
        if (Test-Path $meta) {
            Remove-Item -Force $meta
        }
        Write-Host "Removed orphan $rel"
    }
}

foreach ($junk in @("obj", "bin")) {
    $path = Join-Path $DestRoot $junk
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path
        Write-Host "Removed stray $junk/ from Unity Assets"
    }
}

Write-Host "Done. $(Get-ChildItem $DestRoot -Recurse -Filter '*.cs' | Measure-Object | Select-Object -ExpandProperty Count) files."

$contentSource = Join-Path $RepoRoot "content"
$contentDest = Join-Path $RepoRoot "TopDog.Unity\Assets\StreamingAssets\content"
if (Test-Path $contentSource) {
    Write-Host "Sync authoritative content -> Unity StreamingAssets"
    if (Test-Path $contentDest) {
        Remove-Item -Recurse -Force $contentDest
    }
    New-Item -ItemType Directory -Force -Path (Split-Path $contentDest -Parent) | Out-Null
    Copy-Item -Recurse -Force $contentSource $contentDest
    Write-Host "Content sync done (overlay retained as explicit mode data; not applied over base)."
}
