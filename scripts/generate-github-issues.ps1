# Generate GitHub Issues from Spec-Kit Tasks
# This script parses spec-kit task markdown and generates GitHub issue files

param(
    [Parameter(Mandatory=$true)]
    [string]$TaskFile,

    [Parameter(Mandatory=$false)]
    [string]$OutputDir = ".github/issues",

    [Parameter(Mandatory=$false)]
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Parse task markdown
function Parse-TaskMarkdown {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw
    $tasks = @()

    # Regex to match task sections
    $taskPattern = '### (Task \d+\.\d+): (.+?)\n\*\*Story Points:\*\* (\d+)\s+\n\*\*Priority:\*\* (\w+)\s+\n\*\*Dependencies:\*\* (.+?)\n\n\*\*Subtasks:\*\*\n((?:- \[ \] .+\n)+)\n\*\*Acceptance Criteria:\*\*\n((?:- .+\n)+)\n\*\*Estimated Time:\*\* (.+?)\n'

    $matches = [regex]::Matches($content, $taskPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)

    foreach ($match in $matches) {
        $task = @{
            Id = $match.Groups[1].Value
            Title = $match.Groups[2].Value
            StoryPoints = $match.Groups[3].Value
            Priority = $match.Groups[4].Value
            Dependencies = $match.Groups[5].Value
            Subtasks = $match.Groups[6].Value.Trim()
            AcceptanceCriteria = $match.Groups[7].Value.Trim()
            EstimatedTime = $match.Groups[8].Value
        }
        $tasks += $task
    }

    return $tasks
}

# Generate GitHub issue content
function Generate-IssueContent {
    param($Task, $EpicName)

    $dependencies = if ($Task.Dependencies -eq "None") { "None" } else { $Task.Dependencies }

    $issueContent = @"
---
name: Spec-Kit Task
about: Create a task from spec-kit for AI agent execution
title: '[$($Task.Id)] $($Task.Title)'
labels: spec-kit-task, ai-agent-ready, priority-$($Task.Priority.ToLower())
assignees: ''
---

## Task Metadata

**Task ID:** $($Task.Id)
**Epic:** $EpicName
**Story Points:** $($Task.StoryPoints)
**Priority:** $($Task.Priority)
**Dependencies:** $dependencies
**Estimated Time:** $($Task.EstimatedTime)
**Spec Reference:** ``.specify/tasks/build-preparation-tool-tasks.md#task-$($Task.Id.Replace('.', '').ToLower())``

## Task Description

$($Task.Title)

## Subtasks

$($Task.Subtasks)

## Acceptance Criteria

$($Task.AcceptanceCriteria)

## Technical Context

### Project Structure

This task is part of the Build Preparation Tool located at:
``````
packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/
``````

### Git Root Path Resolution

**IMPORTANT:** All paths in this project are relative to git repository root.

Git Root: ``D:\lunar-snake\constract-work\card-projects\sango-card\``

See ``.agent/base/30-path-resolution.md`` for path resolution rules.

### Technology Stack

- .NET 8.0
- Microsoft.Extensions.Hosting (DI, Logging)
- MessagePipe (Cysharp) - Message bus
- ReactiveUI - Reactive state management
- System.CommandLine - CLI framework
- Terminal.Gui v2 - TUI framework

## AI Agent Instructions

### For GitHub Copilot Workspace

This task is **AI-agent-ready**. Follow these steps:

1. **Read Context Files:**
   - ``.specify/specs/build-preparation-tool.md`` - Full specification
   - ``.specify/tasks/build-preparation-tool-tasks.md`` - Task details
   - ``.agent/base/30-path-resolution.md`` - Path resolution rules
   - ``docs/rfcs/RFC-001-build-preparation-tool.md`` - Architecture

2. **Implement Subtasks:**
   - Complete each subtask in order
   - Follow coding standards in ``.editorconfig``
   - Use dependency injection for services
   - Use async/await for I/O operations

3. **Write Tests:**
   - Create unit tests in ``Tests/Unit/`` directory
   - Target 85%+ code coverage
   - Use xUnit framework
   - Mock dependencies with Moq

4. **Validate:**
   - All acceptance criteria met
   - All tests passing
   - No compiler warnings
   - XML documentation complete

### Context Files to Read

- ``.specify/specs/build-preparation-tool.md``
- ``.specify/tasks/build-preparation-tool-tasks.md``
- ``.agent/base/30-path-resolution.md``
- ``.agent/base/20-rules.md``
- ``docs/rfcs/RFC-001-build-preparation-tool.md``

### Coding Standards

- **Path Resolution:** All paths relative to git root (use ``PathResolver``)
- **Dependency Injection:** Register services in ``HostBuilderExtensions.cs``
- **Logging:** Use ``ILogger<T>`` from Microsoft.Extensions.Logging
- **Messaging:** Use MessagePipe for component communication
- **Async:** Use async/await for I/O operations
- **Naming:** PascalCase for public members, camelCase for private
- **Documentation:** XML comments for public APIs

### Success Criteria for AI Agent

- [ ] All subtasks completed
- [ ] All acceptance criteria met
- [ ] Unit tests written and passing (85%+ coverage)
- [ ] Code follows project standards
- [ ] No hardcoded paths (use PathResolver)
- [ ] Proper error handling with clear messages
- [ ] XML documentation for public APIs
- [ ] No compiler warnings
- [ ] MessagePipe used for component communication

## Definition of Done

- [ ] Code implemented and compiles
- [ ] Unit tests written (85%+ coverage)
- [ ] All tests passing
- [ ] No compiler warnings
- [ ] XML documentation complete
- [ ] Follows coding standards
- [ ] Dependency injection used
- [ ] Error handling implemented
- [ ] Logging added
- [ ] Ready for code review

## Related Issues

- Epic: Build Preparation Tool Implementation
- Depends on: $dependencies
- Spec: ``.specify/specs/build-preparation-tool.md``
- RFC: ``docs/rfcs/RFC-001-build-preparation-tool.md``

## Notes

**For Human Reviewers:**
- This issue was auto-generated from spec-kit tasks
- Review the spec and RFC before starting
- Coordinate with other developers on dependencies
- Update task status in ``.specify/tasks/build-preparation-tool-tasks.md``

**For AI Agents:**
- This task is designed for autonomous execution
- All context is provided in referenced files
- Follow the coding standards strictly
- Ask for clarification if requirements are ambiguous
"@

    return $issueContent
}

# Main execution
Write-Host "Parsing task file: $TaskFile" -ForegroundColor Cyan

if (-not (Test-Path $TaskFile)) {
    Write-Error "Task file not found: $TaskFile"
    exit 1
}

$tasks = Parse-TaskMarkdown -FilePath $TaskFile

Write-Host "Found $($tasks.Count) tasks" -ForegroundColor Green

# Create output directory
if (-not $DryRun) {
    New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
}

# Determine epic name from task ID
$epicMap = @{
    "1" = "Epic 1: Core Infrastructure"
    "2" = "Epic 2: Services Implementation"
    "3" = "Epic 3: Code Patchers"
    "4" = "Epic 4: CLI Mode"
    "5" = "Epic 5: TUI Mode"
    "6" = "Epic 6: Integration & Testing"
}

foreach ($task in $tasks) {
    $epicNumber = $task.Id.Split('.')[0].Replace('Task ', '')
    $epicName = $epicMap[$epicNumber]

    $issueContent = Generate-IssueContent -Task $task -EpicName $epicName

    $fileName = "$($task.Id.Replace(' ', '-').Replace('.', '-').ToLower()).md"
    $filePath = Join-Path $OutputDir $fileName

    if ($DryRun) {
        Write-Host "`nTask: $($task.Id) - $($task.Title)" -ForegroundColor Yellow
        Write-Host "  Would create: $filePath" -ForegroundColor Gray
        Write-Host "  Epic: $epicName" -ForegroundColor Gray
        Write-Host "  Story Points: $($task.StoryPoints)" -ForegroundColor Gray
        Write-Host "  Priority: $($task.Priority)" -ForegroundColor Gray
    } else {
        $issueContent | Out-File -FilePath $filePath -Encoding UTF8
        Write-Host "Created: $filePath" -ForegroundColor Green
    }
}

Write-Host "`nDone! Generated $($tasks.Count) issue files" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "`nThis was a dry run. Use without -DryRun to create files." -ForegroundColor Yellow
} else {
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Review generated issues in $OutputDir" -ForegroundColor White
    Write-Host "2. Create GitHub issues using GitHub CLI:" -ForegroundColor White
    Write-Host "   gh issue create --title 'Title' --body-file $OutputDir/task-1-1.md" -ForegroundColor Gray
    Write-Host "3. Or use the GitHub web interface to create issues" -ForegroundColor White
}
