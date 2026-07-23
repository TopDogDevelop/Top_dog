param(
    [ValidateSet("InstallAndWatch", "WatchOnly", "Devices")]
    [string]$Mode = "InstallAndWatch",
    [string]$ApkPath = "H:\disv1\TopDog.apk",
    [string]$Package = "com.topdogdevelop.topdog",
    [string]$Activity = "com.unity3d.player.UnityPlayerActivity",
    [string]$OutDir = "H:\game_dev\top_dog_unity\builds\android_logs",
    [int]$WatchSeconds = 90
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

function Show-Devices {
    & $adb devices -l
    $lines = & $adb devices | Where-Object { $_ -match "`tdevice$" }
    if (-not $lines) {
        Write-Host @"
No authorized ADB device.
- Phone: enable USB debugging (+ security settings on vivo/iQOO)
- USB mode: File Transfer / MTP, then unlock and Allow this computer
- MTP-only without ADB interface will stay empty
"@
        return $false
    }
    return $true
}

if ($Mode -eq "Devices") {
    [void](Show-Devices)
    exit 0
}

if (-not (Show-Devices)) { exit 2 }

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logPath = Join-Path $OutDir "logcat_$Mode`_$stamp.txt"
$hitPath = Join-Path $OutDir "hits_$Mode`_$stamp.txt"

Write-Host "Clearing logcat..."
& $adb logcat -c | Out-Null

if ($Mode -eq "InstallAndWatch") {
    if (-not (Test-Path $ApkPath)) { throw "APK missing: $ApkPath" }
    Write-Host "Installing $ApkPath ..."
    & $adb install -r $ApkPath
    if ($LASTEXITCODE -ne 0) { throw "adb install failed: $LASTEXITCODE" }
}

Write-Host "Starting $Package/$Activity ..."
& $adb shell am force-stop $Package 2>$null
& $adb shell am start -n "$Package/$Activity"
Start-Sleep -Seconds 2

Write-Host "Watching logcat ${WatchSeconds}s -> $logPath"
$job = Start-Job -ScriptBlock {
    param($Adb, $Out)
    & $Adb logcat -v threadtime *:S Unity:V AndroidRuntime:E DEBUG:E libc:E ActivityManager:I > $Out
} -ArgumentList $adb, $logPath

$deadline = (Get-Date).AddSeconds($WatchSeconds)
while ((Get-Date) -lt $deadline) {
    $pidLine = & $adb shell pidof $Package 2>$null
    if (-not $pidLine -or $pidLine.Trim() -eq "") {
        Write-Host "Process exited early."
        break
    }
    Start-Sleep -Seconds 3
}

Stop-Job $job -ErrorAction SilentlyContinue
Receive-Job $job -ErrorAction SilentlyContinue | Out-Null
Remove-Job $job -Force -ErrorAction SilentlyContinue

# Dump buffered log as well
& $adb logcat -d -v threadtime *:S Unity:V AndroidRuntime:E DEBUG:E libc:E >> $logPath

if (Test-Path $logPath) {
    Select-String -Path $logPath -Pattern "FATAL|AndroidRuntime|Exception|CRASH|SIGSEGV|TopDog|HybridCLR|hotupdate|Missing" |
        Select-Object -Last 120 |
        ForEach-Object { $_.Line } |
        Set-Content -Path $hitPath -Encoding UTF8
}

Write-Host "Wrote:`n  $logPath`n  $hitPath"
& $adb shell pm path $Package 2>&1
