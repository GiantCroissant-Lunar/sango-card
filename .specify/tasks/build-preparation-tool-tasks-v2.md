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

#### Task 2.5: PreparationService

**Execution Metadata:**

```yaml
wave: 5
parallel_group: null
can_run_parallel: false
blocks: [4.1, 4.2, 4.3, 5.1]
blocked_by: [2.1, 2.2, 2.3, 2.4]
agent_type: executor
estimated_duration: 14h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** Task 2.1, 2.2, 2.3, 2.4

**Subtasks:**

- [ ] Implement ExecuteAsync() orchestration
- [ ] Implement backup/restore mechanism
- [ ] Implement rollback on error
- [ ] Coordinate all patchers and services
- [ ] Add progress reporting via MessagePipe
- [ ] Add dry-run mode support
- [ ] Implement validation before execution
- [ ] Write unit tests
- [ ] Write integration tests

**Acceptance Criteria:**

- [ ] Full preparation workflow executes
- [ ] Backup created before changes
- [ ] Rollback works on error
- [ ] Progress events published
- [ ] Dry-run mode doesn't modify files
- [ ] Completes in < 30 seconds
- [ ] 85% code coverage

**Estimated Time:** 14 hours

---

## Epic 3: Code Patchers

### Execution Wave 6 (Parallel - Patchers)

#### Task 3.1: Patcher Interface & Base

**Execution Metadata:**

```yaml
wave: 6
parallel_group: null
can_run_parallel: false
blocks: [3.2, 3.3, 3.4, 3.5]
blocked_by: [1.4]
agent_type: executor
estimated_duration: 4h
complexity: low
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 2  
**Priority:** High  
**Dependencies:** Task 1.4

**Subtasks:**

- [ ] Define IPatcher interface
- [ ] Implement PatcherBase abstract class
- [ ] Add validation hooks
- [ ] Add rollback support
- [ ] Add dry-run support
- [ ] Write unit tests

**Acceptance Criteria:**

- [ ] Interface clearly defined
- [ ] Base class provides common functionality
- [ ] All patchers can extend base
- [ ] Validation and rollback supported
- [ ] 90% code coverage

**Estimated Time:** 4 hours

---

#### Task 3.2: CSharpPatcher (Roslyn)

**Execution Metadata:**

```yaml
wave: 6
parallel_group: "patchers"
can_run_parallel: true
parallel_with: [3.3, 3.5]
blocks: [2.5]
blocked_by: [3.1]
agent_type: executor
estimated_duration: 16h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** Task 3.1

**Subtasks:**

- [ ] Implement RemoveUsing operation
- [ ] Implement ReplaceExpression operation
- [ ] Implement ReplaceBlock operation
- [ ] Implement RemoveBlock operation
- [ ] Preserve formatting and trivia
- [ ] Validate syntax after patching
- [ ] Add comprehensive error messages
- [ ] Write unit tests for each operation
- [ ] Write integration tests

**Acceptance Criteria:**

- [ ] All 4 operations work correctly
- [ ] Formatting preserved
- [ ] Syntax validated after patch
- [ ] Clear errors on failure
- [ ] Handles edge cases (nested, comments)
- [ ] 90% code coverage

**Estimated Time:** 16 hours

---

#### Task 3.3: JsonPatcher

**Execution Metadata:**

```yaml
wave: 6
parallel_group: "patchers"
can_run_parallel: true
parallel_with: [3.2, 3.5]
blocks: [2.5]
blocked_by: [3.1]
agent_type: executor
estimated_duration: 8h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 3.1

**Subtasks:**

- [ ] Implement AddProperty operation
- [ ] Implement RemoveProperty operation
- [ ] Implement ReplaceValue operation
- [ ] Preserve JSON formatting
- [ ] Validate JSON after patching
- [ ] Handle nested objects and arrays
- [ ] Write unit tests

**Acceptance Criteria:**

- [ ] All 3 operations work
- [ ] Formatting preserved
- [ ] JSON validated
- [ ] Nested structures handled
- [ ] 85% code coverage

**Estimated Time:** 8 hours

---

#### Task 3.4: UnityAssetPatcher (YAML)

**Execution Metadata:**

