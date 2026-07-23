param(
    [string]$Version = "",
    [string]$DevelopRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$UpdateRepo = "H:\game_dev\topdog_online_update",
    [string]$RemoteUrl = "https://github.com/TopDogDevelop/topdog_online_update.git",
    [string]$HfBucket = "hf://buckets/liketocode789/topdog_online_update_data",
    [string]$HfResolveBaseUrl = "https://huggingface.co/buckets/liketocode789/topdog_online_update_data/resolve/",
    [string]$HfBucketPage = "https://huggingface.co/buckets/liketocode789/topdog_online_update_data",
    # Hotfix only — content/art/maps/audio stay shared (never platform-split)
# Layout: 倒Y — see docs/RELEASE_AND_HOTUPDATE.md §1.7
    [ValidateSet("android", "windows-x64", "all")]
    [string]$Platform = "all",
    [string]$ShellCompatibilityId = "topdog-unity-6000.3.19f1-hc8.12",
    [switch]$Push,
    [switch]$PushGitHub,
    [switch]$SkipClone,
    [switch]$SkipHf
)

$ErrorActionPreference = "Stop"

function Resolve-Git {
    $cmd = Get-Command git -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($p in @(
        "C:\Program Files\Git\bin\git.exe",
        "C:\Program Files (x86)\Git\bin\git.exe"
    )) {
        if (Test-Path $p) { return $p }
    }
    throw "git.exe not found"
}

$GitExe = Resolve-Git
function Git { & $GitExe @args }

function Get-FileSha256([string]$Path) {
    $hash = Get-FileHash -Algorithm SHA256 -Path $Path
    return $hash.Hash.ToLowerInvariant()
}

$staging = Join-Path $env:TEMP "topdog_online_update_publish"
if (Test-Path $staging) {
    Remove-Item -Recurse -Force $staging
}
New-Item -ItemType Directory -Force -Path $staging | Out-Null

if (-not $Version) {
    $now = Get-Date
    $prefix = "{0}.{1}." -f $now.ToString("yyyyMM"), $now.Day
    $Version = $prefix + "1"
    $hintPath = Join-Path $UpdateRepo "version.json"
    if (Test-Path $hintPath) {
        $existing = Get-Content $hintPath -Raw | ConvertFrom-Json
        if ($existing.version -like ($prefix + "*") -and $existing.version -match ('^' + [regex]::Escape($prefix) + '(\d{1,3})$')) {
            $n = [int]$Matches[1] + 1
            if ($n -le 999) {
                $Version = $prefix + "$n"
            }
        }
    }
}

Write-Host "Publishing online update version=$Version"
Write-Host "HF bucket=$HfBucket"
Write-Host "HF resolve=$HfResolveBaseUrl"

$contentSrc = Join-Path $DevelopRoot "content"
$contentDst = Join-Path $staging "content"
$includeDirs = @(
    "balance", "traits", "ships", "modules", "starting_templates",
    "starting_assets", "banter", "map", "skirmish_overlay", "mechanism_tests"
)

New-Item -ItemType Directory -Force -Path $contentDst | Out-Null
foreach ($d in $includeDirs) {
    $src = Join-Path $contentSrc $d
    if (Test-Path $src) {
        $dst = Join-Path $contentDst $d
        New-Item -ItemType Directory -Force -Path $dst | Out-Null
        Copy-Item -Recurse -Force (Join-Path $src "*") $dst
        Write-Host "Staged content/$d"
    }
}

# Player art (tactical icons + Main combat skyboxes) — consumed via ClientArtPaths
# under content_runtime/art after hot-update (and StreamingAssets/art in next shell).
$artSrcRoot = Join-Path $DevelopRoot "TopDog.Unity\Assets\Art"
$artDstRoot = Join-Path $staging "art"
if (Test-Path $artSrcRoot) {
    $iconSrc = Join-Path $artSrcRoot "TacticalIcons"
    $iconDst = Join-Path $artDstRoot "tactical_icons"
    if (Test-Path $iconSrc) {
        New-Item -ItemType Directory -Force -Path $iconDst | Out-Null
        Get-ChildItem $iconSrc -File -Filter *.png | ForEach-Object {
            Copy-Item -Force $_.FullName (Join-Path $iconDst $_.Name)
        }
        Write-Host "Staged art/tactical_icons"
    }
    $bgSrc = Join-Path $artSrcRoot "CombatBackgrounds"
    $bgDst = Join-Path $artDstRoot "combat_backgrounds"
    if (Test-Path $bgSrc) {
        New-Item -ItemType Directory -Force -Path $bgDst | Out-Null
        $mainSrc = Join-Path $bgSrc "Main"
        if (Test-Path $mainSrc) {
            $mainDst = Join-Path $bgDst "Main"
            New-Item -ItemType Directory -Force -Path $mainDst | Out-Null
            Copy-Item -Recurse -Force (Join-Path $mainSrc "*") $mainDst
            # Drop Unity .meta from hot-update payload
            Get-ChildItem $mainDst -Recurse -Filter *.meta | Remove-Item -Force
            Write-Host "Staged art/combat_backgrounds/Main"
        }
        $man = Join-Path $bgSrc "manifest.json"
        if (Test-Path $man) {
            Copy-Item -Force $man (Join-Path $bgDst "manifest.json")
        }
    }
} else {
    Write-Host "WARN: Art source missing: $artSrcRoot"
}

