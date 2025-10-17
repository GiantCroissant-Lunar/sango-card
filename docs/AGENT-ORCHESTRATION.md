# Agent Orchestration System

## Overview

Multi-agent system for autonomous task execution with parallel processing, dependency management, and auto-merge capabilities.

## Agent Roles

### 1. Orchestrator Agent

**Responsibility:** Task distribution and workflow management

**Capabilities:**

- Parse spec-kit tasks with execution metadata
- Create GitHub issues with wave/dependency info
- Monitor task dependencies
- Trigger next wave when dependencies met
- Track overall progress

**Implementation:** GitHub Actions workflow

---

### 2. Executor Agents

**Responsibility:** Implement assigned tasks

**Types:**

- **GitHub Copilot Workspace** (Primary)
- **Local AI Agents** (Cursor, Windsurf, Claude)

**Capabilities:**

- Read task requirements
- Implement code
- Write tests
- Create pull requests
- Self-validate against acceptance criteria

---

### 3. Reviewer Agent

**Responsibility:** Automated code review

**Capabilities:**

- Check acceptance criteria
- Run tests
- Check code coverage
- Verify coding standards
- Approve or request changes

**Implementation:** GitHub Actions + AI review

---

### 4. Merger Agent

**Responsibility:** Auto-merge approved PRs

**Capabilities:**

- Verify all checks passed
- Verify approval status
- Merge PR
- Trigger next wave
- Update task status

**Implementation:** GitHub Actions

---

## Execution Workflow

```
┌─────────────────────────────────────────────────────────────┐
│                    Orchestrator Agent                        │
│  1. Parse spec-kit tasks                                    │
│  2. Create GitHub issues with metadata                      │
│  3. Organize into execution waves                           │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    Wave 1: Foundation                        │
│  ┌──────────┐                                               │
│  │ Task 1.1 │ ──> Executor Agent ──> PR ──> Review ──> Merge│
│  └──────────┘                                               │
│  ┌──────────┐                                               │
│  │ Task 1.2 │ ──> Executor Agent ──> PR ──> Review ──> Merge│
│  └──────────┘                                               │
└──────────────────────┬──────────────────────────────────────┘
                       │ All Wave 1 merged
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    Wave 2: Core (Parallel)                   │
│  ┌──────────┐                                               │
│  │ Task 1.3 │ ──┐                                           │
│  └──────────┘   │                                           │
│                 ├──> Executor Agents ──> PRs ──> Review ──> Merge
│  ┌──────────┐   │                                           │
│  │ Task 1.4 │ ──┘                                           │
│  └──────────┘                                               │
└──────────────────────┬──────────────────────────────────────┘
                       │ All Wave 2 merged
                       ▼
                    [Continue...]
```

## GitHub Actions Workflows

### Workflow 1: Orchestrator - Create Issues

**File:** `.github/workflows/orchestrator-create-issues.yml`

**Trigger:** Manual or on spec-kit update

**Steps:**

1. Parse spec-kit tasks
2. Extract execution metadata
3. Create GitHub issues
4. Add labels (wave, parallel-group, etc.)
5. Set up dependency tracking

---

### Workflow 2: Executor - Assign to Copilot

**File:** `.github/workflows/executor-assign-copilot.yml`

**Trigger:** Issue labeled `wave-N-ready`

**Steps:**

1. Check if dependencies met
2. Assign to GitHub Copilot (via GraphQL)
3. Monitor Copilot progress
4. Wait for PR creation

**GraphQL Mutation:**

```graphql
mutation AssignCopilot($issueId: ID!) {
  assignCopilotToIssue(input: {
    issueId: $issueId
    copilotId: "github-copilot"
  }) {
    issue {
      id
      number
      assignees {
        nodes {
          login
        }
      }
    }
  }
}
```

---

### Workflow 3: Reviewer - Auto Review

**File:** `.github/workflows/reviewer-auto-review.yml`

**Trigger:** PR opened with label `spec-kit-task`

**Steps:**

1. Checkout code
2. Run tests
3. Check code coverage
4. Verify acceptance criteria
5. Run linters
6. Post review comments
7. Approve if all checks pass (for auto-merge-eligible tasks)
8. Request changes if issues found

---

### Workflow 4: Merger - Auto Merge

**File:** `.github/workflows/merger-auto-merge.yml`

