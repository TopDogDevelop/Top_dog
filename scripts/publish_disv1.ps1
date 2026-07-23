param(
    [string]$DevelopRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$Disv1 = "H:\disv1",
    [string]$WinDir = "",
    [string]$ApkPath = "",
    [string]$StubExe = ""
)

<#
.SYNOPSIS
  Sync latest Windows Setup + Android APK into H:\disv1 (exactly two files).

.NOTES
  Run when the user asks to package EXE/APK / 套壳 / 打包到 disv1
  (via build_and_publish_disv1.ps1 or after builds are ready).
  Delivery folder must contain exactly TopDog_Setup.exe + TopDog.apk.
#>

$ErrorActionPreference = "Stop"
if (-not $WinDir) { $WinDir = Join-Path $DevelopRoot "builds\windows" }
if (-not $ApkPath) { $ApkPath = Join-Path $DevelopRoot "builds\android\TopDog.apk" }
if (-not $StubExe) { $StubExe = Join-Path $DevelopRoot "builds\TopDog_Setup_stub.exe" }

if (-not (Test-Path $ApkPath -PathType Leaf)) { throw "APK missing: $ApkPath — build Android first" }
if (-not (Test-Path (Join-Path $WinDir "TopDog.exe") -PathType Leaf)) { throw "Windows build missing under $WinDir" }
if (-not (Test-Path $StubExe)) { throw "SFX stub missing: $StubExe" }

New-Item -ItemType Directory -Force -Path $Disv1 | Out-Null
$stage = Join-Path $DevelopRoot "builds\win_installer_stage"
$zip = Join-Path $DevelopRoot "builds\TopDog_Windows_payload.zip"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Get-ChildItem $WinDir -Force | Where-Object {
    $_.Name -notmatch 'build_windows|\.log$' -and
    $_.Name -notmatch 'BackUpThisFolder_ButDontShipItWithYourGame$' -and
    $_.Name -notmatch '_DoNotShip$' -and
    $_.Name -notmatch 'BurstDebugInformation_DoNotShip$' -and
    # Agility D3D12 sidecar has caused Crash!!! on Intel HD 530 (SSH test box); stock d3d12.dll is enough.
    $_.Name -ne 'D3D12'
} | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $stage $_.Name) -Recurse -Force
}
if (Test-Path $zip) { Remove-Item $zip -Force }
Push-Location $stage
tar -a -cf $zip *
Pop-Location

$tmpSetup = Join-Path $Disv1 "_TopDog_Setup_building.exe"
$outSetup = Join-Path $Disv1 "TopDog_Setup.exe"
$out = [IO.File]::Create($tmpSetup)
try {
    $s = [IO.File]::OpenRead($StubExe)
    try { $s.CopyTo($out) } finally { $s.Close() }
    $mark = [Text.Encoding]::ASCII.GetBytes("###TOPDOG_ZIP_PAYLOAD###")
    $out.Write($mark, 0, $mark.Length)
    $z = [IO.File]::OpenRead($zip)
    try { $z.CopyTo($out) } finally { $z.Close() }
} finally { $out.Close() }

# Clear extras first
Get-ChildItem $Disv1 -Force | Where-Object {
    $_.Name -notin @('_TopDog_Setup_building.exe')
} | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue

Move-Item -Force $tmpSetup $outSetup
Copy-Item -Force $ApkPath (Join-Path $Disv1 "TopDog.apk")

$files = @(Get-ChildItem $Disv1 -File)
$names = @($files | ForEach-Object { $_.Name } | Sort-Object)
$expected = @('TopDog.apk', 'TopDog_Setup.exe')
if ($files.Count -ne 2 -or ($names -join '|') -ne ($expected -join '|')) {
    throw "disv1 must contain exactly TopDog_Setup.exe + TopDog.apk, found: $($files.Name -join ', ')"
}
# Sanity: reject tiny/stale-looking builds
foreach ($f in $files) {
    $mb = $f.Length / 1MB
    if ($mb -lt 50) {
        throw "disv1 $($f.Name) too small ($([math]::Round($mb,1)) MB) — rebuild before publish"
    }
}
Write-Host "disv1 ready:"
$files | ForEach-Object { Write-Host ("  {0}  {1:N1} MB  {2}" -f $_.Name, ($_.Length/1MB), $_.LastWriteTime) }
