param(
    [string]$OutDir = "e:\game_dev\top_dog_unity\builds\android_logs",
    [int]$WaitSeconds = 120
)

$ErrorActionPreference = "Stop"
$adbCandidates = @(
    "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",
    "C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
)
$adb = $adbCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $adb) { throw "adb.exe not found under Unity AndroidPlayer SDK" }
if (-not (Test-Path $adb)) { throw "adb not found: $adb" }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Write-Host "Waiting for device (plug phone, File Transfer, allow USB debugging)..."
$deadline = (Get-Date).AddSeconds($WaitSeconds)
do {
    $lines = & $adb devices | Where-Object { $_ -match "device$" -and $_ -notmatch "List of" }
    if ($lines) { break }
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $deadline)

$devs = & $adb devices -l
Write-Host $devs
if (-not ($devs -match "\tdevice")) {
    throw "No authorized device. Check cable / MTP mode / RSA prompt."
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$raw = Join-Path $OutDir "logcat_$stamp.txt"
$hit = Join-Path $OutDir "crash_hits_$stamp.txt"

Write-Host "Dumping logcat -> $raw"
& $adb logcat -d -v threadtime > $raw
Select-String -Path $raw -Pattern "AndroidRuntime|FATAL EXCEPTION|libc|SIGSEGV|DEBUG|Unity|TopDog|topdog|online-update|content root|OutOfMatch|Exception" |
    Select-Object -Last 200 |
    ForEach-Object { $_.Line } |
    Set-Content -Path $hit -Encoding UTF8

Write-Host "Wrote:`n  $raw`n  $hit"
Write-Host "Package:"
& $adb shell pm path com.topdogdevelop.topdog 2>&1
Write-Host "Done. Reproduce crash then re-run this script, or: adb logcat -c then reproduce then re-run."
