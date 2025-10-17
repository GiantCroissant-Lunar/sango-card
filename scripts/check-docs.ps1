#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Manual documentation linter - finds scattered docs and front-matter issues.

.DESCRIPTION
    Comprehensive documentation checker that can be run manually anytime.
    Detects scattered docs, missing front-matter, and validates the registry.

.PARAMETER Fix
    Automatically move scattered docs to docs/_inbox/

.PARAMETER Report
    Generate detailed HTML report

.EXAMPLE
    .\scripts\check-docs.ps1
    Check for scattered docs and front-matter issues

.EXAMPLE
    .\scripts\check-docs.ps1 -Fix
    Automatically move scattered docs to docs/_inbox/

.EXAMPLE
    .\scripts\check-docs.ps1 -Report
    Generate HTML report of documentation health
#>

param(
    [switch]$Fix,
    [switch]$Report
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "📚 Documentation Health Check" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

# Define allowed locations
$ALLOWED_PATTERNS = @(
    "^README\.md$",
    "^AGENTS\.md$",
    "^CLAUDE\.md$",
    "^CODE_OF_CONDUCT\.md$",
    "^CONTRIBUTING\.md$",
    "^LICENSE\.md$",
    "^CHANGELOG\.md$",
    "^docs/",
    "^\.agent/",
    "^\.github/",
    "^\.specify/",
    "^\.windsurf/",
    "^\.gemini/",
    ".*/README\.md$",
    ".*/LICENSE\.md$",
    ".*/CHANGELOG\.md$",
    "^build/preparation/.*\.md$",
    "^projects/client/.*\.md$",
    "^packages/.*/dotnet~/.*\.md$"
)

# Find all markdown files
$allMdFiles = Get-ChildItem -Path . -Recurse -Filter "*.md" -File `
    | Where-Object { $_.FullName -notmatch "node_modules|\.git" } `
    | ForEach-Object { $_.FullName.Replace("$PWD\", "").Replace("\", "/") }

Write-Host "Found $($allMdFiles.Count) markdown files total" -ForegroundColor Gray
Write-Host ""

# Check for scattered docs
$scatteredDocs = @()

foreach ($file in $allMdFiles) {
    $isAllowed = $false

    foreach ($pattern in $ALLOWED_PATTERNS) {
        if ($file -match $pattern) {
            $isAllowed = $true
            break
        }
    }

    if (-not $isAllowed) {
        $scatteredDocs += $file
    }
}

# Report scattered docs
if ($scatteredDocs.Count -eq 0) {
    Write-Host "✅ No scattered documentation found!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "❌ Found $($scatteredDocs.Count) scattered documentation files:" -ForegroundColor Red
    Write-Host ""

    foreach ($doc in $scatteredDocs) {
        Write-Host "  - $doc" -ForegroundColor Yellow
    }

    Write-Host ""

    if ($Fix) {
        Write-Host "🔧 Auto-fixing: Moving to docs/_inbox/" -ForegroundColor Cyan
        Write-Host ""

        $inboxDir = "docs/_inbox"
        if (-not (Test-Path $inboxDir)) {
            New-Item -ItemType Directory -Path $inboxDir -Force | Out-Null
        }

        foreach ($doc in $scatteredDocs) {
            $fileName = Split-Path -Leaf $doc
            $target = Join-Path $inboxDir $fileName

            # Handle name conflicts
            $counter = 1
            while (Test-Path $target) {
                $baseName = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
                $extension = [System.IO.Path]::GetExtension($fileName)
                $target = Join-Path $inboxDir "$baseName-$counter$extension"
                $counter++
            }

            Move-Item -Path $doc -Destination $target -Force
            Write-Host "  Moved: $doc → $target" -ForegroundColor Green
        }

        Write-Host ""
        Write-Host "✅ Fixed! Files moved to docs/_inbox/" -ForegroundColor Green
        Write-Host "   Remember to add front-matter!" -ForegroundColor Yellow
        Write-Host ""
    } else {
        Write-Host "💡 Run with -Fix to automatically move to docs/_inbox/" -ForegroundColor Cyan
        Write-Host ""
    }
}

# Check front-matter in docs/
Write-Host "🔍 Checking front-matter in docs/..." -ForegroundColor Cyan
Write-Host ""

$docsFiles = Get-ChildItem -Path "docs" -Recurse -Filter "*.md" -File `
    | Where-Object { $_.Name -ne "README.md" }

$missingFrontmatter = @()

foreach ($file in $docsFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    if (-not ($content -match '^---\s*\n.*?\n---')) {
        $relativePath = $file.FullName.Replace("$PWD\", "").Replace("\", "/")
        $missingFrontmatter += $relativePath
    }
}

if ($missingFrontmatter.Count -eq 0) {
    Write-Host "✅ All docs have front-matter!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Found $($missingFrontmatter.Count) files missing front-matter:" -ForegroundColor Yellow
    Write-Host ""
    foreach ($file in $missingFrontmatter) {
        Write-Host "  - $file" -ForegroundColor Yellow
    }
}

Write-Host ""

# Run Python validator
Write-Host "🔍 Running full validation (Python)..." -ForegroundColor Cyan
Write-Host ""

python scripts/docs_validate.py

# Generate report if requested
if ($Report) {
    Write-Host ""
    Write-Host "📊 Generating HTML report..." -ForegroundColor Cyan

    $reportHtml = @"
<!DOCTYPE html>
<html>
<head>
    <title>Documentation Health Report</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; margin: 40px; }
        .good { color: #22c55e; }
        .warning { color: #f59e0b; }
        .error { color: #ef4444; }
        .section { margin: 20px 0; padding: 20px; background: #f9fafb; border-radius: 8px; }
        ul { list-style: none; padding-left: 0; }
        li { padding: 4px 0; }
        li:before { content: "→ "; color: #6b7280; }
    </style>
</head>
<body>
    <h1>📚 Documentation Health Report</h1>
    <p>Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")</p>

    <div class="section">
        <h2>Scattered Documentation</h2>
        $(if ($scatteredDocs.Count -eq 0) {
            "<p class='good'>✅ No scattered docs found</p>"
        } else {
            "<p class='error'>❌ Found $($scatteredDocs.Count) scattered docs</p><ul>"
            $scatteredDocs | ForEach-Object { "<li>$_</li>" }
            "</ul>"
        })
    </div>

    <div class="section">
        <h2>Front-Matter Status</h2>
        $(if ($missingFrontmatter.Count -eq 0) {
            "<p class='good'>✅ All docs have front-matter</p>"
        } else {
            "<p class='warning'>⚠️ $($missingFrontmatter.Count) files missing front-matter</p><ul>"
            $missingFrontmatter | ForEach-Object { "<li>$_</li>" }
            "</ul>"
        })
    </div>

    <div class="section">
        <h2>Registry Stats</h2>
        $(if (Test-Path "docs/index/registry.json") {
            $registry = Get-Content "docs/index/registry.json" | ConvertFrom-Json
            "<p>Total docs: <strong>$($registry.total_docs)</strong></p>"
            "<p>By type: $($registry.by_type | ConvertTo-Json)</p>"
            "<p>By status: $($registry.by_status | ConvertTo-Json)</p>"
        } else {
            "<p class='error'>❌ Registry not found</p>"
        })
    </div>

    <div class="section">
        <h2>Quick Actions</h2>
        <ul>
            <li>Fix scattered docs: <code>.\scripts\check-docs.ps1 -Fix</code></li>
            <li>Validate all: <code>python scripts/docs_validate.py</code></li>
            <li>Pre-commit check: <code>pre-commit run check-scattered-docs --all-files</code></li>
        </ul>
    </div>
</body>
</html>
"@

    $reportPath = "docs-health-report.html"
    $reportHtml | Out-File -FilePath $reportPath -Encoding utf8
    Write-Host "✅ Report saved: $reportPath" -ForegroundColor Green
    Write-Host ""

    # Open in browser
    if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
        Start-Process $reportPath
    }
}

# Summary
Write-Host ""
Write-Host "=" * 60 -ForegroundColor Cyan

if ($scatteredDocs.Count -eq 0 -and $missingFrontmatter.Count -eq 0) {
    Write-Host "✅ Documentation is healthy!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Found issues - please review and fix" -ForegroundColor Yellow
    exit 1
}