```yaml
wave: 6
parallel_group: null
can_run_parallel: false
blocks: [2.5]
blocked_by: [3.1]
agent_type: executor
estimated_duration: 20h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 13  
**Priority:** High  
**Dependencies:** Task 3.1

**Subtasks:**

- [ ] Implement YAML parsing with YamlDotNet
- [ ] Implement ModifyProperty operation
- [ ] Implement AddComponent operation
- [ ] Implement RemoveComponent operation
- [ ] Handle Unity YAML quirks
- [ ] Preserve Unity YAML format
- [ ] Validate Unity asset structure
- [ ] Write unit tests with real Unity assets
- [ ] Document Unity version compatibility

**Acceptance Criteria:**

- [ ] Unity YAML parsed correctly
- [ ] All operations work
- [ ] Unity format preserved
- [ ] Asset remains valid in Unity
- [ ] Works with Unity 6000.2.x
- [ ] 85% code coverage

**Estimated Time:** 20 hours

---

#### Task 3.5: TextPatcher (Regex)

**Execution Metadata:**

```yaml
wave: 6
parallel_group: "patchers"
can_run_parallel: true
parallel_with: [3.2, 3.3]
blocks: [2.5]
blocked_by: [3.1]
agent_type: executor
estimated_duration: 6h
complexity: low
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 3  
**Priority:** Medium  
**Dependencies:** Task 3.1

**Subtasks:**

- [ ] Implement Replace operation (regex)
- [ ] Implement InsertBefore operation
- [ ] Implement InsertAfter operation
- [ ] Implement RemoveLine operation
- [ ] Add regex validation
- [ ] Write unit tests

**Acceptance Criteria:**

- [ ] All 4 operations work
- [ ] Regex patterns validated
- [ ] Clear errors on invalid regex
- [ ] 85% code coverage

**Estimated Time:** 6 hours

---

## Epic 4: CLI Implementation

### Execution Wave 7 (Parallel - CLI Commands)

#### Task 4.1: Config Command Group

**Execution Metadata:**

```yaml
wave: 7
parallel_group: "cli-commands"
can_run_parallel: true
parallel_with: [4.2, 4.3]
blocks: [6.1]
blocked_by: [2.5]
agent_type: executor
estimated_duration: 10h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 2.5

**Subtasks:**

- [ ] Implement `config create` command
- [ ] Implement `config validate` command
- [ ] Implement `config add-package` command
- [ ] Implement `config add-assembly` command
- [ ] Implement `config add-define` command
- [ ] Implement `config add-patch` command
- [ ] Add argument validation
- [ ] Add progress output
- [ ] Add error handling
- [ ] Write integration tests

**Acceptance Criteria:**

- [ ] All 6 subcommands work
- [ ] Arguments validated
- [ ] Clear error messages
- [ ] Exit codes correct (0 = success)
- [ ] Progress to stdout, errors to stderr
- [ ] 85% code coverage

**Estimated Time:** 10 hours

---

#### Task 4.2: Cache Command Group

**Execution Metadata:**

```yaml
wave: 7
parallel_group: "cli-commands"
can_run_parallel: true
parallel_with: [4.1, 4.3]
blocks: [6.1]
blocked_by: [2.5]
agent_type: executor
estimated_duration: 8h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** Task 2.5

**Subtasks:**

- [ ] Implement `cache populate` command
- [ ] Implement `cache list` command
- [ ] Implement `cache add-package` command
- [ ] Implement `cache add-assembly` command
- [ ] Implement `cache clean` command
- [ ] Add progress output
- [ ] Add error handling
- [ ] Write integration tests

**Acceptance Criteria:**

- [ ] All 5 subcommands work
- [ ] Progress output clear
- [ ] Cache-config sync works
- [ ] Exit codes correct
- [ ] 85% code coverage

**Estimated Time:** 8 hours

---

#### Task 4.3: Prepare Command Group

**Execution Metadata:**

