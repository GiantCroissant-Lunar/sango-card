---
doc_id: DOC-2025-00147
title: Build Preparation Tool Tasks V2
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [build-preparation-tool-tasks-v2]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00098
title: Build Preparation Tool Tasks V2
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [build-preparation-tool-tasks-v2]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00096
title: Build Preparation Tool Tasks V2
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [build-preparation-tool-tasks-v2]
summary: >
  (Add summary here)
source:
  author: system
---
# TASKS: Build Preparation Tool Implementation v2

**Spec:** SPEC-BPT-001  
**RFC:** RFC-001  
**Sprint:** 6 weeks  
**Team Size:** 1-2 developers

## Task Execution Metadata

### Parallelization Strategy

Tasks are organized into **execution waves**. Tasks within the same wave can be executed in parallel.

```
Wave 1: Foundation (Serial)
  └─> Wave 2: Core Services (Parallel)
      └─> Wave 3: Specialized Services (Parallel)
          └─> Wave 4: UI Layers (Parallel)
              └─> Wave 5: Integration (Serial)
```

### Agent Roles

**Orchestrator Agent:**

- Parses spec-kit tasks
- Creates GitHub issues
- Manages dependencies
- Assigns to executor agents
- Monitors progress

**Executor Agents:**

- GitHub Copilot Workspace
- Local AI agents (Cursor, Windsurf)
- Implements assigned tasks
- Creates pull requests

**Reviewer Agent:**

- Reviews PRs
- Checks acceptance criteria
- Approves or requests changes

**Merger Agent:**

- Auto-merges approved PRs
- Triggers next wave
- Updates task status

## Epic 1: Core Infrastructure

### Execution Wave 1 (Serial - Foundation)

#### Task 1.1: Project Setup

**Execution Metadata:**

```yaml
wave: 1
parallel_group: null
can_run_parallel: false
blocks: [1.2, 1.3, 1.4]
blocked_by: []
agent_type: executor
estimated_duration: 4h
complexity: low
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 2  
**Priority:** Critical  
**Dependencies:** None

**Subtasks:**

- [ ] Create .NET 8.0 console project at `dotnet~/tool/`
- [ ] Add NuGet packages (see dependencies)
- [ ] Setup solution structure
- [ ] Configure build settings (PublishSingleFile, SelfContained)
- [ ] Setup .editorconfig
- [ ] Setup test project

**Acceptance Criteria:**

- [ ] Project builds successfully
- [ ] All NuGet packages restored
- [ ] Test project runs

**Estimated Time:** 4 hours

---

#### Task 1.2: Dependency Injection Setup

**Execution Metadata:**

```yaml
wave: 1
parallel_group: null
can_run_parallel: false
blocks: [1.3, 2.1, 2.2, 2.3, 2.4, 2.5]
blocked_by: [1.1]
agent_type: executor
estimated_duration: 6h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 3  
**Priority:** Critical  
**Dependencies:** Task 1.1

**Subtasks:**

- [ ] Implement Program.cs with HostBuilder
- [ ] Register core services in DI container
- [ ] Setup Microsoft.Extensions.Logging
- [ ] Setup MessagePipe
- [ ] Create service registration extensions

**Acceptance Criteria:**

- [ ] Host starts successfully
- [ ] Services resolve from DI
- [ ] Logging works
- [ ] MessagePipe configured

**Estimated Time:** 6 hours

---

### Execution Wave 2 (Parallel - Core Components)

#### Task 1.3: Path Resolver Implementation

**Execution Metadata:**

```yaml
wave: 2
parallel_group: "core-utilities"
can_run_parallel: true
parallel_with: [1.4]
blocks: [2.1, 2.2, 2.5]
blocked_by: [1.2]
agent_type: executor
estimated_duration: 8h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** Task 1.2

**Subtasks:**

- [ ] Implement GitHelper.DetectGitRoot()
- [ ] Implement PathResolver.Resolve()
- [ ] Implement PathResolver.MakeRelative()
- [ ] Add cross-platform path handling
- [ ] Add validation and error messages
- [ ] Write unit tests

**Acceptance Criteria:**

- [ ] Git root detected correctly
- [ ] Paths resolved relative to git root
- [ ] Clear error if git root not found
- [ ] Cross-platform tests pass
- [ ] 90% code coverage

**Estimated Time:** 8 hours

---

#### Task 1.4: Core Models

**Execution Metadata:**

```yaml
wave: 2
parallel_group: "core-utilities"
can_run_parallel: true
parallel_with: [1.3]
blocks: [2.1, 2.2, 2.3, 2.4, 2.5]
blocked_by: [1.2]
agent_type: executor
estimated_duration: 6h
complexity: low
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 3  
**Priority:** High  
**Dependencies:** Task 1.2