# Packaged maps (e.g. eve_bug6-x.topdog-map) — CustomLobby ListMaps reads AppRoot/maps
# After hot-update AppRoot is content_runtime, so maps MUST be in the HF bucket.
$mapsSrcCandidates = @(
    (Join-Path $DevelopRoot "TopDog.Unity\Assets\StreamingAssets\maps"),
    (Join-Path $DevelopRoot "maps")
)
$mapsDst = Join-Path $staging "maps"
$mapsCopied = $false
foreach ($mapsSrc in $mapsSrcCandidates) {
    if (-not (Test-Path $mapsSrc)) { continue }
    New-Item -ItemType Directory -Force -Path $mapsDst | Out-Null
    Get-ChildItem $mapsSrc -Directory | Where-Object { $_.Name -like "*.topdog-map" } | ForEach-Object {
        $dst = Join-Path $mapsDst $_.Name
        if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
        Copy-Item -Recurse -Force $_.FullName $dst
        Get-ChildItem $dst -Recurse -Filter *.meta -ErrorAction SilentlyContinue | Remove-Item -Force
        Write-Host "Staged maps/$($_.Name)"
        $mapsCopied = $true
    }
    if ($mapsCopied) { break }
}
if (-not $mapsCopied) {
    Write-Host "WARN: no *.topdog-map found under StreamingAssets/maps or maps/"
}

# HybridCLR hot assemblies — platform-split under hotupdate/<platform>/
# content/art/maps above remain shared (do not put under platform folders).
$hotDllName = "TopDog.Hot.dll"
$platformSpecs = @()
if ($Platform -eq "all" -or $Platform -eq "windows-x64") {
    $platformSpecs += [pscustomobject]@{
        Id = "windows-x64"
        HotDllRoot = Join-Path $DevelopRoot "TopDog.Unity\HybridCLRData\HotUpdateDlls\StandaloneWindows64"
        AotStripFolder = "StandaloneWindows64"
    }
}
if ($Platform -eq "all" -or $Platform -eq "android") {
    $platformSpecs += [pscustomobject]@{
        Id = "android"
        HotDllRoot = Join-Path $DevelopRoot "TopDog.Unity\HybridCLRData\HotUpdateDlls\Android"
        AotStripFolder = "Android"
    }
}

$aotSrcRoot = Join-Path $DevelopRoot "TopDog.Unity\HybridCLRData\AssembliesPostIl2CppStrip"
$aotNames = @("mscorlib.dll","System.dll","System.Core.dll","TopDog.Core.dll","TopDog.Client.dll")

foreach ($spec in $platformSpecs) {
    $hotDst = Join-Path $staging "hotupdate\$($spec.Id)"
    New-Item -ItemType Directory -Force -Path $hotDst | Out-Null
    $candidate = Join-Path $spec.HotDllRoot $hotDllName
    if (Test-Path $candidate) {
        Copy-Item -Force $candidate (Join-Path $hotDst $hotDllName)
        Write-Host "Staged hotupdate/$($spec.Id)/$hotDllName from $($spec.HotDllRoot)"
        $pdb = $candidate + ".pdb"
        if (Test-Path $pdb) {
            Copy-Item -Force $pdb (Join-Path $hotDst ($hotDllName + ".pdb"))
        }
    }
    else {
        Write-Host "WARN: $hotDllName not found for $($spec.Id) under $($spec.HotDllRoot)"
    }

    [System.IO.File]::WriteAllText(
        (Join-Path $hotDst "shellCompatibilityId.txt"),
        $ShellCompatibilityId,
        [System.Text.UTF8Encoding]::new($false))
    Write-Host "Staged hotupdate/$($spec.Id)/shellCompatibilityId.txt = $ShellCompatibilityId"

    $aotPlatformSrc = Join-Path $aotSrcRoot $spec.AotStripFolder
    $aotDst = Join-Path $hotDst "aot"
    if (Test-Path $aotPlatformSrc) {
        New-Item -ItemType Directory -Force -Path $aotDst | Out-Null
        foreach ($name in $aotNames) {
            $found = Get-ChildItem -Path $aotPlatformSrc -Recurse -Filter $name -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($found) {
                Copy-Item -Force $found.FullName (Join-Path $aotDst $name)
                Write-Host "Staged hotupdate/$($spec.Id)/aot/$name"
            }
        }
    }
    else {
        Write-Host "WARN: AOT strip folder missing for $($spec.Id): $aotPlatformSrc"
    }
}