```yaml
wave: 7
parallel_group: "cli-commands"
can_run_parallel: true
parallel_with: [4.1, 4.2]
blocks: [6.1]
blocked_by: [2.5]
agent_type: executor
estimated_duration: 12h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** Task 2.5

**Subtasks:**

- [ ] Implement `prepare run` command
- [ ] Implement `prepare restore` command
- [ ] Implement `prepare dry-run` command
- [ ] Add --verbose flag
- [ ] Add --force flag
- [ ] Add detailed progress output
- [ ] Add error handling and rollback
- [ ] Write integration tests
- [ ] Write E2E tests

**Acceptance Criteria:**

- [ ] Full preparation executes from CLI
- [ ] Restore works correctly
- [ ] Dry-run doesn't modify files
- [ ] Progress output informative
- [ ] Errors clearly reported
- [ ] Rollback on failure
- [ ] 90% code coverage

**Estimated Time:** 12 hours

---

## Epic 5: TUI Implementation

### Execution Wave 7 (Parallel - TUI Views)

#### Task 5.1: TUI Host & Navigation

**Execution Metadata:**

```yaml
wave: 7
parallel_group: "tui-foundation"
can_run_parallel: false
blocks: [5.2, 5.3, 5.4, 5.5]
blocked_by: [2.5]
agent_type: executor
estimated_duration: 12h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 2.5

**Subtasks:**

- [ ] Implement Terminal.Gui v2 MainWindow
- [ ] Implement navigation menu
- [ ] Implement keyboard shortcuts (F1-F12)
- [ ] Implement view switching
- [ ] Setup ReactiveUI bindings
- [ ] Implement AppState observable
- [ ] Add error dialog
- [ ] Add confirmation dialog
- [ ] Write UI tests

**Acceptance Criteria:**

- [ ] TUI launches successfully
- [ ] Navigation between views works
- [ ] Keyboard shortcuts work
- [ ] Responsive UI (< 100ms updates)
- [ ] Handles window resize
- [ ] Error dialogs display correctly

**Estimated Time:** 12 hours

---

#### Task 5.2: Cache Management View

**Execution Metadata:**

```yaml
wave: 8
parallel_group: "tui-views"
can_run_parallel: true
parallel_with: [5.3, 5.4]
blocks: [6.2]
blocked_by: [5.1]
agent_type: executor
estimated_duration: 10h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** Task 5.1

**Subtasks:**

- [ ] Implement cache list view (ListView)
- [ ] Implement add package dialog
- [ ] Implement add assembly dialog
- [ ] Implement remove confirmation
- [ ] Implement populate from code-quality
- [ ] Bind to CacheService via ReactiveUI
- [ ] Add keyboard shortcuts
- [ ] Write UI tests

**Acceptance Criteria:**

- [ ] Cache items displayed
- [ ] Add/remove works
- [ ] Populate works
- [ ] Real-time updates
- [ ] Keyboard navigation works
- [ ] Config auto-updates

**Estimated Time:** 10 hours

---

#### Task 5.3: Config Editor View

**Execution Metadata:**

```yaml
wave: 8
parallel_group: "tui-views"
can_run_parallel: true
parallel_with: [5.2, 5.4]
blocks: [6.2]
blocked_by: [5.1]
agent_type: executor
estimated_duration: 14h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 5.1

**Subtasks:**

- [ ] Implement config tree view
- [ ] Implement package editor
- [ ] Implement assembly editor
- [ ] Implement scripting defines editor
- [ ] Implement code patch editor
- [ ] Implement asset manipulation editor
- [ ] Bind to ConfigService via ReactiveUI
- [ ] Add validation indicators
- [ ] Add save/discard functionality
- [ ] Write UI tests

**Acceptance Criteria:**

- [ ] All config sections editable
- [ ] Validation shown real-time
- [ ] Save/discard works
- [ ] Changes reactive
- [ ] Keyboard navigation works
- [ ] No data loss

**Estimated Time:** 14 hours

---

#### Task 5.4: Validation View

**Execution Metadata:**

```yaml
wave: 8
parallel_group: "tui-views"
can_run_parallel: true
parallel_with: [5.2, 5.3]
blocks: [6.2]
blocked_by: [5.1]
agent_type: executor
estimated_duration: 8h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** Task 5.1

**Subtasks:**

- [ ] Implement validation results tree
- [ ] Implement error detail view
- [ ] Implement re-validate button
- [ ] Color-code by severity
- [ ] Bind to ValidationService via ReactiveUI
- [ ] Add jump-to-error functionality
- [ ] Write UI tests

**Acceptance Criteria:**

- [ ] Validation results displayed
- [ ] Grouped by level
- [ ] Color-coded (red/yellow/green)
- [ ] Re-validate works
- [ ] Real-time updates
- [ ] Error details shown

**Estimated Time:** 8 hours

---

#### Task 5.5: Preparation Execution View

**Execution Metadata:**

```yaml
wave: 8
parallel_group: null
can_run_parallel: false
blocks: [6.2]
blocked_by: [5.1]
agent_type: executor
estimated_duration: 10h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 5.1

