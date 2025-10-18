# Create GitHub Issues from Generated Issue Files
# Requires GitHub CLI (gh) to be installed and authenticated

param(
    [Parameter(Mandatory=$false)]
    [string]$IssueDir = ".github/issues",

    [Parameter(Mandatory=$false)]
    [string]$Repository = "",  # e.g., "owner/repo"

    [Parameter(Mandatory=$false)]
    [switch]$DryRun,

    [Parameter(Mandatory=$false)]
    [string]$Milestone = "",

    [Parameter(Mandatory=$false)]
    [string]$Project = ""
)

$ErrorActionPreference = "Stop"

# Check if gh CLI is installed
try {
    $ghVersion = gh --version
    Write-Host "GitHub CLI detected: $($ghVersion[0])" -ForegroundColor Green
} catch {
    Write-Error "GitHub CLI (gh) not found. Install from: https://cli.github.com/"
    exit 1
}

# Check if authenticated
try {
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Not authenticated with GitHub. Run: gh auth login"
        exit 1
    }
    Write-Host "Authenticated with GitHub" -ForegroundColor Green
} catch {
    Write-Error "Failed to check GitHub authentication status"
    exit 1
}

# Get repository if not specified
if ([string]::IsNullOrEmpty($Repository)) {
    try {
        $Repository = gh repo view --json nameWithOwner -q .nameWithOwner
        Write-Host "Using repository: $Repository" -ForegroundColor Cyan
    } catch {
        Write-Error "Could not detect repository. Specify with -Repository parameter"
        exit 1
    }
}

# Get issue files
if (-not (Test-Path $IssueDir)) {
    Write-Error "Issue directory not found: $IssueDir"
    Write-Host "Run generate-github-issues.ps1 first to create issue files" -ForegroundColor Yellow
    exit 1
}

$issueFiles = Get-ChildItem -Path $IssueDir -Filter "*.md" | Sort-Object Name

if ($issueFiles.Count -eq 0) {
    Write-Error "No issue files found in $IssueDir"
    exit 1
}

Write-Host "Found $($issueFiles.Count) issue files" -ForegroundColor Cyan

# Parse issue file to extract metadata
function Parse-IssueFile {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw

    # Extract title from frontmatter
    if ($content -match "title: '(.+?)'") {
        $title = $Matches[1]
    } else {
        $title = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
    }

    # Extract labels
    $labels = @("spec-kit-task", "ai-agent-ready")
    if ($content -match "labels: (.+)") {
        $labelText = $Matches[1]
        $labels = $labelText -split ',' | ForEach-Object { $_.Trim() }
    }

    # Extract priority
    if ($content -match "\*\*Priority:\*\* (\w+)") {
        $priority = $Matches[1].ToLower()
        $labels += "priority-$priority"
    }

    # Extract story points
    if ($content -match "\*\*Story Points:\*\* (\d+)") {
        $storyPoints = $Matches[1]
        $labels += "story-points-$storyPoints"
    }

    # Extract epic
    if ($content -match "\*\*Epic:\*\* (.+)") {
        $epic = $Matches[1].Trim()
        $epicLabel = $epic -replace '[^\w\s-]', '' -replace '\s+', '-' -replace '^Epic-\d+:-', 'epic-'
        $labels += $epicLabel.ToLower()
    }

    return @{
        Title = $title
        Labels = $labels
        FilePath = $FilePath
    }
}

# Create issues
$createdIssues = @()
$failedIssues = @()

foreach ($issueFile in $issueFiles) {
    $issue = Parse-IssueFile -FilePath $issueFile.FullName

    Write-Host "`nProcessing: $($issue.Title)" -ForegroundColor Yellow
    Write-Host "  Labels: $($issue.Labels -join ', ')" -ForegroundColor Gray

    if ($DryRun) {
        Write-Host "  [DRY RUN] Would create issue" -ForegroundColor Cyan
        continue
    }

    try {
        # Build gh issue create command
        $ghArgs = @(
            "issue", "create",
            "--repo", $Repository,
            "--title", $issue.Title,
            "--body-file", $issue.FilePath
        )

        # Add labels
        foreach ($label in $issue.Labels) {
            $ghArgs += "--label"
            $ghArgs += $label
        }

        # Add milestone if specified
        if (-not [string]::IsNullOrEmpty($Milestone)) {
            $ghArgs += "--milestone"
            $ghArgs += $Milestone
        }

        # Add to project if specified
        if (-not [string]::IsNullOrEmpty($Project)) {
            $ghArgs += "--project"
            $ghArgs += $Project
        }

        # Create issue
        $issueUrl = & gh @ghArgs

        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Created: $issueUrl" -ForegroundColor Green
            $createdIssues += @{
                Title = $issue.Title
                Url = $issueUrl
            }
        } else {
            Write-Host "  ✗ Failed to create issue" -ForegroundColor Red
            $failedIssues += $issue.Title
        }

        # Rate limiting - wait between requests
        Start-Sleep -Milliseconds 500

    } catch {
        Write-Host "  ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
        $failedIssues += $issue.Title
    }
}

# Summary
Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "`nDRY RUN - No issues created" -ForegroundColor Yellow
    Write-Host "Would create $($issueFiles.Count) issues" -ForegroundColor White
    Write-Host "`nRun without -DryRun to create issues" -ForegroundColor Yellow
} else {
    Write-Host "`nCreated: $($createdIssues.Count) issues" -ForegroundColor Green
    Write-Host "Failed: $($failedIssues.Count) issues" -ForegroundColor $(if ($failedIssues.Count -gt 0) { "Red" } else { "Green" })

    if ($createdIssues.Count -gt 0) {
        Write-Host "`nCreated Issues:" -ForegroundColor Cyan
        foreach ($created in $createdIssues) {
            Write-Host "  ✓ $($created.Title)" -ForegroundColor Green
            Write-Host "    $($created.Url)" -ForegroundColor Gray
        }
    }

    if ($failedIssues.Count -gt 0) {
        Write-Host "`nFailed Issues:" -ForegroundColor Red
        foreach ($failed in $failedIssues) {
            Write-Host "  ✗ $failed" -ForegroundColor Red
        }
    }
}

Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