**Trigger:** PR approved + all checks passed

**Steps:**

1. Verify PR has `auto-merge-eligible` label
2. Verify all required checks passed
3. Verify approval from reviewer
4. Merge PR
5. Close linked issue
6. Trigger next wave if dependencies met
7. Update task status in spec-kit

---

### Workflow 5: Wave Coordinator

**File:** `.github/workflows/wave-coordinator.yml`

**Trigger:** Issue closed or PR merged

**Steps:**

1. Check which wave the task belongs to
2. Check if all tasks in wave are complete
3. If wave complete, trigger next wave:
   - Label next wave issues as `wave-N-ready`
   - Assign to Copilot
4. Update progress dashboard

---

## Dependency Management

### Dependency Tracking

Each issue has metadata:

```yaml
blocked_by: [TASK-1.1, TASK-1.2]
blocks: [TASK-2.1, TASK-2.2]
wave: 3
```

### Dependency Resolution

**Workflow:**

1. Issue created with `blocked` label
2. Monitor `blocked_by` tasks
3. When all dependencies closed:
   - Remove `blocked` label
   - Add `wave-N-ready` label
   - Assign to executor agent

**Implementation:**

```yaml
# .github/workflows/dependency-resolver.yml
name: Dependency Resolver

on:
  issues:
    types: [closed]

jobs:
  resolve:
    runs-on: ubuntu-latest
    steps:
      - name: Find blocked issues
        uses: actions/github-script@v7
        with:
          script: |
            const closedIssue = context.payload.issue.number;

            // Find issues blocked by this one
            const issues = await github.rest.issues.listForRepo({
              owner: context.repo.owner,
              repo: context.repo.repo,
              labels: 'blocked',
              state: 'open'
            });

            for (const issue of issues.data) {
              const body = issue.body;
              const blockedByMatch = body.match(/blocked_by: \[(.*?)\]/);

              if (blockedByMatch) {
                const blockedBy = blockedByMatch[1]
                  .split(',')
                  .map(t => t.trim());

                // Check if this issue was blocking
                if (blockedBy.includes(`TASK-${closedIssue}`)) {
                  // Remove from blocked_by list
                  // Check if all dependencies met
                  const allMet = await checkDependencies(blockedBy);

                  if (allMet) {
                    // Unblock and trigger
                    await github.rest.issues.removeLabel({
                      owner: context.repo.owner,
                      repo: context.repo.repo,
                      issue_number: issue.number,
                      name: 'blocked'
                    });

                    // Add wave-ready label
                    const wave = extractWave(issue.body);
                    await github.rest.issues.addLabels({
                      owner: context.repo.owner,
                      repo: context.repo.repo,
                      issue_number: issue.number,
                      labels: [`wave-${wave}-ready`]
                    });
                  }
                }
              }
            }
```

---

## Auto-Merge System

### Requirements for Auto-Merge

A PR can be auto-merged if:

1. ✅ Issue has `auto-merge-eligible: true`
2. ✅ All required checks passed
3. ✅ Code coverage meets threshold (85%+)
4. ✅ Reviewer agent approved
5. ✅ No merge conflicts
6. ✅ Branch up to date with main

### Auto-Merge Workflow

```yaml
# .github/workflows/auto-merge.yml
name: Auto Merge

on:
  pull_request_review:
    types: [submitted]
  check_suite:
    types: [completed]

jobs:
  auto-merge:
    if: |
      github.event.review.state == 'approved' ||
      github.event.check_suite.conclusion == 'success'
    runs-on: ubuntu-latest
    steps:
      - name: Check if auto-merge eligible
        id: check
        uses: actions/github-script@v7
        with:
          script: |
            const pr = context.payload.pull_request;
            const issue = await github.rest.issues.get({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: pr.number
            });

            const labels = issue.data.labels.map(l => l.name);
            const autoMergeEligible = labels.includes('auto-merge-eligible');

            return autoMergeEligible;

      - name: Check all requirements
        if: steps.check.outputs.result == 'true'
        uses: actions/github-script@v7
        with:
          script: |
            const pr = context.payload.pull_request;

            // Check reviews
            const reviews = await github.rest.pulls.listReviews({
              owner: context.repo.owner,
              repo: context.repo.repo,
              pull_number: pr.number
            });

            const approved = reviews.data.some(r => r.state === 'APPROVED');

            // Check status checks
            const checks = await github.rest.checks.listForRef({
              owner: context.repo.owner,
              repo: context.repo.repo,
              ref: pr.head.sha
            });

            const allPassed = checks.data.check_runs.every(
              c => c.conclusion === 'success'
            );

            // Check coverage
            const coverage = await getCoverage(pr.number);
            const coverageOk = coverage >= 85;

            if (approved && allPassed && coverageOk) {
              // Merge!
              await github.rest.pulls.merge({
                owner: context.repo.owner,
                repo: context.repo.repo,
                pull_number: pr.number,
                merge_method: 'squash'
              });

              console.log(`✅ Auto-merged PR #${pr.number}`);

              // Trigger next wave
              await triggerNextWave(pr.number);
            }