**Subtasks:**

- [ ] Implement progress bar view
- [ ] Implement step-by-step display
- [ ] Implement log output view
- [ ] Implement run/restore/dry-run buttons
- [ ] Bind to PreparationService via ReactiveUI
- [ ] Subscribe to progress messages
- [ ] Add cancel functionality
- [ ] Write UI tests

**Acceptance Criteria:**

- [ ] Preparation progress shown
- [ ] Steps displayed clearly
- [ ] Logs scrollable
- [ ] Run/restore/dry-run work
- [ ] Cancel works
- [ ] Real-time updates

**Estimated Time:** 10 hours

---

## Epic 6: Testing & Integration

### Execution Wave 9 (Serial - Testing)

#### Task 6.1: Unit Tests

**Execution Metadata:**

```yaml
wave: 9
parallel_group: null
can_run_parallel: false
blocks: [6.2]
blocked_by: [4.1, 4.2, 4.3]
agent_type: executor
estimated_duration: 16h
complexity: medium
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 4.1, 4.2, 4.3

**Subtasks:**

- [ ] Write tests for PathResolver
- [ ] Write tests for all Services
- [ ] Write tests for all Patchers
- [ ] Write tests for CLI commands
- [ ] Setup code coverage reporting
- [ ] Achieve 80% coverage target
- [ ] Add mock Unity assets
- [ ] Add test fixtures

**Acceptance Criteria:**

- [ ] 80% code coverage
- [ ] All critical paths tested
- [ ] Edge cases covered
- [ ] Mocks for external dependencies
- [ ] Tests run in < 30 seconds
- [ ] CI-ready

**Estimated Time:** 16 hours

---

#### Task 6.2: Integration Tests

**Execution Metadata:**

```yaml
wave: 9
parallel_group: null
can_run_parallel: false
blocks: [6.3]
blocked_by: [6.1, 5.2, 5.3, 5.4, 5.5]
agent_type: executor
estimated_duration: 14h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 6.1, 5.2, 5.3, 5.4, 5.5

**Subtasks:**

- [ ] Write cache population integration test
- [ ] Write config loading/saving integration test
- [ ] Write full preparation workflow test
- [ ] Write CLI command integration tests
- [ ] Write TUI interaction tests
- [ ] Setup test Unity project
- [ ] Add performance assertions
- [ ] Add rollback tests

**Acceptance Criteria:**

- [ ] All integration scenarios tested
- [ ] Real file system operations
- [ ] Test Unity project used
- [ ] Performance validated (< 30s)
- [ ] Rollback tested
- [ ] Tests run in CI

**Estimated Time:** 14 hours

---

#### Task 6.3: E2E Tests

**Execution Metadata:**

```yaml
wave: 9
parallel_group: null
can_run_parallel: false
blocks: [6.4]
blocked_by: [6.2]
agent_type: executor
estimated_duration: 12h
complexity: high
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** Task 6.2

**Subtasks:**

- [ ] Create E2E test Unity project
- [ ] Write prepare → build → restore E2E test
- [ ] Write config create → populate → validate E2E test
- [ ] Write TUI workflow E2E test
- [ ] Test on Windows/Linux/macOS
- [ ] Add smoke tests
- [ ] Document test setup

**Acceptance Criteria:**

- [ ] Full workflow tested E2E
- [ ] Unity build succeeds
- [ ] Restore works correctly
- [ ] Cross-platform tested
- [ ] Smoke tests pass
- [ ] < 5 minutes per E2E test

**Estimated Time:** 12 hours

---

#### Task 6.4: Performance & Stress Tests

**Execution Metadata:**

```yaml
wave: 9
parallel_group: null
can_run_parallel: false
blocks: [7.1]
blocked_by: [6.3]
agent_type: executor
estimated_duration: 8h
complexity: medium
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 5  
**Priority:** Low  
**Dependencies:** Task 6.3

**Subtasks:**

