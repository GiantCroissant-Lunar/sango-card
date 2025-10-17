#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-commit hook to detect scattered documentation files.

.DESCRIPTION
    Checks for markdown files in non-canonical locations and blocks commit if found.
    Part of the documentation management system (R-DOC-xxx).
#>

$ErrorActionPreference = "Stop"

# Define allowed documentation locations
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
    "^build/nuke/build/.*\.md$",       # Nuke build system docs
    "^build/preparation/.*\.md$",      # Build preparation docs
    "^projects/client/.*\.md$",        # Unity project docs
    "^projects/code-quality/.*\.md$",  # Code quality project docs
    "^packages/.*/dotnet~/.*\.md$"     # .NET tool docs
)

# Get staged markdown files
$stagedFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { $_ -match '\.md$' }

if (-not $stagedFiles) {
    exit 0
}

$scatteredDocs = @()

foreach ($file in $stagedFiles) {
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

if ($scatteredDocs.Count -gt 0) {
    Write-Host ""
    Write-Host "ERROR: Scattered documentation detected!" -ForegroundColor Red
    Write-Host ""
    Write-Host "The following markdown files are in non-canonical locations:" -ForegroundColor Yellow
    Write-Host ""

    foreach ($doc in $scatteredDocs) {
        Write-Host "  - $doc" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "Documentation must be in canonical locations:" -ForegroundColor Cyan
    Write-Host "  - New docs        -> docs/_inbox/" -ForegroundColor Green
    Write-Host "  - Guides          -> docs/guides/" -ForegroundColor Green
    Write-Host "  - Specifications  -> docs/specs/" -ForegroundColor Green
    Write-Host "  - RFCs            -> docs/rfcs/" -ForegroundColor Green
    Write-Host "  - ADRs            -> docs/adrs/" -ForegroundColor Green
    Write-Host "  - Plans           -> docs/plans/" -ForegroundColor Green
    Write-Host "  - Findings        -> docs/findings/" -ForegroundColor Green
    Write-Host "  - Obsolete docs   -> docs/archive/" -ForegroundColor Green
    Write-Host ""
    Write-Host "See: docs/DOCUMENTATION-SCHEMA.md" -ForegroundColor Cyan
    Write-Host "See: .agent/base/40-documentation.md (R-DOC-001)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To fix:" -ForegroundColor Yellow
    Write-Host "  1. Move files to proper location (usually docs/_inbox/)" -ForegroundColor White
    Write-Host "  2. Add YAML front-matter (see docs/DOCUMENTATION-SCHEMA.md)" -ForegroundColor White
    Write-Host "  3. Or remove from staging: git reset HEAD <file>" -ForegroundColor White
    Write-Host ""

    exit 1
}

exit 0
