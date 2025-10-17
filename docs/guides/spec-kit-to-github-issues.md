---
doc_id: DOC-2025-00063
title: Spec Kit to GitHub Issues
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:
- spec-kit
- github
- issues
summary: Guide for converting Spec Kit artifacts to GitHub issues.
---

# Spec-Kit to GitHub Issues Workflow

## Overview

This document describes how to convert spec-kit tasks into GitHub issues that can be assigned to AI agents (GitHub Copilot Workspace, Cursor, etc.) for autonomous execution.

## Workflow

```
┌─────────────────┐
│   Spec-Kit      │
│   Tasks         │
│  (.specify/)    │
└────────┬────────┘
         │
         │ 1. Parse
         ▼
┌─────────────────┐
│  Generate       │
│  Issue Files    │
│  (script)       │
└────────┬────────┘
         │
         │ 2. Create
         ▼
┌─────────────────┐
│  GitHub         │
│  Issues         │
└────────┬────────┘
         │
         │ 3. Assign
         ▼
┌─────────────────┐
│  AI Agent       │
│  (Copilot)      │
└────────┬────────┘
         │
         │ 4. Execute
         ▼
┌─────────────────┐
│  Pull Request   │
│  (Review)       │
└─────────────────┘
```

## Step 1: Generate Issue Files

### Prerequisites

- PowerShell 7+ installed
- Spec-kit tasks written in `.specify/tasks/`

### Generate Issues

```powershell
# Dry run to preview
.\scripts\generate-github-issues.ps1 `
    -TaskFile ".specify\tasks\build-preparation-tool-tasks.md" `
    -DryRun

# Generate issue files
.\scripts\generate-github-issues.ps1 `
    -TaskFile ".specify\tasks\build-preparation-tool-tasks.md" `
    -OutputDir ".github\issues"
```

**Output:**

- Creates `.github/issues/task-1-1.md`, `task-1-2.md`, etc.
- Each file is a complete GitHub issue with:
  - Task metadata
  - Subtasks
  - Acceptance criteria
  - AI agent instructions
  - Context file references

## Step 2: Create GitHub Issues

### Prerequisites

- GitHub CLI (`gh`) installed: <https://cli.github.com/>
- Authenticated: `gh auth login`

### Create Issues

```powershell
# Dry run to preview
.\scripts\create-github-issues.ps1 `
    -IssueDir ".github\issues" `
    -DryRun

# Create all issues
.\scripts\create-github-issues.ps1 `
    -IssueDir ".github\issues" `
    -Repository "owner/repo"

# Create with milestone and project
.\scripts\create-github-issues.ps1 `
    -IssueDir ".github\issues" `
    -Repository "owner/repo" `
    -Milestone "v1.0" `
    -Project "Build Tool"
```

**Output:**

- Creates GitHub issues with labels:
  - `spec-kit-task` - Marks as spec-kit generated
  - `ai-agent-ready` - Ready for AI agent
  - `priority-high` - Priority level
  - `story-points-5` - Story points
  - `epic-core-infrastructure` - Epic grouping

## Step 3: Assign to AI Agent

### GitHub Copilot Workspace

1. **Open Issue:**
   - Navigate to created issue
   - Click "Open in Copilot Workspace"

2. **Copilot Reads Context:**
   - Automatically reads referenced files:
     - `.specify/specs/build-preparation-tool.md`
     - `.specify/tasks/build-preparation-tool-tasks.md`
     - `.agent/base/30-path-resolution.md`
     - `docs/rfcs/RFC-001-build-preparation-tool.md`

3. **Copilot Generates Plan:**
   - Proposes implementation plan
   - Shows files to create/modify
   - Estimates changes

4. **Review and Execute:**
   - Review Copilot's plan
   - Approve execution
   - Copilot implements code

5. **Create PR:**
   - Copilot creates pull request
   - Links to original issue
   - Ready for human review

### Other AI Agents (Cursor, Windsurf, etc.)

1. **Copy Issue Content:**
   - Copy issue description from GitHub

2. **Paste to AI Agent:**
   - Paste into agent's chat/command interface
   - Agent reads context files
   - Agent implements solution

3. **Manual PR Creation:**
   - Commit changes
   - Create pull request
   - Link to issue

## Issue Template Structure

Each generated issue includes:

### 1. Task Metadata

```markdown
**Task ID:** TASK-1.3  
**Epic:** Epic 1: Core Infrastructure  
**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** TASK-1.2  
**Estimated Time:** 8 hours
```

### 2. Subtasks

```markdown
- [ ] Implement GitHelper.DetectGitRoot()
- [ ] Implement PathResolver.Resolve()
- [ ] Add cross-platform path handling
```

### 3. Acceptance Criteria

```markdown
- [ ] Git root detected correctly
- [ ] Paths resolved relative to git root
- [ ] Cross-platform tests pass
```

### 4. Technical Context

```markdown
### Project Structure
packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/

### Git Root Path Resolution
All paths relative to: D:\lunar-snake\constract-work\card-projects\sango-card\
```

### 5. AI Agent Instructions

```markdown
### For GitHub Copilot Workspace

1. Read Context Files:
   - .specify/specs/build-preparation-tool.md
   - .agent/base/30-path-resolution.md

2. Implement Subtasks:
   - Follow coding standards
   - Use dependency injection
   - Write unit tests

3. Validate:
   - All acceptance criteria met
   - Tests passing
   - No warnings
```

### 6. Success Criteria

```markdown
- [ ] All subtasks completed
- [ ] All acceptance criteria met
- [ ] Unit tests written (85%+ coverage)
- [ ] Code follows project standards
- [ ] No hardcoded paths
```

## Labels and Organization

### Automatic Labels

Issues are automatically labeled:

- **`spec-kit-task`** - Generated from spec-kit
- **`ai-agent-ready`** - Ready for AI execution
- **`priority-{level}`** - critical, high, medium, low
- **`story-points-{n}`** - Story point estimate
- **`epic-{name}`** - Epic grouping

### Manual Labels (Optional)

Add these manually if needed:

- **`blocked`** - Waiting on dependencies
- **`in-progress`** - Currently being worked on
- **`needs-review`** - Ready for human review
- **`ai-generated`** - Code generated by AI
- **`needs-refinement`** - Requires human refinement

## Dependency Management

### Task Dependencies

Issues include dependency information:

```markdown
**Dependencies:** TASK-1.2, TASK-1.3
```

### Handling Dependencies

1. **Check Dependencies:**
   - Before starting, ensure dependent issues are closed
   - Or dependent PRs are merged

2. **Block if Needed:**
   - Add `blocked` label
   - Comment on issue explaining blocker

3. **Notify:**
   - Mention dependent issue in comment
   - Link PRs together

## AI Agent Best Practices

### For Successful AI Execution

1. **Clear Context:**
   - All context files referenced
   - Path resolution rules clear
   - Coding standards documented

2. **Specific Acceptance Criteria:**
   - Measurable outcomes
   - Clear success definition
   - Test requirements specified

3. **Bounded Scope:**
   - Single responsibility
   - 5-8 story points max
   - 8-14 hours estimated

4. **Test Requirements:**
   - Unit test examples provided
   - Coverage target specified
   - Test framework specified

### What Makes a Task "AI-Agent-Ready"

✅ **Good for AI:**

- Well-defined scope
- Clear acceptance criteria
- Referenced context files
- Specific coding patterns
- Test requirements
- No ambiguity

❌ **Not Good for AI:**

- Vague requirements
- "Figure it out" tasks
- Requires human judgment
- Complex architecture decisions
- Cross-cutting concerns
- Requires domain expertise

## Monitoring and Review

### Track Progress

```bash
# List all spec-kit tasks
gh issue list --label "spec-kit-task"

# List AI-agent-ready tasks
gh issue list --label "ai-agent-ready"

# List by epic
gh issue list --label "epic-core-infrastructure"

# List by priority
gh issue list --label "priority-critical"
```

### Review Checklist

When reviewing AI-generated PRs:

- [ ] All acceptance criteria met
- [ ] Tests written and passing
- [ ] Code follows standards
- [ ] No hardcoded values
- [ ] Proper error handling
- [ ] Documentation complete
- [ ] No security issues
- [ ] Performance acceptable

## Example Workflow

### Complete Example: Task 1.3

**1. Generate Issue:**

```powershell
.\scripts\generate-github-issues.ps1 `
    -TaskFile ".specify\tasks\build-preparation-tool-tasks.md"