- [ ] Benchmark preparation with 100 packages
- [ ] Benchmark TUI with 1000 items
- [ ] Benchmark large file patching
- [ ] Profile memory usage
- [ ] Identify bottlenecks
- [ ] Optimize hot paths
- [ ] Document performance baselines

**Acceptance Criteria:**

- [ ] Preparation < 30s (p95)
- [ ] TUI startup < 2s
- [ ] TUI responsive < 100ms
- [ ] Memory usage reasonable (< 500MB)
- [ ] Bottlenecks documented
- [ ] Optimization opportunities identified

**Estimated Time:** 8 hours

---

## Epic 7: Documentation & Deployment

### Execution Wave 10 (Parallel - Documentation)

#### Task 7.1: User Documentation

**Execution Metadata:**

```yaml
wave: 10
parallel_group: "documentation"
can_run_parallel: true
parallel_with: [7.2]
blocks: [7.3]
blocked_by: [6.4]
agent_type: executor
estimated_duration: 12h
complexity: low
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 6.4

**Subtasks:**

- [ ] Write installation guide
- [ ] Write quick start guide
- [ ] Write CLI command reference
- [ ] Write TUI user guide
- [ ] Write configuration format reference
- [ ] Write troubleshooting guide
- [ ] Add screenshots and examples
- [ ] Create video walkthrough

**Acceptance Criteria:**

- [ ] All user docs complete
- [ ] Examples work
- [ ] Screenshots current
- [ ] Clear and concise
- [ ] Searchable
- [ ] Version-tagged

**Estimated Time:** 12 hours

---

#### Task 7.2: Developer Documentation

**Execution Metadata:**

```yaml
wave: 10
parallel_group: "documentation"
can_run_parallel: true
parallel_with: [7.1]
blocks: [7.3]
blocked_by: [6.4]
agent_type: executor
estimated_duration: 10h
complexity: low
ai_agent_ready: true
requires_human_review: false
auto_merge_eligible: true
```

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** Task 6.4

**Subtasks:**

- [ ] Write architecture overview
- [ ] Document adding new patcher types
- [ ] Document adding new CLI commands
- [ ] Document adding new TUI views
- [ ] Write testing guide
- [ ] Write contributing guide
- [ ] Generate API documentation
- [ ] Add architecture diagrams

**Acceptance Criteria:**

- [ ] All dev docs complete
- [ ] API docs generated
- [ ] Diagrams clear
- [ ] Examples provided
- [ ] Contributing guide comprehensive

**Estimated Time:** 10 hours

---

#### Task 7.3: Deployment & CI/CD

**Execution Metadata:**

```yaml
wave: 11
parallel_group: null
can_run_parallel: false
blocks: []
blocked_by: [7.1, 7.2]
agent_type: executor
estimated_duration: 10h
complexity: medium
ai_agent_ready: true
requires_human_review: true
auto_merge_eligible: false
```

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 7.1, 7.2

**Subtasks:**

- [ ] Create GitHub Actions workflow
- [ ] Setup automated builds (Windows/Linux/macOS)
- [ ] Setup automated tests
- [ ] Setup code coverage reporting
- [ ] Create release workflow
- [ ] Integrate with Nuke build system
- [ ] Create installation scripts
- [ ] Document deployment process

**Acceptance Criteria:**

- [ ] CI/CD pipeline works
- [ ] Builds on all platforms
- [ ] Tests run automatically
- [ ] Coverage reported
- [ ] Releases automated
- [ ] Nuke integration complete
- [ ] Installation easy

**Estimated Time:** 10 hours

---

## Execution Waves Summary

### Wave 1: Foundation (Serial) ✅ COMPLETE

**Duration:** ~10 hours  
**Tasks:** 1.1, 1.2  
**Parallelization:** None (must be serial)  
**Status:** Complete

### Wave 2: Core Components (Parallel) ✅ COMPLETE

**Duration:** ~8 hours (max of parallel tasks)  
**Tasks:** 1.3, 1.4  
**Parallelization:** 2 tasks in parallel  
**Status:** Complete

### Wave 3: Services (Parallel) ✅ COMPLETE

**Duration:** ~10 hours (max of parallel tasks)  
**Tasks:** 2.1, 2.4  
**Parallelization:** 2 tasks in parallel  
**Status:** Complete

### Wave 4: Dependent Services (Parallel) ✅ COMPLETE

**Duration:** ~12 hours (max of parallel tasks)  
**Tasks:** 2.2, 2.3  
**Parallelization:** 2 tasks in parallel  
**Status:** Complete

### Wave 5: Preparation Service (Serial) ⏳ NEXT

**Duration:** ~14 hours  
**Tasks:** 2.5  
**Parallelization:** None (depends on all services)  
**Status:** Ready to start

### Wave 6: Patchers (Parallel)

**Duration:** ~20 hours (max of parallel tasks)  
**Tasks:** 3.1, 3.2, 3.3, 3.4, 3.5  
**Parallelization:** 4 tasks in parallel (after 3.1)  
**Details:**

- 3.1 must complete first (4h)
- Then 3.2, 3.3, 3.5 in parallel (16h max)
- 3.4 can run alone or with others (20h)

### Wave 7: CLI & TUI Foundation (Parallel)

**Duration:** ~12 hours (max of parallel tasks)  
**Tasks:** 4.1, 4.2, 4.3, 5.1  
**Parallelization:** CLI commands in parallel, TUI foundation serial  
**Details:**

- 4.1, 4.2, 4.3 can run in parallel (12h max)
- 5.1 must run before other TUI views (12h)

### Wave 8: TUI Views (Parallel)

**Duration:** ~14 hours (max of parallel tasks)  
**Tasks:** 5.2, 5.3, 5.4, 5.5  
**Parallelization:** 3 views in parallel, then 5.5  
**Details:**

- 5.2, 5.3, 5.4 in parallel (14h max)
- 5.5 depends on 5.1 (10h)

### Wave 9: Testing (Serial)

**Duration:** ~50 hours  
**Tasks:** 6.1, 6.2, 6.3, 6.4  
**Parallelization:** None (tests must be serial)  
**Details:**

- 6.1 Unit tests (16h)
- 6.2 Integration tests (14h)
- 6.3 E2E tests (12h)
- 6.4 Performance tests (8h)

### Wave 10: Documentation (Parallel)

**Duration:** ~12 hours (max of parallel tasks)  
**Tasks:** 7.1, 7.2  
**Parallelization:** 2 tasks in parallel  
**Details:**

- 7.1, 7.2 can run in parallel (12h max)

### Wave 11: Deployment (Serial)

**Duration:** ~10 hours  
**Tasks:** 7.3  
**Parallelization:** None  
**Status:** Final task

## Total Timeline

**Serial Execution:** ~242 hours (6 weeks, 1 developer)  
**Parallel Execution:** ~128 hours (3.2 weeks, multiple agents)  
**Speedup:** 1.9x with parallelization

**Current Progress:** 4/11 waves complete (36%)  
**Remaining:** 7 waves, ~100 hours (parallel) or ~192 hours (serial)

## Agent Assignment Strategy

### Auto-Merge Eligible Tasks

Tasks that can be auto-merged after approval:

- Low complexity
- No human review required
- High test coverage
- Clear acceptance criteria

**Examples:** 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.4, 3.1, 3.3, 3.5, 4.1, 4.2, 5.2, 5.4, 6.1, 7.1, 7.2

**Count:** 17/34 tasks (50%)

### Human Review Required Tasks

Tasks that need human review before merge:

- High complexity
- Critical functionality
- Complex algorithms
- Security implications
- Integration points

**Examples:** 2.3 (Validation), 2.5 (Preparation), 3.2 (CSharp Patcher), 3.4 (Unity YAML), 4.3 (Prepare Commands), 5.1 (TUI Host), 5.3 (Config Editor), 5.5 (Execution View), 6.2-6.4 (Integration/E2E/Performance), 7.3 (Deployment)

**Count:** 17/34 tasks (50%)

## Task Priority Distribution

**Critical Priority:** 8 tasks  

- 1.1, 1.2, 1.3, 2.5, 3.2, 4.3, Project completion blockers

**High Priority:** 18 tasks  

- Most services, patchers, CLI commands, TUI views, testing

**Medium Priority:** 6 tasks  

- Cache commands, some TUI views, performance testing

**Low Priority:** 2 tasks  

- Performance optimization, stress tests

## Complexity Distribution

**Low Complexity:** 8 tasks (24%)  
**Medium Complexity:** 14 tasks (41%)  
**High Complexity:** 12 tasks (35%)

## Recommended Execution Order for AI Agents

### Immediate Start (Wave 5)

1. **Task 2.5** - PreparationService (critical path blocker)

### After Wave 5 (Wave 6)

Parallel execution:

1. **Task 3.1** - Patcher Interface (blocks others)
2. Then parallel: **3.2, 3.3, 3.5** (CSharp, Json, Text patchers)
3. **Task 3.4** - Unity YAML patcher (can run with others or alone)

### After Wave 6 (Wave 7)

Parallel execution:

1. **CLI Group:** 4.1, 4.2, 4.3 in parallel
2. **TUI Foundation:** 5.1 (blocks TUI views)

### After Wave 7 (Wave 8)

Parallel execution:

1. **TUI Views:** 5.2, 5.3, 5.4 in parallel
2. **Execution View:** 5.5 after 5.1

### After Wave 8 (Wave 9)

Serial execution:

1. **Task 6.1** - Unit tests
2. **Task 6.2** - Integration tests
3. **Task 6.3** - E2E tests
4. **Task 6.4** - Performance tests

### After Wave 9 (Wave 10-11)

Final documentation and deployment:

1. **Parallel:** 7.1, 7.2 (documentation)
2. **Serial:** 7.3 (deployment)

## Dependency Graph

```
Epic 1: Core Infrastructure (COMPLETE ✅)
1.1 (Project Setup) ──> 1.2 (DI Setup)
                         ├──> 1.3 (PathResolver)
                         └──> 1.4 (Core Models)

