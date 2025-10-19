#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verify preparation cache against configuration
.DESCRIPTION
    Compares the actual build/preparation/cache contents with the expected items from preparation.json
.PARAMETER ConfigPath
    Path to the preparation config file (default: build/preparation/configs/preparation.json)
.EXAMPLE
    .\scripts\verify-preparation-cache.ps1
.EXAMPLE
    .\scripts\verify-preparation-cache.ps1 -ConfigPath build/preparation/configs/preparation.json
#>

[CmdletBinding()]
param(
    [string]$ConfigPath = "build/preparation/configs/preparation.json"
)

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Preparation Cache Verification" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Load configuration
if (-not (Test-Path $ConfigPath)) {
    Write-Host "❌ Configuration file not found: $ConfigPath" -ForegroundColor Red
    exit 1
}

Write-Host "📄 Loading configuration: $ConfigPath" -ForegroundColor Yellow
$config = Get-Content $ConfigPath | ConvertFrom-Json

# Count expected items
$expectedPackages = $config.packages.Count
$expectedAssemblies = $config.assemblies.Count
$expectedTotal = $expectedPackages + $expectedAssemblies

Write-Host "📊 Expected items:" -ForegroundColor Yellow
Write-Host "   - Packages:   $expectedPackages" -ForegroundColor Gray
Write-Host "   - Assemblies: $expectedAssemblies" -ForegroundColor Gray
Write-Host "   - Total:      $expectedTotal" -ForegroundColor Gray
Write-Host ""

# Check cache directory
$cacheDir = "build/preparation/cache"
if (-not (Test-Path $cacheDir)) {
    Write-Host "❌ Cache directory not found: $cacheDir" -ForegroundColor Red
    Write-Host "   Run: task build:prepare:cache" -ForegroundColor Yellow
    exit 1
}

# Verify packages
Write-Host "🔍 Verifying packages..." -ForegroundColor Yellow
$missingPackages = @()
$foundPackages = 0

foreach ($package in $config.packages) {
    $targetPath = $package.target
    if (Test-Path $targetPath) {
        $foundPackages++
    } else {
        $missingPackages += @{
            name = $package.name
            version = $package.version
            target = $targetPath
        }
    }
}

# Verify assemblies
Write-Host "🔍 Verifying assemblies..." -ForegroundColor Yellow
$missingAssemblies = @()
$foundAssemblies = 0

foreach ($assembly in $config.assemblies) {
    $targetPath = $assembly.target
    if (Test-Path $targetPath) {
        $foundAssemblies++
    } else {
        $missingAssemblies += @{
            name = $assembly.name
            version = $assembly.version
            target = $targetPath
        }
    }
}

# Calculate totals
$foundTotal = $foundPackages + $foundAssemblies
$missingTotal = $missingPackages.Count + $missingAssemblies.Count
$actualCacheFolders = (Get-ChildItem -Path $cacheDir -Directory | Measure-Object).Count

# Display results
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Verification Results" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📦 Packages:" -ForegroundColor White
Write-Host "   ✅ Found:   $foundPackages / $expectedPackages" -ForegroundColor Green
if ($missingPackages.Count -gt 0) {
    Write-Host "   ❌ Missing: $($missingPackages.Count)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🔧 Assemblies:" -ForegroundColor White
Write-Host "   ✅ Found:   $foundAssemblies / $expectedAssemblies" -ForegroundColor Green
if ($missingAssemblies.Count -gt 0) {
    Write-Host "   ❌ Missing: $($missingAssemblies.Count)" -ForegroundColor Red
}

Write-Host ""
Write-Host "📊 Total:" -ForegroundColor White
Write-Host "   ✅ Found:       $foundTotal / $expectedTotal" -ForegroundColor Green
Write-Host "   📁 Cache dirs:  $actualCacheFolders" -ForegroundColor Cyan
if ($missingTotal -gt 0) {
    Write-Host "   ❌ Missing:     $missingTotal" -ForegroundColor Red
}

# Show missing items details if any
if ($missingTotal -gt 0) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host "  Missing Items Details" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow

    if ($missingPackages.Count -gt 0) {
        Write-Host ""
        Write-Host "Missing Packages:" -ForegroundColor Red
        foreach ($pkg in $missingPackages) {
            Write-Host "   - $($pkg.name) @ $($pkg.version)" -ForegroundColor Gray
            Write-Host "     Expected at: $($pkg.target)" -ForegroundColor DarkGray
        }
    }

    if ($missingAssemblies.Count -gt 0) {
        Write-Host ""
        Write-Host "Missing Assemblies:" -ForegroundColor Red
        foreach ($asm in $missingAssemblies) {
            Write-Host "   - $($asm.name) @ $($asm.version)" -ForegroundColor Gray
            Write-Host "     Expected at: $($asm.target)" -ForegroundColor DarkGray
        }
    }

    Write-Host ""
    Write-Host "💡 Tip: Run 'task build:prepare:cache' to populate the cache" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan

# Exit with appropriate code
if ($missingTotal -eq 0) {
    Write-Host "✅ All items verified successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Verification completed with missing items" -ForegroundColor Yellow
    exit 1
}
