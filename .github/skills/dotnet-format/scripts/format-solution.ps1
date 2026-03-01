#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run dotnet format on a .NET solution with flexible options.

.DESCRIPTION
    Formats C# code in a specified solution, with support for:
    - Full solution formatting
    - Verify-only mode (lint check, no changes)
    - File/project filtering
    - Detailed reporting

.PARAMETER SolutionPath
    Path to the .sln file (relative to workspace root).
    Default: src/VSTSDemoGeneratorV2.sln

.PARAMETER VerifyNoChanges
    If set, verify no formatting changes would be performed (lint mode).

.PARAMETER Include
    Filter to specific files/projects (glob pattern).
    Example: "src/VstsDemoBuilder/**"

.PARAMETER Verbosity
    Logging level: quiet, minimal, normal, detailed, diagnostic (default: normal)

.EXAMPLE
    ./format-solution.ps1 -SolutionPath "src/VSTSDemoGeneratorV2.sln"
    # Format entire solution

.EXAMPLE
    ./format-solution.ps1 -SolutionPath "src/VSTSDemoGeneratorV2.sln" -VerifyNoChanges
    # Check formatting without changes

.EXAMPLE
    ./format-solution.ps1 -SolutionPath "src/VSTSDemoGeneratorV2.sln" -Include "src/VstsDemoBuilder/**"
    # Format only VstsDemoBuilder project
#>

param(
    [string]$SolutionPath = "src/VSTSDemoGeneratorV2.sln",
    [switch]$VerifyNoChanges,
    [string]$Include,
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$Verbosity = "normal"
)

$ErrorActionPreference = "Stop"

# Resolve solution path
$slnAbsPath = Resolve-Path $SolutionPath -ErrorAction Stop

Write-Host "🔧 dotnet format utility runner" -ForegroundColor Blue
Write-Host "Solution: $($slnAbsPath.Path)" -ForegroundColor Gray
Write-Host ""

# Check if dotnet format is installed
$formatVersion = dotnet format --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ dotnet format not found. Install with:" -ForegroundColor Red
    Write-Host "   dotnet tool install -g dotnet-format" -ForegroundColor Yellow
    exit 1
}

Write-Host "ℹ️  dotnet format version: $formatVersion" -ForegroundColor Gray

# Build command
$arguments = @($slnAbsPath.Path)

if ($VerifyNoChanges) {
    Write-Host "Mode: Verify (no changes will be made)" -ForegroundColor Cyan
    $arguments += "--verify-no-changes"
} else {
    Write-Host "Mode: Format (applying changes)" -ForegroundColor Cyan
}

$arguments += "--verbosity"
$arguments += $Verbosity

if ($Include) {
    Write-Host "Filter: $Include" -ForegroundColor Cyan
    $arguments += "--include"
    $arguments += $Include
}

Write-Host ""

# Run dotnet format
Write-Host "Running: dotnet format $($arguments -join ' ')" -ForegroundColor Gray
Write-Host ""

& dotnet format @arguments
$exitCode = $LASTEXITCODE

Write-Host ""

# Report results
if ($exitCode -eq 0) {
    if ($VerifyNoChanges) {
        Write-Host "✅ All files are formatted correctly" -ForegroundColor Green
    } else {
        Write-Host "✅ Formatting complete" -ForegroundColor Green
    }
} else {
    if ($VerifyNoChanges) {
        Write-Host "⚠️  Some files are not formatted" -ForegroundColor Yellow
        Write-Host "Run without --verify-no-changes to apply formatting" -ForegroundColor Yellow
    } else {
        Write-Host "⚠️  Formatting encountered issues" -ForegroundColor Yellow
    }
}

exit $exitCode
