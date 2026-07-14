param(
    [string]$UnityExe = "",
    [string]$Project = "e:\game_dev\top_dog_unity\TopDog.Unity",
    [string]$OutDir = "e:\game_dev\top_dog_unity\builds\android"
)

$ErrorActionPreference = "Stop"
if (-not $UnityExe) {
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe"
    )
    $UnityExe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $UnityExe -or -not (Test-Path $UnityExe)) {
    throw "Unity.exe not found. Pass -UnityExe"
}

$waitUntil = (Get-Date).AddMinutes(5)
while (Get-Process Unity -ErrorAction SilentlyContinue) {
    if ((Get-Date) -gt $waitUntil) {
        throw "Unity still running after 5m — close Editor / other batch builds first"
    }
    Write-Host "Waiting for other Unity to exit..."
    Start-Sleep 5
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$log = Join-Path $OutDir "build_android.log"
$apk = Join-Path $OutDir "TopDog.apk"
# Stale Gradle export left as a directory named TopDog.apk breaks BuildPlayer
if (Test-Path $apk) {
    Remove-Item -LiteralPath $apk -Recurse -Force
    Write-Host "Removed previous $apk"
}

$method = "TopDog.Editor.BatchBuild.BuildAndroid"
Write-Host "Building Android with $UnityExe"
# Force short path: Cursor sandbox GRADLE_USER_HOME under Temp\cursor-sandbox-cache
# is too long for ninja Stat() and fails the Gradle/IL2CPP link step.
$env:GRADLE_USER_HOME = "C:\g"
New-Item -ItemType Directory -Force -Path $env:GRADLE_USER_HOME | Out-Null
Write-Host "GRADLE_USER_HOME=$env:GRADLE_USER_HOME"
$p = Start-Process -FilePath $UnityExe -ArgumentList @(
    '-quit','-batchmode','-nographics',
    '-projectPath', $Project,
    '-executeMethod', $method,
    '-logFile', $log
) -Wait -PassThru -NoNewWindow
$code = $p.ExitCode
if ($code -ne 0) {
    Write-Host "Unity exit=$code — see $log"
    exit $code
}
if (-not (Test-Path $apk -PathType Leaf)) {
    throw "Android build missing APK file: $apk (see $log) — check exportAsGoogleAndroidProject"
}
Write-Host ("Done. {0} ({1:N1} MB)" -f $apk, ((Get-Item $apk).Length / 1MB))
exit 0
