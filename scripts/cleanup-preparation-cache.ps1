#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Clean up orphaned items from preparation cache
.DESCRIPTION
    Removes folders from build/preparation/cache that are not listed in the preparation config.
    Useful for removing old package versions and unused dependencies.
.PARAMETER ConfigPath
    Path to the preparation config file (default: build/preparation/configs/preparation.json)
.PARAMETER DryRun
    Show what would be deleted without actually deleting
.PARAMETER Force
    Skip confirmation prompts
.EXAMPLE
    .\scripts\cleanup-preparation-cache.ps1 -DryRun
.EXAMPLE
    .\scripts\cleanup-preparation-cache.ps1 -Force
.EXAMPLE
    .\scripts\cleanup-preparation-cache.ps1 -ConfigPath build/preparation/configs/preparation.json
#>

[CmdletBinding()]
param(
    [string]$ConfigPath = "build/preparation/configs/preparation.json",
    [switch]$DryRun,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Preparation Cache Cleanup" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Load configuration
if (-not (Test-Path $ConfigPath)) {
    Write-Host "❌ Configuration file not found: $ConfigPath" -ForegroundColor Red
    exit 1
}

Write-Host "📄 Loading configuration: $ConfigPath" -ForegroundColor Yellow
$config = Get-Content $ConfigPath | ConvertFrom-Json

# Check cache directory
$cacheDir = "build/preparation/cache"
if (-not (Test-Path $cacheDir)) {
    Write-Host "✅ Cache directory does not exist - nothing to clean" -ForegroundColor Green
    exit 0
}

# Build list of expected folder names from config
Write-Host "🔍 Analyzing cache contents..." -ForegroundColor Yellow
$expectedFolders = @()

foreach ($package in $config.packages) {
    $folderName = Split-Path $package.target -Leaf
    $expectedFolders += $folderName
}

foreach ($assembly in $config.assemblies) {
    $folderName = Split-Path $assembly.target -Leaf
    $expectedFolders += $folderName
}

# Get actual folders in cache
$actualFolders = Get-ChildItem -Path $cacheDir -Directory | Select-Object -ExpandProperty Name

# Find orphaned folders
$orphanedFolders = $actualFolders | Where-Object { $_ -notin $expectedFolders }

# Calculate sizes
$totalCacheFolders = $actualFolders.Count
$orphanedCount = $orphanedFolders.Count
$keepCount = $totalCacheFolders - $orphanedCount

# Display summary
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Analysis Results" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Cache Status:" -ForegroundColor White
Write-Host "   📁 Total folders:    $totalCacheFolders" -ForegroundColor Gray
Write-Host "   ✅ Keep (in config): $keepCount" -ForegroundColor Green
Write-Host "   🗑️  Orphaned:         $orphanedCount" -ForegroundColor Yellow
Write-Host ""

if ($orphanedCount -eq 0) {
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "✅ No orphaned folders found - cache is clean!" -ForegroundColor Green
    exit 0
}

# Calculate total size of orphaned folders
Write-Host "📦 Calculating orphaned folder sizes..." -ForegroundColor Yellow
$totalSize = 0
$orphanedDetails = @()

foreach ($folder in $orphanedFolders) {
    $folderPath = Join-Path $cacheDir $folder
    if (Test-Path $folderPath) {
        $size = (Get-ChildItem -Path $folderPath -Recurse -File -ErrorAction SilentlyContinue |
                 Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum
        if ($null -eq $size) { $size = 0 }
        $totalSize += $size
        $orphanedDetails += @{
            Name = $folder
            Path = $folderPath
            Size = $size
        }
    }
}

$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host "  Orphaned Folders to Delete" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""
Write-Host "💾 Total size to reclaim: $totalSizeMB MB" -ForegroundColor Cyan
Write-Host ""

# Show first 20 orphaned folders
$displayCount = [Math]::Min(20, $orphanedCount)
Write-Host "Showing first $displayCount of $orphanedCount orphaned folders:" -ForegroundColor Gray
foreach ($detail in ($orphanedDetails | Sort-Object -Property Size -Descending | Select-Object -First $displayCount)) {
    $sizeMB = [math]::Round($detail.Size / 1MB, 2)
    Write-Host "   - $($detail.Name) " -NoNewline -ForegroundColor DarkGray
    if ($sizeMB -gt 0) {
        Write-Host "($sizeMB MB)" -ForegroundColor DarkCyan
    } else {
        Write-Host "(< 0.01 MB)" -ForegroundColor DarkCyan
    }
}

if ($orphanedCount -gt $displayCount) {
    Write-Host "   ... and $($orphanedCount - $displayCount) more" -ForegroundColor DarkGray
}

# DryRun mode - just show what would be deleted
if ($DryRun) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "ℹ️  Dry-run mode - no files will be deleted" -ForegroundColor Cyan
    Write-Host "   Run without -DryRun to actually delete these folders" -ForegroundColor Gray
    exit 0
}

# Confirmation prompt unless -Force
if (-not $Force) {
    Write-Host ""
    Write-Host "⚠️  WARNING: This will permanently delete $orphanedCount folders ($totalSizeMB MB)" -ForegroundColor Yellow
    Write-Host ""
    $response = Read-Host "Do you want to continue? (yes/no)"
    if ($response -ne "yes") {
        Write-Host ""
        Write-Host "❌ Cleanup cancelled" -ForegroundColor Yellow
        exit 0
    }
}

# Perform deletion
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Deleting Orphaned Folders" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$deletedCount = 0
$failedCount = 0
$reclaimedSize = 0

foreach ($detail in $orphanedDetails) {
    try {
        Write-Host "🗑️  Deleting: $($detail.Name)" -ForegroundColor Gray
        Remove-Item -Path $detail.Path -Recurse -Force -ErrorAction Stop
        $deletedCount++
        $reclaimedSize += $detail.Size
    }
    catch {
        Write-Host "   ❌ Failed: $_" -ForegroundColor Red
        $failedCount++
    }
}

$reclaimedSizeMB = [math]::Round($reclaimedSize / 1MB, 2)

# Final summary
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Cleanup Complete" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Results:" -ForegroundColor White
Write-Host "   ✅ Deleted:  $deletedCount folders" -ForegroundColor Green
Write-Host "   💾 Reclaimed: $reclaimedSizeMB MB" -ForegroundColor Cyan

if ($failedCount -gt 0) {
    Write-Host "   ❌ Failed:   $failedCount folders" -ForegroundColor Red
}

$remainingFolders = (Get-ChildItem -Path $cacheDir -Directory | Measure-Object).Count
Write-Host ""
Write-Host "📁 Cache now contains: $remainingFolders folders" -ForegroundColor Cyan

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan

if ($failedCount -gt 0) {
    Write-Host "⚠️  Cleanup completed with errors" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "✅ Cleanup completed successfully!" -ForegroundColor Green
    exit 0
}
