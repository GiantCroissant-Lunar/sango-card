#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Move scattered build tool documentation to canonical locations.

.DESCRIPTION
    Moves documentation files from packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/
    to their proper canonical locations in docs/

.PARAMETER DryRun
    Preview moves without actually moving files

.EXAMPLE
    .\scripts\move-build-tool-docs.ps1 -DryRun
    Preview what would be moved

.EXAMPLE
    .\scripts\move-build-tool-docs.ps1
    Actually move the files
#>

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "📦 Build Tool Documentation Migration" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

$sourceDir = "packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool"

# Define file mappings: source -> destination
$moves = @(
    @{
        Source = "$sourceDir/WAVE_8_COMPLETION.md"
        Dest = "docs/archive/build-tool-wave-8-completion.md"
        Type = "archive"
    },
    @{
        Source = "$sourceDir/WAVE_9_COMPLETION.md"
        Dest = "docs/archive/build-tool-wave-9-completion.md"
        Type = "archive"
    },
    @{
        Source = "$sourceDir/WAVE_9_TEST_PLAN.md"
        Dest = "docs/archive/build-tool-wave-9-test-plan.md"
        Type = "archive"
    },
    @{
        Source = "$sourceDir/MIGRATION-GUIDE.md"
        Dest = "docs/guides/build-tool-migration-guide.md"
        Type = "guide"
    },
    @{
        Source = "$sourceDir/TERMINAL_GUI_V2_MIGRATION.md"
        Dest = "docs/guides/terminal-gui-v2-migration.md"
        Type = "guide"
    },
    @{
        Source = "$sourceDir/TUI_MANUAL_TESTING_GUIDE.md"
        Dest = "docs/guides/build-tool-tui-testing-guide.md"
        Type = "guide"
    }
)

# Files to keep in place
$keep = @(
    "$sourceDir/README.md"
)

Write-Host "Files to move:" -ForegroundColor Yellow
Write-Host ""

$movedCount = 0
$skippedCount = 0

foreach ($move in $moves) {
    $source = $move.Source
    $dest = $move.Dest
    $type = $move.Type

    if (-not (Test-Path $source)) {
        Write-Host "⚠️  SKIP: $source (not found)" -ForegroundColor Yellow
        $skippedCount++
        continue
    }

    $icon = if ($type -eq "archive") { "📦" } else { "📖" }
    Write-Host "$icon [$type] $source" -ForegroundColor Cyan
    Write-Host "     → $dest" -ForegroundColor Green

    if (-not $DryRun) {
        # Ensure destination directory exists
        $destDir = Split-Path -Parent $dest
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }

        # Move file
        Move-Item -Path $source -Destination $dest -Force
        Write-Host "     ✓ Moved" -ForegroundColor Green
    } else {
        Write-Host "     [DRY RUN]" -ForegroundColor Gray
    }

    Write-Host ""
    $movedCount++
}

Write-Host "Files to keep in place:" -ForegroundColor Cyan
Write-Host ""

foreach ($file in $keep) {
    if (Test-Path $file) {
        Write-Host "✓ $file" -ForegroundColor Green
    } else {
        Write-Host "⚠️  $file (not found)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=" * 60 -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "🔍 Dry run complete - no files were moved" -ForegroundColor Cyan
    Write-Host "   Run without -DryRun to apply changes" -ForegroundColor Yellow
} else {
    Write-Host "✅ Moved $movedCount files!" -ForegroundColor Green
    if ($skippedCount -gt 0) {
        Write-Host "⚠️  Skipped $skippedCount files (not found)" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Add front-matter: pwsh scripts/add-frontmatter.ps1" -ForegroundColor White
    Write-Host "  2. Validate docs: task docs:check" -ForegroundColor White
    Write-Host "  3. Update any internal links if needed" -ForegroundColor White
}

Write-Host ""
