param(
    [string]$UnityExe = "",
    [string]$DevelopRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$Disv1 = "H:\disv1",
    [switch]$SkipWindows,
    [switch]$SkipAndroid
)

<#
.SYNOPSIS
  Build Windows + Android shells, then publish exactly two files into H:\disv1.

.NOTES
  Run when the user explicitly asks to package EXE/APK / 套壳 / 打包到 disv1.
  Intermediate artifacts stay under builds\; delivery folder only TopDog_Setup.exe + TopDog.apk.
#>

$ErrorActionPreference = "Stop"
$scripts = $PSScriptRoot

Write-Host "=== TopDog shell -> $Disv1 (exactly 2 files) ==="

if (-not $SkipWindows) {
    Write-Host "`n--- Windows ---"
    & (Join-Path $scripts "build_windows.ps1") -UnityExe $UnityExe
    if ($LASTEXITCODE -ne 0) {
        throw "Windows build failed (exit=$LASTEXITCODE)"
    }
    $exe = Join-Path $DevelopRoot "builds\windows\TopDog.exe"
    if (-not (Test-Path $exe -PathType Leaf)) { throw "Windows build missing: $exe" }
    Write-Host ("Windows ok: {0}" -f (Get-Item $exe).LastWriteTime)
}

if (-not $SkipAndroid) {
    Write-Host "`n--- Android ---"
    $env:GRADLE_USER_HOME = "C:\g"
    & (Join-Path $scripts "build_android.ps1") -UnityExe $UnityExe
    if ($LASTEXITCODE -ne 0) {
        throw "Android build failed (exit=$LASTEXITCODE)"
    }
    $apk = Join-Path $DevelopRoot "builds\android\TopDog.apk"
    if (-not (Test-Path $apk -PathType Leaf)) { throw "Android build missing APK file: $apk" }
    Write-Host ("Android ok: {0}" -f (Get-Item $apk).LastWriteTime)
}

Write-Host "`n--- publish disv1 ---"
& (Join-Path $scripts "publish_disv1.ps1") -DevelopRoot $DevelopRoot -Disv1 $Disv1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nDone. disv1:"
Get-ChildItem $Disv1 -Force | ForEach-Object {
    Write-Host ("  {0}  {1:N1} MB  {2}" -f $_.Name, ($_.Length / 1MB), $_.LastWriteTime)
}