**Subtasks:**

- [ ] Create PreparationConfig model
- [ ] Create AssetManipulation model
- [ ] Create CodePatch model
- [ ] Create ScriptingDefineSymbol model
- [ ] Create CacheItem model
- [ ] Create ValidationResult model
- [ ] Add JSON serialization attributes

**Acceptance Criteria:**

- [ ] All models defined
- [ ] JSON serialization works
- [ ] Models immutable where appropriate
- [ ] XML documentation complete

**Estimated Time:** 6 hours

---

## Epic 2: Services Implementation

### Execution Wave 3 (Parallel - Services)

#### Task 2.1: ConfigService

**Execution Metadata:**

```yaml
wave: 3
parallel_group: "services"
can_run_parallel: true
parallel_with: [2.4]
blocks: [2.2, 2.3, 2.5]
blocked_by: [1.3, 1.4]
agent_type: executor
estimated_duration: 10h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 1.3, Task 1.4

**Subtasks:**

- [ ] Implement LoadAsync(string path)
- [ ] Implement SaveAsync(PreparationConfig config, string path)
- [ ] Implement AddPackage()
- [ ] Implement AddAssembly()
- [ ] Implement AddDefine()
- [ ] Implement AddPatch()
- [ ] Implement Remove()
- [ ] Add MessagePipe publishers
- [ ] Write unit tests

**Acceptance Criteria:**

- [ ] Config loads from JSON
- [ ] Config saves to JSON
- [ ] Paths resolved correctly
- [ ] Messages published on changes
- [ ] 85% code coverage

**Estimated Time:** 10 hours

---

#### Task 2.4: ManifestService

**Execution Metadata:**

```yaml
wave: 3
parallel_group: "services"
can_run_parallel: true
parallel_with: [2.1]
blocks: [2.5]
blocked_by: [1.3, 1.4]
agent_type: executor
estimated_duration: 6h
complexity: low
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 3  
**Priority:** Medium  
**Dependencies:** Task 1.3, Task 1.4

**Subtasks:**

- [ ] Implement ReadManifest()
- [ ] Implement AddPackage()
- [ ] Implement RemovePackage()
- [ ] Implement ModifyManifest()
- [ ] Handle JSON formatting
- [ ] Write unit tests

**Acceptance Criteria:**

- [ ] Manifest.json parsed correctly
- [ ] Packages added/removed
- [ ] Formatting preserved
- [ ] 85% code coverage

**Estimated Time:** 6 hours

---

### Execution Wave 4 (Parallel - Dependent Services)

#### Task 2.2: CacheService

**Execution Metadata:**

```yaml
wave: 4
parallel_group: "dependent-services"
can_run_parallel: true
parallel_with: [2.3]
blocks: [2.5]
blocked_by: [2.1]
agent_type: executor
estimated_duration: 10h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 2.1

**Subtasks:**

- [ ] Implement PopulateFromCodeQuality()
- [ ] Implement AddPackage()
- [ ] Implement AddAssembly()
- [ ] Implement ListCache()
- [ ] Implement Clean()
- [ ] Add config auto-update on cache changes
- [ ] Add MessagePipe publishers
- [ ] Write unit tests

**Acceptance Criteria:**

- [ ] Cache populates from code-quality
- [ ] Items added to cache
- [ ] Config updated automatically
- [ ] Messages published
- [ ] 85% code coverage

**Estimated Time:** 10 hours

---

#### Task 2.3: ValidationService

**Execution Metadata:**

```yaml
wave: 4
parallel_group: "dependent-services"
can_run_parallel: true
parallel_with: [2.2]
blocks: [2.5]
blocked_by: [2.1]
agent_type: executor
estimated_duration: 12h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 2.1