```

### Manual Override

For tasks requiring human review:

- `requires_human_review: true`
- `auto_merge_eligible: false`
- Human must manually approve and merge

---

## Parallel Execution

### Parallel Groups

Tasks in the same wave and parallel group can run simultaneously:

```yaml
# Task 1.3
wave: 2
parallel_group: "core-utilities"
parallel_with: [1.4]

# Task 1.4
wave: 2
parallel_group: "core-utilities"
parallel_with: [1.3]
```

### Parallel Execution Workflow

```yaml
# .github/workflows/parallel-executor.yml
name: Parallel Executor

on:
  workflow_dispatch:
    inputs:
      wave:
        description: 'Wave number to execute'
        required: true

jobs:
  find-parallel-tasks:
    runs-on: ubuntu-latest
    outputs:
      tasks: ${{ steps.find.outputs.tasks }}
    steps:
      - name: Find tasks in wave
        id: find
        uses: actions/github-script@v7
        with:
          script: |
            const wave = context.payload.inputs.wave;

            const issues = await github.rest.issues.listForRepo({
              owner: context.repo.owner,
              repo: context.repo.repo,
              labels: [`wave-${wave}`, 'wave-ready'],
              state: 'open'
            });

            return issues.data.map(i => i.number);

  execute-parallel:
    needs: find-parallel-tasks
    runs-on: ubuntu-latest
    strategy:
      matrix:
        task: ${{ fromJson(needs.find-parallel-tasks.outputs.tasks) }}
      max-parallel: 10  # Execute up to 10 tasks in parallel
    steps:
      - name: Assign to Copilot
        uses: ./.github/actions/assign-copilot
        with:
          issue-number: ${{ matrix.task }}
```

---

## Progress Dashboard

### Real-Time Progress Tracking

Create a GitHub issue as progress dashboard:

**Title:** `[DASHBOARD] Build Preparation Tool Progress`

**Body:**

```markdown
# Build Preparation Tool - Progress Dashboard

**Last Updated:** 2025-10-17 09:50 UTC

## Overall Progress

**Completed:** 12/24 tasks (50%)  
**In Progress:** 4 tasks  
**Blocked:** 3 tasks  
**Ready:** 5 tasks

## Wave Status

