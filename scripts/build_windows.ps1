param(
    [string]$UnityExe = "",
    [string]$Project = "e:\game_dev\top_dog_unity\TopDog.Unity",
    [string]$OutDir = "e:\game_dev\top_dog_unity\builds\windows"
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

# Avoid "another Unity instance is running"
$waitUntil = (Get-Date).AddMinutes(5)
while (Get-Process Unity -ErrorAction SilentlyContinue) {
    if ((Get-Date) -gt $waitUntil) {
        throw "Unity still running after 5m — close Editor / other batch builds first"
    }
    Write-Host "Waiting for other Unity to exit..."
    Start-Sleep 5
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$log = Join-Path $OutDir "build_windows.log"
$exe = Join-Path $OutDir "TopDog.exe"
$method = "TopDog.Editor.BatchBuild.BuildWindows"
Write-Host "Building Windows with $UnityExe"
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
if (-not (Test-Path $exe -PathType Leaf)) {
    throw "Windows build missing output: $exe (see $log)"
}
Write-Host ("Done. {0} ({1:N1} MB)" -f $exe, ((Get-Item $exe).Length / 1MB))
exit 0