**Subtasks:**

- [ ] Implement ValidateSchema()
- [ ] Implement ValidateFileExistence()
- [ ] Implement ValidateUnityPackages()
- [ ] Implement ValidateCodePatches()
- [ ] Implement ValidateAll()
- [ ] Format validation results
- [ ] Add MessagePipe publishers
- [ ] Write unit tests

**Acceptance Criteria:**

- [ ] All 4 validation levels work
- [ ] Clear error messages
- [ ] File paths in errors
- [ ] Summary with counts
- [ ] 90% code coverage

**Estimated Time:** 12 hours

---

## Execution Waves Summary

### Wave 1: Foundation (Serial)

**Duration:** ~10 hours  
**Tasks:** 1.1, 1.2  
**Parallelization:** None (must be serial)

### Wave 2: Core Components (Parallel)

**Duration:** ~8 hours (max of parallel tasks)  
**Tasks:** 1.3, 1.4  
**Parallelization:** 2 tasks in parallel

### Wave 3: Services (Parallel)

**Duration:** ~10 hours (max of parallel tasks)  
**Tasks:** 2.1, 2.4  
**Parallelization:** 2 tasks in parallel

### Wave 4: Dependent Services (Parallel)

**Duration:** ~12 hours (max of parallel tasks)  
**Tasks:** 2.2, 2.3  
**Parallelization:** 2 tasks in parallel

### Wave 5: Preparation Service (Serial)

**Duration:** ~14 hours  
**Tasks:** 2.5  
**Parallelization:** None (depends on all services)

### Wave 6: Patchers (Parallel)

**Duration:** ~20 hours (max of parallel tasks)  
**Tasks:** 3.1, 3.2, 3.3, 3.4, 3.5  
**Parallelization:** 5 tasks in parallel (after 3.1)

### Wave 7: CLI & TUI (Parallel)

**Duration:** ~14 hours (max of parallel tasks)  
**Tasks:** 4.x, 5.x  
**Parallelization:** CLI and TUI in parallel

### Wave 8: Integration (Serial)

**Duration:** ~26 hours  
**Tasks:** 6.1, 6.2, 6.3, 6.4  
**Parallelization:** None (integration must be serial)

## Total Timeline

**Serial Execution:** 242 hours (6 weeks, 1 developer)  
**Parallel Execution:** ~104 hours (2.6 weeks, multiple agents)  
**Speedup:** 2.3x with parallelization

## Agent Assignment Strategy

### Auto-Merge Eligible Tasks

Tasks that can be auto-merged after approval:

- Low complexity
- No human review required
- High test coverage
- Clear acceptance criteria

**Examples:** 1.1, 1.2, 1.3, 1.4, 2.1, 2.4, 2.2

### Human Review Required Tasks

Tasks that need human review before merge:

- High complexity
- Critical functionality
- Complex algorithms
- Security implications

**Examples:** 2.3 (Validation), 3.4 (Unity YAML), 6.2 (E2E Tests)

## Dependency Graph

```
1.1 (Project Setup)
 └─> 1.2 (DI Setup)
      ├─> 1.3 (PathResolver) ──┐
      │                         ├─> 2.1 (ConfigService) ──┐
      └─> 1.4 (Core Models) ────┤                         ├─> 2.2 (CacheService) ──┐
                                 └─> 2.4 (ManifestService)─┤                        ├─> 2.5 (PreparationService)
                                                            └─> 2.3 (ValidationService)─┘
```

## Metadata Schema

```yaml
# Task Execution Metadata
wave: <number>                    # Execution wave number
parallel_group: <string>          # Group name for parallel tasks
can_run_parallel: <boolean>       # Can run in parallel with others
parallel_with: [<task_ids>]       # Tasks that can run in parallel
blocks: [<task_ids>]              # Tasks blocked by this task
blocked_by: [<task_ids>]          # Tasks blocking this task
agent_type: <executor|reviewer>   # Type of agent needed
estimated_duration: <hours>       # Time estimate
complexity: <low|medium|high>     # Task complexity
ai_agent_ready: <boolean>         # Ready for AI execution
requires_human_review: <boolean>  # Needs human review
auto_merge_eligible: <boolean>    # Can be auto-merged
```