### ✅ Wave 1: Foundation (Complete)
- ✅ TASK-1.1: Project Setup (#40, PR #41) - Merged
- ✅ TASK-1.2: DI Setup (#42, PR #43) - Merged

### 🔄 Wave 2: Core Components (In Progress)
- 🔄 TASK-1.3: Path Resolver (#44, PR #45) - Review
- 🔄 TASK-1.4: Core Models (#46, PR #47) - Review

### ⏳ Wave 3: Services (Ready)
- ⏳ TASK-2.1: ConfigService (#48) - Ready
- ⏳ TASK-2.4: ManifestService (#49) - Ready

### 🔒 Wave 4: Dependent Services (Blocked)
- 🔒 TASK-2.2: CacheService (#50) - Blocked by 2.1
- 🔒 TASK-2.3: ValidationService (#51) - Blocked by 2.1

## Velocity

**Sprint 1:**
- Planned: 20 story points
- Completed: 15 story points
- Velocity: 75%

**Estimated Completion:** Week 5 (on track)

## Blockers

1. **TASK-2.2** blocked by TASK-2.1 (in review)
2. **TASK-2.3** blocked by TASK-2.1 (in review)
3. **TASK-2.5** blocked by TASK-2.2, TASK-2.3

## Recent Activity

- 2025-10-17 09:45: PR #47 opened for TASK-1.4
- 2025-10-17 09:30: PR #45 opened for TASK-1.3
- 2025-10-17 09:00: TASK-1.2 merged
```

**Auto-Update Workflow:**

```yaml
# .github/workflows/update-dashboard.yml
name: Update Dashboard

on:
  issues:
    types: [opened, closed, labeled]
  pull_request:
    types: [opened, closed, merged]

jobs:
  update:
    runs-on: ubuntu-latest
    steps:
      - name: Update dashboard
        uses: actions/github-script@v7
        with:
          script: |
            // Find dashboard issue
            const dashboard = await findDashboard();

            // Collect stats
            const stats = await collectStats();

            // Generate markdown
            const body = generateDashboard(stats);

            // Update issue
            await github.rest.issues.update({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: dashboard.number,
              body: body
            });
```

---

## Agent Coordination Patterns

### Pattern 1: Sequential with Checkpoints

```
Agent 1 → Checkpoint → Agent 2 → Checkpoint → Agent 3
```

Use for: Foundation tasks, integration tasks

### Pattern 2: Parallel with Merge

```
Agent 1 ──┐
Agent 2 ──┼──> Merge → Next Wave
Agent 3 ──┘
```

Use for: Independent services, patchers

### Pattern 3: Pipeline

```
Agent 1 → Agent 2 → Agent 3 → Agent 4
         (each waits for previous)
```

Use for: Dependent services

### Pattern 4: Fan-Out/Fan-In

```
        ┌─> Agent 1 ──┐
Trigger ├─> Agent 2 ──┼─> Merge → Continue
        └─> Agent 3 ──┘
```

Use for: Parallel patchers, parallel UI components

---

## Best Practices

### 1. Task Granularity

- Keep tasks 4-14 hours
- Single responsibility
- Clear acceptance criteria

### 2. Dependency Management

- Minimize dependencies
- Make dependencies explicit
- Use waves to organize

### 3. Auto-Merge Criteria

- High test coverage (85%+)
- Low complexity
- Clear requirements
- No security implications

### 4. Human Review Triggers

- High complexity
- Security-sensitive
- Architecture decisions
- Integration tasks

### 5. Error Handling

- Retry failed tasks (max 3 times)
- Escalate to human after retries
- Log all failures
- Maintain audit trail

---

## Monitoring and Alerts

### Metrics to Track

1. **Velocity:** Story points per week
2. **Cycle Time:** Issue open to PR merged
3. **Success Rate:** Auto-merge vs manual intervention
4. **Blocker Time:** Time tasks spend blocked
5. **Agent Utilization:** Parallel execution efficiency

### Alerts

- Task blocked > 24 hours
- PR pending review > 12 hours
- Test failures > 2 consecutive
- Coverage drops below threshold
- Wave completion delayed

---

## Example: Complete Flow

### Scenario: Execute Wave 2 (Parallel)

**1. Wave 1 Complete:**

- All tasks merged
- Wave coordinator triggered

**2. Wave 2 Preparation:**

- Label TASK-1.3 and TASK-1.4 as `wave-2-ready`
- Remove `blocked` labels

**3. Parallel Execution:**

- Assign TASK-1.3 to Copilot Agent A
- Assign TASK-1.4 to Copilot Agent B
- Both execute simultaneously

**4. PR Creation:**

- Agent A creates PR #45 for TASK-1.3
- Agent B creates PR #47 for TASK-1.4

**5. Auto Review:**

- Reviewer agent checks both PRs
- Runs tests, checks coverage
- Approves both (auto-merge-eligible)

**6. Auto Merge:**

- PR #45 merged
- PR #47 merged
- Both issues closed

**7. Wave 3 Trigger:**

- Wave coordinator detects Wave 2 complete
- Labels Wave 3 tasks as ready
- Assigns to agents

**Total Time:** ~8 hours (vs 14 hours sequential)

---

## Conclusion

This multi-agent orchestration system enables:

- ✅ Parallel task execution (2.3x speedup)
- ✅ Automatic dependency management
- ✅ Auto-merge for eligible tasks
- ✅ Human oversight for critical tasks
- ✅ Real-time progress tracking
- ✅ Efficient resource utilization

**Next Steps:**

1. Implement GitHub Actions workflows
2. Test with Wave 1 tasks
3. Monitor and refine
4. Scale to full project
