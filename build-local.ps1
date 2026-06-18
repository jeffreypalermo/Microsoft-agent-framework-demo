#Requires -Version 7
<#
.SYNOPSIS
    Local build + test script for FoundryAgentLocal using Ollama.

.DESCRIPTION
    Builds the solution, writes appsettings.Development.json with the local
    Ollama endpoint, runs all unit tests, then runs the integration test that
    requires Ollama to be running.  Never committed — listed in .gitignore.

.PARAMETER OllamaEndpoint
    Base URL of the local Ollama server.  Default: http://localhost:11434

.PARAMETER Model
    Ollama model tag to use.  Default: gemma4

.PARAMETER SkipIntegration
    Skip the Ollama integration test even if Ollama is reachable.

.PARAMETER WatchFolder
    Folder the worker will watch during the optional smoke-run.
    Defaults to a temp directory.  Pass an absolute path to use your own.
#>
param(
    [string]  $OllamaEndpoint  = 'http://localhost:11434',
    [string]  $Model           = 'gemma4',
    [switch]  $SkipIntegration,
    [string]  $WatchFolder     = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot   = $PSScriptRoot
$WorkerDir  = Join-Path $RepoRoot 'src\FoundryAgentLocal.Worker'
$TestsDir   = Join-Path $RepoRoot 'tests\FoundryAgentLocal.Tests'
$SettingsFile = Join-Path $WorkerDir 'appsettings.Development.json'

# ── helpers ──────────────────────────────────────────────────────────────────

function Write-Step([string]$msg) {
    Write-Host "`n==> $msg" -ForegroundColor Cyan
}

function Write-Ok([string]$msg) {
    Write-Host "    OK  $msg" -ForegroundColor Green
}

function Write-Warn([string]$msg) {
    Write-Host "    WARN  $msg" -ForegroundColor Yellow
}

function Invoke-Checked([string]$cmd) {
    Invoke-Expression $cmd
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $cmd" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# ── 1. Prerequisites ──────────────────────────────────────────────────────────

Write-Step 'Checking prerequisites'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host 'dotnet SDK not found. Install from https://dot.net' -ForegroundColor Red
    exit 1
}
$sdkVer = (dotnet --version)
Write-Ok ".NET SDK $sdkVer"

$ollamaCmd = Get-Command ollama -ErrorAction SilentlyContinue
if (-not $ollamaCmd) {
    Write-Warn 'ollama CLI not found on PATH — integration test will be skipped.'
    $SkipIntegration = $true
}

# ── 2. Check Ollama is running ────────────────────────────────────────────────

$ollamaRunning = $false
if (-not $SkipIntegration) {
    Write-Step "Checking Ollama at $OllamaEndpoint"
    try {
        $resp = Invoke-RestMethod -Uri "$OllamaEndpoint/api/tags" -TimeoutSec 5
        $ollamaRunning = $true
        Write-Ok 'Ollama is running'

        $availableModels = $resp.models | ForEach-Object { $_.name }
        $modelMatch = $availableModels | Where-Object { $_ -like "$Model*" } | Select-Object -First 1

        if ($modelMatch) {
            Write-Ok "Model found: $modelMatch"
            $Model = $modelMatch   # use the exact tag (e.g. gemma4:27b)
        } else {
            Write-Warn "Model '$Model' not found in Ollama. Available: $($availableModels -join ', ')"
            Write-Host "    Run: ollama pull $Model" -ForegroundColor Yellow
            Write-Warn 'Integration test will be skipped.'
            $SkipIntegration = $true
        }
    } catch {
        Write-Warn "Could not reach Ollama ($($_.Exception.Message)). Integration test will be skipped."
        $SkipIntegration = $true
    }
}

# ── 3. Write appsettings.Development.json ────────────────────────────────────

Write-Step "Writing $SettingsFile"

$devSettings = @{
    Ollama = @{
        Endpoint = if ($ollamaRunning) { $OllamaEndpoint } else { '' }
        Model    = $Model
    }
    WatchFolder = @{
        Path = if ($WatchFolder) { $WatchFolder } else { Join-Path ([IO.Path]::GetTempPath()) 'AgentWatch' }
    }
} | ConvertTo-Json -Depth 4

Set-Content -Path $SettingsFile -Value $devSettings -Encoding UTF8
Write-Ok $SettingsFile

# ── 4. Build ──────────────────────────────────────────────────────────────────

Write-Step 'Building solution'
Invoke-Checked "dotnet build `"$RepoRoot\FoundryAgentLocal.sln`" --configuration Release -nologo -v:m"
Write-Ok 'Build succeeded'

# ── 5. Unit tests (no Ollama required) ───────────────────────────────────────

Write-Step 'Running unit tests  (integration tests excluded)'
Invoke-Checked "dotnet test `"$TestsDir`" --configuration Release --no-build --filter `"Category!=Integration`" -nologo --logger `"console;verbosity=normal`""
Write-Ok 'Unit tests passed'

# ── 6. Ollama integration test ────────────────────────────────────────────────

if ($SkipIntegration) {
    Write-Warn 'Skipping integration test (Ollama not available or model not pulled).'
} else {
    Write-Step "Running Ollama integration test  (model: $Model)"
    Invoke-Checked "dotnet test `"$TestsDir`" --configuration Release --no-build --filter `"Category=Integration`" -nologo --logger `"console;verbosity=normal`""
    Write-Ok 'Integration test passed'
}

# ── Done ──────────────────────────────────────────────────────────────────────

Write-Host ''
Write-Host '  Build complete.' -ForegroundColor Green
if (-not $SkipIntegration) {
    Write-Host "  Ollama endpoint : $OllamaEndpoint" -ForegroundColor Green
    Write-Host "  Model           : $Model" -ForegroundColor Green
}
Write-Host ''
Write-Host '  To run the worker against Ollama:' -ForegroundColor DarkCyan
Write-Host "    cd src\FoundryAgentLocal.Worker" -ForegroundColor DarkCyan
Write-Host "    dotnet run" -ForegroundColor DarkCyan
Write-Host ''
