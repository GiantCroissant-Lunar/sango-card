# Quick Start: Multi-Agent Workflow

## 🚀 Get Started in 5 Minutes

### Prerequisites

- [x] GitHub CLI installed: `gh --version`
- [x] GitHub CLI authenticated: `gh auth login`
- [x] GitHub Copilot enabled for repository
- [x] PowerShell 7+ installed

### Step 1: Generate Issues (2 minutes)

```powershell
# Navigate to repository root
cd D:\lunar-snake\constract-work\card-projects\sango-card

# Generate issue files from spec-kit tasks
.\scripts\generate-github-issues.ps1 `
    -TaskFile ".specify\tasks\build-preparation-tool-tasks-v2.md" `
    -OutputDir ".github\issues"
```

**Output:** 24 issue files created in `.github/issues/`

### Step 2: Create GitHub Issues (2 minutes)

```powershell
# Create all issues on GitHub
.\scripts\create-github-issues.ps1 `
    -IssueDir ".github\issues" `
    -Repository "owner/repo"
```

**Output:** 24 GitHub issues created with labels

### Step 3: Start Wave 1 (1 minute)

```powershell
# Label Wave 1 tasks as ready
gh issue edit 1 --add-label "wave-1-ready"
gh issue edit 2 --add-label "wave-1-ready"
```

**Or use GitHub web interface:**

1. Go to Issues
2. Find TASK-1.1 and TASK-1.2
3. Add label `wave-1-ready`

### Step 4: Watch the Magic ✨

The workflows will automatically:

1. ✅ Assign issues to GitHub Copilot
2. ✅ Copilot implements and creates PRs
3. ✅ Auto-review checks PRs
4. ✅ Auto-merge approved PRs
5. ✅ Trigger next wave when complete

## 📊 Monitor Progress

### View All Tasks

```bash
gh issue list --label "spec-kit-task"
```

### View Current Wave

```bash
gh issue list --label "wave-1-ready"
```

### View Blocked Tasks

```bash
gh issue list --label "blocked"
```

### View Progress Dashboard

```bash
gh issue view <dashboard-issue-number>
```

## 🔧 Manual Controls

### Manually Trigger Next Wave

```bash
gh workflow run wave-coordinator.yml -f wave=2
```

### Manually Assign to Copilot

```bash
gh issue edit <issue-number> --add-label "wave-N-ready"
```

### Manually Merge PR

```bash
gh pr merge <pr-number> --squash
```

## 🎯 What Happens Next

### Wave 1: Foundation (Serial)

**Duration:** ~10 hours

- TASK-1.1: Project Setup
- TASK-1.2: DI Setup

**When complete:** Wave 2 auto-triggers

### Wave 2: Core Components (Parallel)

**Duration:** ~8 hours

- TASK-1.3: PathResolver (parallel)
- TASK-1.4: Core Models (parallel)

**When complete:** Wave 3 auto-triggers

### Wave 3-8: Continue

All subsequent waves trigger automatically!

## ⚙️ Configuration

### Auto-Merge Settings

**Edit workflow:** `.github/workflows/copilot-assign-and-merge.yml`

```yaml
# Change coverage threshold
coverage >= 85  # Change to your threshold

# Change merge method
merge_method: 'squash'  # or 'merge', 'rebase'
```

### Wave Timing

**Edit task metadata:** `.specify/tasks/build-preparation-tool-tasks-v2.md`

```yaml
# Change wave assignment
wave: 2  # Move to different wave

# Change parallel group
parallel_group: "services"  # Group with other tasks
```

## 🐛 Troubleshooting

### Copilot Not Assigned?

**Check:**

```bash
# Is Copilot enabled?
gh api /repos/owner/repo/copilot

# Check issue labels
gh issue view <issue-number> --json labels
```

**Fix:**

```bash
# Manually assign
gh issue edit <issue-number> --add-assignee github-copilot
```

### Auto-Merge Not Working?

