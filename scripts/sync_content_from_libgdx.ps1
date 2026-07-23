param(
    [string]$SourceRoot = "H:\game_dev\top_dog",
    [string]$DestRoot = "H:\game_dev\top_dog_unity"
)

$ErrorActionPreference = "Stop"
$src = Join-Path $SourceRoot "content"
$dst = Join-Path $DestRoot "content"
$streaming = Join-Path $DestRoot "TopDog.Unity\Assets\StreamingAssets\content"
$preserveDir = Join-Path $env:TEMP "topdog_unity_starting_templates_preserve"
$unityTemplates = Join-Path $dst "starting_templates"
$skirmishOverlay = Join-Path $DestRoot "content\skirmish_overlay"
$skirmishOverlayPreserve = Join-Path $env:TEMP "topdog_unity_skirmish_overlay_preserve"

# Unity 仓独占：约战/首包扩展内容（libGDX 同步后 overlay 回写）
if (Test-Path $skirmishOverlay) {
    if (Test-Path $skirmishOverlayPreserve) { Remove-Item -Recurse -Force $skirmishOverlayPreserve }
    New-Item -ItemType Directory -Force -Path $skirmishOverlayPreserve | Out-Null
    Copy-Item -Recurse -Force $skirmishOverlay $skirmishOverlayPreserve
    Write-Host "Preserved skirmish_overlay -> $skirmishOverlayPreserve"
}

# Unity 仓独占：开局团员模版 CSV（libGDX 同步不覆盖）
if (Test-Path $unityTemplates) {
    if (Test-Path $preserveDir) { Remove-Item -Recurse -Force $preserveDir }
    New-Item -ItemType Directory -Force -Path $preserveDir | Out-Null
    Copy-Item -Recurse -Force $unityTemplates $preserveDir
    Write-Host "Preserved Unity starting_templates -> $preserveDir"
}

Write-Host "Sync content: $src -> $dst"
if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
Copy-Item -Recurse -Force $src $dst

if (Test-Path $preserveDir) {
    $restore = Join-Path $dst "starting_templates"
    New-Item -ItemType Directory -Force -Path $restore | Out-Null
    Copy-Item -Recurse -Force (Join-Path $preserveDir "*") $restore
    Write-Host "Restored Unity starting_templates"
}

if (Test-Path $skirmishOverlayPreserve) {
    $restoreOverlay = Join-Path $dst "skirmish_overlay"
    Copy-Item -Recurse -Force $skirmishOverlayPreserve $restoreOverlay
    Write-Host "Restored skirmish_overlay as isolated mode data"
}

Write-Host "Sync authoritative content: $dst -> $streaming"
New-Item -ItemType Directory -Force -Path (Split-Path $streaming) | Out-Null
if (Test-Path $streaming) { Remove-Item -Recurse -Force $streaming }
Copy-Item -Recurse -Force $dst $streaming

$unityStreamingTemplates = Join-Path $streaming "starting_templates"
if (Test-Path $unityTemplates) {
    New-Item -ItemType Directory -Force -Path $unityStreamingTemplates | Out-Null
    Copy-Item -Force (Join-Path $unityTemplates "*") $unityStreamingTemplates
    Write-Host "Overlay Unity starting_templates -> StreamingAssets"
}

& (Join-Path $PSScriptRoot "publish_unity_templates.ps1")

Write-Host "Done."
