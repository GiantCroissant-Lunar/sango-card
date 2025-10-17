#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fix duplicate front-matter in documentation files.

.DESCRIPTION
    Scans docs/ for files with duplicate YAML front-matter and removes duplicates,
    keeping the better/canonical one.

.PARAMETER DryRun
    Preview fixes without modifying files

.EXAMPLE
    .\scripts\fix-duplicate-frontmatter.ps1 -DryRun
    Preview what would be fixed

.EXAMPLE
    .\scripts\fix-duplicate-frontmatter.ps1
    Fix duplicate front-matter
#>

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "🔧 Fix Duplicate Front-Matter" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

# Find all markdown files in docs/
$docsFiles = Get-ChildItem -Path "docs" -Recurse -Filter "*.md" -File

$fixed = 0
$skipped = 0

foreach ($file in $docsFiles) {
    $content = Get-Content -Path $file.FullName -Raw

    # Check for duplicate front-matter (two --- ... --- blocks)
    $frontmatterPattern = '(?s)^---\s*\r?\n(.*?)\r?\n---\s*\r?\n---\s*\r?\n(.*?)\r?\n---'

    if ($content -match $frontmatterPattern) {
        $firstFrontmatter = $Matches[1]
        $secondFrontmatter = $Matches[2]

        $relativePath = $file.FullName.Replace("$PWD\", "").Replace("\", "/")

        Write-Host "❌ DUPLICATE: $relativePath" -ForegroundColor Yellow
        Write-Host "   First front-matter (lines 1-13):" -ForegroundColor Gray
        Write-Host "   Second front-matter (lines 14-28):" -ForegroundColor Gray
        Write-Host ""

        # Determine which front-matter to keep
        # Keep the second one if it has more fields or is marked canonical
        $keepSecond = $false

        if ($secondFrontmatter -match 'canonical:\s*true') {
            $keepSecond = $true
            Write-Host "   → Keeping second (has canonical: true)" -ForegroundColor Green
        } elseif ($secondFrontmatter.Length > $firstFrontmatter.Length) {
            $keepSecond = $true
            Write-Host "   → Keeping second (more complete)" -ForegroundColor Green
        } else {
            Write-Host "   → Keeping first" -ForegroundColor Green
        }

        if (-not $DryRun) {
            if ($keepSecond) {
                # Remove first front-matter, keep second
                $newContent = $content -replace '(?s)^---\s*\r?\n.*?\r?\n---\s*\r?\n(---\s*\r?\n.*?\r?\n---)', '$1'
            } else {
                # Remove second front-matter, keep first
                $newContent = $content -replace '(?s)(^---\s*\r?\n.*?\r?\n---)\s*\r?\n---\s*\r?\n.*?\r?\n---', '$1'
            }

            Set-Content -Path $file.FullName -Value $newContent -Encoding utf8NoBOM
            Write-Host "   ✓ Fixed" -ForegroundColor Green
        } else {
            Write-Host "   [DRY RUN]" -ForegroundColor Gray
        }

        Write-Host ""
        $fixed++
    }
}

Write-Host "=" * 60 -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "🔍 Dry run complete" -ForegroundColor Cyan
    Write-Host "   Found $fixed files with duplicate front-matter" -ForegroundColor Yellow
    Write-Host "   Run without -DryRun to fix" -ForegroundColor White
} else {
    if ($fixed -gt 0) {
        Write-Host "✅ Fixed $fixed files!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "  1. Verify: task docs:check" -ForegroundColor White
        Write-Host "  2. Review changes: git diff docs/" -ForegroundColor White
    } else {
        Write-Host "✅ No duplicate front-matter found!" -ForegroundColor Green
    }
}

Write-Host ""
