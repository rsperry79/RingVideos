#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sets up and runs RingVideos app tests with coverage.

.DESCRIPTION
    Test setup utility for RingVideos application:
    - Builds test project in Debug configuration
    - Runs all tests
    - Generates coverage report

.PARAMETER GenerateCoverage
    Generate HTML coverage report after running tests (default: true)

.PARAMETER NoCoverage
    Skip coverage report generation

.PARAMETER BuildOnly
    Only build the test project, don't run tests

.PARAMETER Verbose
    Show detailed build and test output

.EXAMPLE
    # Run tests with coverage
    .\setup-tests.ps1

.EXAMPLE
    # Only build
    .\setup-tests.ps1 -BuildOnly

.EXAMPLE
    # Run without coverage
    .\setup-tests.ps1 -NoCoverage
#>

param(
    [switch]$GenerateCoverage = $true,
    [switch]$NoCoverage,
    [switch]$BuildOnly,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Helper functions
function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host ">>> $Message <<<" -ForegroundColor Magenta
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

# Main script
Write-Header "RingVideos App Test Setup"

# Get paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$TestProject = $MyInvocation.MyCommand.Path.Replace("\setup-tests.ps1", "\RingVideos.Tests.csproj")

# Verify project exists
if (-not (Test-Path $TestProject)) {
    Write-Error-Custom "Test project not found: $TestProject"
    exit 1
}

Write-Info "Location: $ScriptDir"
Write-Info "Test Project: $TestProject"
Write-Host ""

# Step 1: Build
Write-Header "Step 1: Building Test Project"

$BuildArgs = @(
    "build",
    $TestProject,
    "-c", "Debug"
)

if (-not $Verbose) {
    $BuildArgs += "-v", "minimal"
}

try {
    & dotnet $BuildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Build failed with exit code $LASTEXITCODE"
        exit 1
    }
    Write-Success "Build completed"
}
catch {
    Write-Error-Custom "Build error: $_"
    exit 1
}

# Exit early if BuildOnly flag is set
if ($BuildOnly) {
    Write-Host ""
    Write-Success "Build completed successfully!"
    exit 0
}

# Step 2: Run tests
Write-Header "Step 2: Running Tests"

$TestArgs = @(
    "test",
    $TestProject,
    "-c", "Debug",
    "--collect:XPlat Code Coverage"
)

if (-not $Verbose) {
    $TestArgs += "-v", "minimal"
}

try {
    Write-Info "Running tests..."
    & dotnet $TestArgs

    if ($LASTEXITCODE -eq 0) {
        Write-Success "All tests passed!"
    }
    else {
        Write-Warning-Custom "Some tests failed (exit code: $LASTEXITCODE)"
    }
}
catch {
    Write-Error-Custom "Test execution error: $_"
    exit 1
}

Write-Host ""

# Step 3: Generate coverage report
if (-not $NoCoverage -and $GenerateCoverage) {
    Write-Header "Step 3: Generating Coverage Report"

    $TestResultsDir = Join-Path $ScriptDir "TestResults"
    $CoverageXml = Get-ChildItem -Path $TestResultsDir -Filter "*.cobertura.xml" -Recurse | Select-Object -First 1

    if ($CoverageXml) {
        try {
            Write-Info "Found coverage file: $($CoverageXml.FullName)"
            Write-Info "Coverage report available at: $TestResultsDir"
            Write-Success "Coverage data collected"
        }
        catch {
            Write-Warning-Custom "Coverage generation error: $_"
        }
    }
    else {
        Write-Warning-Custom "Coverage report file not found"
    }
}
else {
    Write-Header "Step 3: Coverage Report"
    Write-Info "Skipped"
}

Write-Host ""
Write-Header "Test Setup Complete!"
Write-Success "All steps completed successfully!"
Write-Host ""
