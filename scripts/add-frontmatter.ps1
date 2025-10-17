#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Add front-matter to documentation files missing it.

.DESCRIPTION
    Scans docs/ for files without front-matter and adds a basic template.

.PARAMETER DryRun
    Preview changes without modifying files

.EXAMPLE
    .\scripts\add-frontmatter.ps1 -DryRun
    Preview what front-matter would be added

.EXAMPLE
    .\scripts\add-frontmatter.ps1
    Add front-matter to all files missing it
#>

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "📝 Adding Front-Matter to Documentation" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

# Get the next available doc_id
$registryPath = "docs/index/registry.json"
$nextDocId = "00001"

if (Test-Path $registryPath) {
    try {
        $registry = Get-Content $registryPath | ConvertFrom-Json
        $maxId = 0
        foreach ($doc in $registry.docs) {
            if ($doc.doc_id -match 'DOC-\d{4}-(\d{5})') {
                $id = [int]$Matches[1]
                if ($id -gt $maxId) {
                    $maxId = $id
                }
            }
        }
        $nextDocId = "{0:D5}" -f ($maxId + 1)
    } catch {
        Write-Warning "Could not parse registry, using default doc_id"
    }
}

$currentYear = Get-Date -Format "yyyy"
$currentDate = Get-Date -Format "yyyy-MM-dd"

# Find all docs without front-matter
$docsDir = "docs"
$filesWithoutFrontmatter = @()

Get-ChildItem -Path $docsDir -Recurse -Filter "*.md" -File | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw

    if (-not ($content -match '^---\s*\n.*?\n---')) {
        $filesWithoutFrontmatter += $_
    }
}

Write-Host "Found $($filesWithoutFrontmatter.Count) files without front-matter" -ForegroundColor Gray
Write-Host ""

if ($filesWithoutFrontmatter.Count -eq 0) {
    Write-Host "✅ All docs already have front-matter!" -ForegroundColor Green
    exit 0
}

$counter = [int]$nextDocId

foreach ($file in $filesWithoutFrontmatter) {
    $relativePath = $file.FullName.Replace("$PWD\", "").Replace("\", "/")

    # Infer doc_type from directory
    $docType = "guide"
    if ($relativePath -match "docs/specs/") { $docType = "spec" }
    elseif ($relativePath -match "docs/rfcs/") { $docType = "rfc" }
    elseif ($relativePath -match "docs/adrs/") { $docType = "adr" }
    elseif ($relativePath -match "docs/plans/") { $docType = "plan" }
    elseif ($relativePath -match "docs/findings/") { $docType = "finding" }
    elseif ($relativePath -match "docs/guides/") { $docType = "guide" }
    elseif ($relativePath -match "docs/archive/") { $docType = "guide" }
    elseif ($relativePath -match "docs/_inbox/") { $docType = "guide" }

    # Infer status from directory
    $status = "active"
    if ($relativePath -match "docs/archive/") { $status = "archived" }
    elseif ($relativePath -match "docs/_inbox/") { $status = "draft" }

    # Generate title from filename
    $filename = $file.BaseName
    $title = ($filename -replace '[-_]', ' ').Trim()
    $title = (Get-Culture).TextInfo.ToTitleCase($title)

    # Generate tags from filename
    $tags = @()
    $filename.Split('-_') | ForEach-Object {
        $word = $_.Trim().ToLower()
        if ($word.Length -gt 3 -and $word -notmatch '^\d+$') {
            $tags += $word
        }
    }
    $tagsStr = ($tags | Select-Object -Unique | Select-Object -First 5) -join ', '

    # Create front-matter
    $docId = "DOC-$currentYear-{0:D5}" -f $counter
    $frontmatter = @"
---
doc_id: $docId
title: $title
doc_type: $docType
status: $status
canonical: false
created: $currentDate
tags: [$tagsStr]
summary: >
  (Add summary here)
source:
  author: system
---

"@

    $content = Get-Content -Path $file.FullName -Raw
    $newContent = $frontmatter + $content

    if ($DryRun) {
        Write-Host "[$docType] $relativePath" -ForegroundColor Yellow
        Write-Host "    doc_id: $docId" -ForegroundColor Gray
        Write-Host "    title: $title" -ForegroundColor Gray
        Write-Host ""
    } else {
        Set-Content -Path $file.FullName -Value $newContent -Encoding utf8NoBOM
        Write-Host "✓ [$docType] $relativePath" -ForegroundColor Green
        Write-Host "    doc_id: $docId" -ForegroundColor Gray
        Write-Host ""
    }

    $counter++
}

Write-Host ""
Write-Host "=" * 60 -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "🔍 Dry run complete - no files were modified" -ForegroundColor Cyan
    Write-Host "   Run without -DryRun to apply changes" -ForegroundColor Yellow
} else {
    Write-Host "✅ Front-matter added to $($filesWithoutFrontmatter.Count) files!" -ForegroundColor Green
    Write-Host "   Next steps:" -ForegroundColor Cyan
    Write-Host "   1. Review and update summaries" -ForegroundColor White
    Write-Host "   2. Set canonical: true for primary docs" -ForegroundColor White
    Write-Host "   3. Run: task docs:validate" -ForegroundColor White
}

Write-Host ""