Epic 2: Services (COMPLETE ✅)
1.3 + 1.4 ──> 2.1 (ConfigService) ──┐
           ├─> 2.4 (ManifestService) ├──> 2.2 (CacheService) ──┐
           └─> 2.3 (ValidationService)                         ├──> 2.5 (PreparationService)
                                                                └──────────────────────────┘

Epic 3: Patchers (NEXT ⏳)
1.4 ──> 3.1 (IPatcher Interface)
         ├──> 3.2 (CSharpPatcher) ──┐
         ├──> 3.3 (JsonPatcher) ────├──> 2.5 (blocks PreparationService)
         ├──> 3.4 (UnityPatcher) ───┤
         └──> 3.5 (TextPatcher) ────┘

Epic 4: CLI (depends on Epic 2)
2.5 ──> 4.1 (Config Commands) ──┐
     ├─> 4.2 (Cache Commands) ──├──> 6.1 (Unit Tests)
     └─> 4.3 (Prepare Commands) ─┘

Epic 5: TUI (depends on Epic 2)
2.5 ──> 5.1 (TUI Host) ──┐
                          ├──> 5.2 (Cache View) ──┐
                          ├──> 5.3 (Config View) ─├──> 6.2 (Integration Tests)
                          ├──> 5.4 (Validation) ──┤
                          └──> 5.5 (Execution) ───┘