$files = @()
Get-ChildItem -Path $staging -Recurse -File | Where-Object {
    $_.Name -notin @('version.json','manifest.json')
} | ForEach-Object {
    $rel = $_.FullName.Substring($staging.Length).TrimStart('\', '/').Replace('\', '/')
    $files += [ordered]@{
        path   = $rel
        sha256 = Get-FileSha256 $_.FullName
        size   = $_.Length
    }
}

$publishedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssK")
$baseUrl = $HfResolveBaseUrl
if (-not $baseUrl.EndsWith('/')) { $baseUrl += '/' }

$versionJson = @"
{
  "version": "$Version",
  "publishedAt": "$publishedAt",
  "baseUrl": "$baseUrl",
  "shellCompatibilityId": "$ShellCompatibilityId",
  "hotfixPlatforms": [$(($platformSpecs | ForEach-Object { '"' + $_.Id + '"' }) -join ', ')],
  "notes": "TopDog shared content + platform-split HybridCLR hotupdate (HF bucket)"
}
"@
[System.IO.File]::WriteAllText((Join-Path $staging "version.json"), $versionJson.Replace("`r`n", "`n"), [System.Text.UTF8Encoding]::new($false))

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("{")
[void]$sb.AppendLine("  `"version`": `"$Version`",")
[void]$sb.AppendLine("  `"files`": [")
for ($i = 0; $i -lt $files.Count; $i++) {
    $f = $files[$i]
    $comma = if ($i -lt $files.Count - 1) { "," } else { "" }
    [void]$sb.AppendLine("    { `"path`": `"$($f.path)`", `"sha256`": `"$($f.sha256)`", `"size`": $($f.size) }$comma")
}
[void]$sb.AppendLine("  ]")
[void]$sb.AppendLine("}")
[System.IO.File]::WriteAllText((Join-Path $staging "manifest.json"), $sb.ToString(), [System.Text.UTF8Encoding]::new($false))

if (-not $SkipHf) {
    $hf = Get-Command hf -ErrorAction SilentlyContinue
    if (-not $hf) { throw "hf CLI not found. Install huggingface_hub CLI or pass -SkipHf." }
    Write-Host "Syncing staging -> $HfBucket (--delete)..."
    & hf buckets sync $staging $HfBucket --delete
    if ($LASTEXITCODE -ne 0) { throw "hf buckets sync failed (exit $LASTEXITCODE)" }
}

# --- GitHub pointer (rare): static UTF-8 templates, no Chinese embedded in this .ps1 ---
# Clients resolve HF directly; GitHub is human navigation only. Default -Push skips GitHub.
if ($PushGitHub) {
    if (-not (Test-Path $UpdateRepo)) {
        New-Item -ItemType Directory -Force -Path (Split-Path $UpdateRepo -Parent) | Out-Null
        if (-not $SkipClone) {
            Git clone $RemoteUrl $UpdateRepo
        }
        else {
            New-Item -ItemType Directory -Force -Path $UpdateRepo | Out-Null
        }
    }

    $contentDead = Join-Path $UpdateRepo "content"
    if (Test-Path $contentDead) {
        Remove-Item -Recurse -Force $contentDead
    }
    $manifestDead = Join-Path $UpdateRepo "manifest.json"
    if (Test-Path $manifestDead) {
        Remove-Item -Force $manifestDead
    }

    $tplDir = Join-Path $PSScriptRoot "online_update_github"
    foreach ($name in @("README.md", "PROTOCOL.md", "index.html")) {
        $src = Join-Path $tplDir $name
        if (-not (Test-Path $src)) { throw "Missing GitHub template: $src" }
        Copy-Item -Force $src (Join-Path $UpdateRepo $name)
    }

    [System.IO.File]::WriteAllText(
        (Join-Path $UpdateRepo "version.json"),
        $versionJson.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false))

    Push-Location $UpdateRepo
    try {
        if (-not (Test-Path ".git")) {
            Git init
            Git remote add origin $RemoteUrl
        }
        Git add -A
        Git add -u
        Git status
        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        Git -c user.email="topdog-publish@local" -c user.name="TopDog Publish" `
            -c core.safecrlf=false `
            commit -m "Refresh HF pointer; version $Version" 2>&1 | Out-Host
        $ErrorActionPreference = $prevEap
        if ($Push -or $PushGitHub) {
            Git branch -M main
            Git push -u origin main
            Write-Host "Pushed pointer repo $Version to $RemoteUrl"
        }
        else {
            Write-Host "GitHub commit ready locally (pass -Push with -PushGitHub to upload)."
        }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "Skipped GitHub pointer repo (pass -PushGitHub when you want a rare refresh)."
}

Write-Host "Done. files=$($files.Count) version=$Version hf=$HfBucket github=$UpdateRepo"