```

**2. Create GitHub Issue:**

```powershell
.\scripts\create-github-issues.ps1 `
    -IssueDir ".github\issues"
```

**3. Assign to Copilot:**

- Open issue #42 in GitHub
- Click "Open in Copilot Workspace"
- Review Copilot's plan
- Approve execution

**4. Copilot Implements:**

- Creates `GitHelper.cs`
- Creates `PathResolver.cs`
- Writes unit tests
- Creates PR #43

**5. Human Review:**

- Review PR #43
- Check acceptance criteria
- Run tests locally
- Approve and merge

**6. Update Spec-Kit:**

- Mark task as complete in `.specify/tasks/`
- Update progress tracking

## Troubleshooting

### Issue: AI Agent Can't Find Context Files

**Solution:**

- Ensure all referenced files exist
- Use correct relative paths from git root
- Check file permissions

### Issue: AI Agent Produces Wrong Code

**Solution:**

- Review acceptance criteria (too vague?)
- Add more specific examples
- Provide code snippets in issue
- Refine and re-assign

### Issue: Tests Failing

**Solution:**

- Check test requirements in issue
- Verify test framework setup
- Review test examples
- May need human intervention

### Issue: Dependencies Not Met

**Solution:**

- Check dependency issues are closed
- Verify dependent code is merged
- May need to reorder tasks

## Advanced: Batch Operations

### Create All Issues for an Epic

```powershell
# Generate all issues
.\scripts\generate-github-issues.ps1 `
    -TaskFile ".specify\tasks\build-preparation-tool-tasks.md"

# Create only Epic 1 issues
Get-ChildItem ".github\issues\task-1-*.md" | ForEach-Object {
    gh issue create --body-file $_.FullName
}
```

### Bulk Update Labels

```bash
# Add label to all spec-kit tasks
gh issue list --label "spec-kit-task" --json number --jq '.[].number' | \
  xargs -I {} gh issue edit {} --add-label "sprint-1"
```

### Close Completed Tasks

```bash
# Close all completed tasks
gh issue list --label "spec-kit-task" --state open --json number,title | \
  jq -r '.[] | select(.title | contains("[DONE]")) | .number' | \
  xargs -I {} gh issue close {}
```

## Integration with Spec-Kit

### Update Task Status

When issue is closed, update spec-kit:

```markdown
### Task 1.3: Path Resolver Implementation
**Status:** ✅ Completed  
**Story Points:** 5  
**Priority:** Critical  
**GitHub Issue:** #42  
**Pull Request:** #43  
**Completed:** 2025-10-17
```

### Track Progress

Add to `.specify/progress.md`:

```markdown
## Sprint 1 Progress

- [x] TASK-1.1: Project Setup (#40, PR #41)
- [x] TASK-1.2: DI Setup (#41, PR #42)
- [x] TASK-1.3: Path Resolver (#42, PR #43)
- [ ] TASK-1.4: Core Models (#43)
```

## Benefits

### For Developers

✅ **Less Manual Work:** AI handles boilerplate and implementation  
✅ **Faster Development:** Parallel execution of independent tasks  
✅ **Consistent Quality:** AI follows standards strictly  
✅ **Better Documentation:** Issues document decisions  

### For Project Management

✅ **Clear Tracking:** Every task is an issue  
✅ **Velocity Metrics:** Story points tracked  
✅ **Dependency Visibility:** Blockers clear  
✅ **Progress Transparency:** Real-time status  

### For AI Agents

✅ **Clear Instructions:** Unambiguous requirements  
✅ **Full Context:** All needed files referenced  
✅ **Success Criteria:** Know when done  
✅ **Standards:** Follow project conventions  

## Conclusion

This workflow bridges spec-kit's structured task breakdown with GitHub's issue tracking and AI agent execution capabilities. By generating well-structured, context-rich issues, we enable AI agents to autonomously implement features while maintaining human oversight and code quality.

**Key Success Factors:**

1. Clear, specific task definitions
2. Comprehensive context files
3. Measurable acceptance criteria
4. Proper dependency management
5. Human review of AI output

**Next Steps:**

1. Generate issues for your spec-kit tasks
2. Assign to AI agents
3. Review and merge PRs
4. Track progress in spec-kit
5. Iterate and improve