Epic 6: Testing (depends on Epic 4 & 5)
4.1+4.2+4.3 ──> 6.1 (Unit Tests)
6.1 + 5.x ──> 6.2 (Integration Tests)
6.2 ──> 6.3 (E2E Tests)
6.3 ──> 6.4 (Performance Tests)

Epic 7: Documentation & Deployment (depends on Epic 6)
6.4 ──> 7.1 (User Docs) ──┐
     ├─> 7.2 (Dev Docs) ──├──> 7.3 (Deployment)
                          ─┘
```

## Critical Path Analysis

The **critical path** determines the minimum project duration:

```
1.1 (4h) → 1.2 (6h) → 1.3 (8h) → 2.1 (10h) → 2.5 (14h) → 3.4 (20h) →
4.3 (12h) → 5.1 (12h) → 5.3 (14h) → 6.1 (16h) → 6.2 (14h) → 6.3 (12h) →
6.4 (8h) → 7.1 (12h) → 7.3 (10h)

Total Critical Path: 172 hours (4.3 weeks with perfect execution)
```

**Optimization Opportunities:**

- Wave 6-8 offer most parallelization potential
- Testing (Wave 9) is serial bottleneck - cannot be parallelized
- Early completion of 2.5 and 3.x unblocks most remaining work

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