**Check:**

```bash
# View PR status
gh pr view <pr-number> --json statusCheckRollup,reviewDecision

# Check labels
gh pr view <pr-number> --json labels
```

**Fix:**

```bash
# Ensure label present
gh pr edit <pr-number> --add-label "auto-merge-eligible"

# Manually approve
gh pr review <pr-number> --approve

# Manually merge
gh pr merge <pr-number> --squash
```

### Wave Not Progressing?

**Check:**

```bash
# View wave status
gh issue list --label "wave-2" --json number,title,state

# Check dependencies
gh issue view <issue-number> --json body
```

**Fix:**

```bash
# Manually trigger wave coordinator
gh workflow run wave-coordinator.yml -f wave=2

# Or manually label next wave
gh issue edit <issue-number> --add-label "wave-3-ready"
```

## 📈 Expected Timeline

### With Multi-Agent (Parallel)

- **Week 1:** Waves 1-2 (Foundation + Core)
- **Week 2:** Waves 3-4 (Services)
- **Week 3:** Waves 5-6 (Preparation + Patchers)
- **Week 4:** Waves 7-8 (UI + Integration)

**Total:** ~4 weeks

### Without Multi-Agent (Serial)

- **Weeks 1-6:** All tasks sequential

**Total:** ~6 weeks

**Speedup:** 1.5x (conservative estimate)

## 🎓 Learning Resources

### Understanding the System

1. Read: `docs/AGENT-ORCHESTRATION.md`
2. Read: `docs/MULTI-AGENT-WORKFLOW-SUMMARY.md`
3. Review: `.specify/tasks/build-preparation-tool-tasks-v2.md`

### Workflows

1. `.github/workflows/copilot-assign-and-merge.yml`
2. `.github/workflows/wave-coordinator.yml`

### Scripts

1. `scripts/generate-github-issues.ps1`
2. `scripts/create-github-issues.ps1`

## 💡 Tips

### 1. Start Small

- Test with Wave 1 only
- Verify workflows work
- Then scale to all waves

### 2. Monitor Closely

- Watch first few PRs
- Check auto-review comments
- Verify auto-merge behavior

### 3. Adjust as Needed

- Tune coverage thresholds
- Adjust wave groupings
- Refine auto-merge criteria

### 4. Human Oversight

- Review critical PRs manually
- Check architecture decisions
- Validate integration tests

### 5. Iterate

- Learn from first wave
- Refine task metadata
- Improve acceptance criteria

## 🚨 Important Notes

### Auto-Merge Safety

- Only low-risk tasks auto-merge
- High-risk tasks require human review
- All changes are reversible

### Copilot Limitations

- May need guidance on complex tasks
- Review all generated code
- Test thoroughly

### GitHub API Limits

- Rate limits apply
- Workflows may queue
- Be patient with large batches

## ✅ Success Checklist

Before starting:

- [ ] Spec-kit tasks have execution metadata
- [ ] GitHub workflows committed
- [ ] Scripts tested locally
- [ ] GitHub Copilot enabled
- [ ] Team notified

After Wave 1:

- [ ] Issues created successfully
- [ ] Copilot assigned automatically
- [ ] PRs created by Copilot
- [ ] Auto-review ran
- [ ] Auto-merge worked (or manual merge)
- [ ] Wave 2 triggered

After All Waves:

- [ ] All tasks completed
- [ ] All PRs merged
- [ ] Code quality maintained
- [ ] Tests passing
- [ ] Documentation updated

## 🎉 You're Ready

Run the commands above and watch your tasks execute automatically!

**Questions?** Check the full documentation:

- `docs/AGENT-ORCHESTRATION.md`
- `docs/MULTI-AGENT-WORKFLOW-SUMMARY.md`
- `docs/SPEC-KIT-TO-GITHUB-ISSUES.md`

**Issues?** See troubleshooting section above or create a GitHub issue.

---

**Happy Automating! 🤖**
