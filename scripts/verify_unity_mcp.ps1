$ErrorActionPreference = "Continue"
$project = "e:\game_dev\top_dog_unity\TopDog.Unity"
$mcpJson = Join-Path $env:USERPROFILE ".cursor\mcp.json"
$editor = "C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe"
$endpoint = "http://localhost:8080/"

Write-Host "=== Unity Community MCP preflight ===" -ForegroundColor Cyan

$wrongProject = "e:\game_dev\top_dog_unity"
if (Test-Path $project) { Write-Host "[OK] project: $project" -ForegroundColor Green }
else { Write-Host "[FAIL] project path missing" -ForegroundColor Red }

if ((Get-ChildItem "$wrongProject\Assets" -Filter *.cs -Recurse -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0 `
    -and (Get-ChildItem "$project\Assets" -Filter *.cs -Recurse -ErrorAction SilentlyContinue | Measure-Object).Count -gt 0) {
    Write-Host "[!!] Hub 可能打开了外层 $wrongProject — 请改开 $project" -ForegroundColor Yellow
}

$manifest = Join-Path $project "Packages\com.emeryporter.unitymcp\package.json"
if (Test-Path $manifest) {
    Write-Host "[OK] embedded package: com.emeryporter.unitymcp" -ForegroundColor Green
}
else { Write-Host "[!!] Missing Packages/com.emeryporter.unitymcp (embed from GitHub release zip)" -ForegroundColor Yellow }

if ((Test-Path $mcpJson) -and ((Get-Content $mcpJson -Raw) -match "unity-community")) {
    Write-Host "[OK] Cursor mcp.json has unity-community" -ForegroundColor Green
}
else { Write-Host "[!!] Check $mcpJson for unity-community url entry" -ForegroundColor Yellow }

if (Test-Path $editor) { Write-Host "[OK] Unity 6000.5.0f1 installed" -ForegroundColor Green }
else { Write-Host "[!!] Unity 6000.5.0f1 not found" -ForegroundColor Yellow }

try {
    $init = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"verify","version":"1.0"}}}'
    $resp = Invoke-WebRequest -Uri $endpoint -Method POST -Body $init -ContentType "application/json" -TimeoutSec 5 -UseBasicParsing
    $name = ($resp.Content | ConvertFrom-Json).result.serverInfo.name
    Write-Host "[OK] HTTP MCP server responding on $endpoint (server: $name)" -ForegroundColor Green
}
catch {
    Write-Host "[!!] HTTP server not reachable at $endpoint" -ForegroundColor Yellow
    Write-Host "     Open Unity -> Window -> Unity MCP -> Start Server" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Cursor: use scripts/unity_mcp_stdio_bridge.mjs (node), then restart Cursor"
Write-Host "Next: Unity Start Server -> restart Cursor -> Settings MCP should show ~50 tools"
