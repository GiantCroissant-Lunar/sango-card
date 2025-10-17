---
doc_id: DOC-2025-00148
title: Build Preparation Tool Tasks
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [build-preparation-tool-tasks]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00099
title: Build Preparation Tool Tasks
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [build-preparation-tool-tasks]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00097
title: Build Preparation Tool Tasks
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [build-preparation-tool-tasks]
summary: >
  (Add summary here)
source:
  author: system
---
# TASKS: Build Preparation Tool Implementation

**Spec:** SPEC-BPT-001  
**RFC:** RFC-001  
**Sprint:** 6 weeks  
**Team Size:** 1-2 developers

## Epic 1: Core Infrastructure

### Task 1.1: Project Setup

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

- Project builds successfully
- All NuGet packages restored
- Test project runs

**Estimated Time:** 4 hours

---

### Task 1.2: Dependency Injection Setup

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

- Host starts successfully
- Services resolve from DI
- Logging works
- MessagePipe configured

**Estimated Time:** 6 hours

---

### Task 1.3: Path Resolver Implementation

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

- Git root detected correctly
- Paths resolved relative to git root
- Clear error if git root not found
- Cross-platform tests pass
- 90% code coverage

**Estimated Time:** 8 hours

**Test Cases:**

```csharp
[Fact] void DetectsGitRoot_FromNestedPath()
[Fact] void DetectsGitRoot_FailsOutsideRepo()
[Fact] void ResolvesPath_RelativeToGitRoot()
[Fact] void MakesRelative_FromAbsolutePath()
[Fact] void HandlesWindowsPaths()
[Fact] void HandlesUnixPaths()
```

---

### Task 1.4: Core Models

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

- All models defined
- JSON serialization works
- Models immutable where appropriate
- XML documentation complete

**Estimated Time:** 6 hours

---

## Epic 2: Services Implementation

### Task 2.1: ConfigService

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

- Config loads from JSON
- Config saves to JSON
- Paths resolved correctly
- Messages published on changes
- 85% code coverage

**Estimated Time:** 10 hours

---

### Task 2.2: CacheService

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

- Cache populates from code-quality
- Items added to cache
- Config updated automatically
- Messages published
- 85% code coverage

**Estimated Time:** 10 hours

---

### Task 2.3: ValidationService

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

- All 4 validation levels work
- Clear error messages
- File paths in errors
- Summary with counts
- 90% code coverage

**Estimated Time:** 12 hours

---

### Task 2.4: ManifestService

**Story Points:** 3  
**Priority:** Medium  
**Dependencies:** Task 2.1

**Subtasks:**

- [ ] Implement ReadManifest()
- [ ] Implement AddPackage()
- [ ] Implement RemovePackage()
- [ ] Implement ModifyManifest()
- [ ] Handle JSON formatting
- [ ] Write unit tests

**Acceptance Criteria:**

- Manifest.json parsed correctly
- Packages added/removed
- Formatting preserved
- 85% code coverage

**Estimated Time:** 6 hours

---

### Task 2.5: PreparationService

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 2.1, Task 2.2, Task 2.4

**Subtasks:**

- [ ] Implement Execute()
- [ ] Implement CopyAssets()
- [ ] Implement ModifyManifest()
- [ ] Implement ApplyDefines()
- [ ] Implement ApplyPatches()
- [ ] Add dry-run support
- [ ] Add progress reporting
- [ ] Add rollback on error
- [ ] Write integration tests

**Acceptance Criteria:**

- Full preparation executes
- Dry-run shows changes without applying
- Progress reported via messages
- Rollback on error
- 80% code coverage

**Estimated Time:** 14 hours

---

## Epic 3: Code Patchers

### Task 3.1: IPatcher Interface

**Story Points:** 1  
**Priority:** High  
**Dependencies:** Task 1.4

**Subtasks:**

- [ ] Define IPatcher interface
- [ ] Define PatchResult model
- [ ] Define PatchValidation model
- [ ] Add documentation

**Acceptance Criteria:**

- Interface defined
- Models defined
- Documentation complete

**Estimated Time:** 2 hours

---

### Task 3.2: CSharpPatcher (Roslyn)

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 3.1

**Subtasks:**

- [ ] Implement CanHandle()
- [ ] Implement RemoveUsing()
- [ ] Implement ReplaceExpression()
- [ ] Implement ReplaceBlock()
- [ ] Implement RemoveBlock()
- [ ] Implement ValidatePatch()
- [ ] Preserve formatting and trivia
- [ ] Add rollback support
- [ ] Write unit tests

**Acceptance Criteria:**

- All patch types work
- Syntax preserved
- Formatting preserved
- Validation works
- 90% code coverage

**Estimated Time:** 14 hours

**Test Cases:**

```csharp
[Fact] void RemovesUsing_PreservesOtherUsings()
[Fact] void ReplaceExpression_PreservesFormatting()
[Fact] void ReplaceBlock_HandlesNestedBlocks()
[Fact] void ValidatePatch_DetectsInvalidSyntax()
[Fact] void Rollback_RestoresOriginal()
```

---

### Task 3.3: JsonPatcher

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 3.1

**Subtasks:**

- [ ] Implement CanHandle()
- [ ] Implement SetValue() with JSON path
- [ ] Implement RemoveProperty()
- [ ] Implement AddToArray()
- [ ] Implement ValidatePatch()
- [ ] Preserve formatting
- [ ] Write unit tests

**Acceptance Criteria:**

- JSON path navigation works
- Values set correctly
- Formatting preserved
- 85% code coverage

**Estimated Time:** 8 hours

---

### Task 3.4: UnityAssetPatcher

**Story Points:** 13  
**Priority:** High  
**Dependencies:** Task 3.1

**Subtasks:**

- [ ] Implement UnityYamlParser
- [ ] Parse Unity YAML format
- [ ] Implement CanHandle()
- [ ] Implement ModifyScriptingDefines()
- [ ] Implement SetProperty()
- [ ] Implement ValidatePatch()
- [ ] Handle Unity-specific YAML features
- [ ] Write unit tests

**Acceptance Criteria:**

- Unity YAML parsed correctly
- Scripting defines modified
- Properties set
- Format preserved
- 80% code coverage

**Estimated Time:** 20 hours

**Note:** This is complex due to Unity's custom YAML format

---

### Task 3.5: TextPatcher

**Story Points:** 3  
**Priority:** Medium  
**Dependencies:** Task 3.1

**Subtasks:**

- [ ] Implement CanHandle()
- [ ] Implement Replace() with regex
- [ ] Implement RemoveLine()
- [ ] Implement InsertAfter()
- [ ] Implement ValidatePatch()
- [ ] Write unit tests

**Acceptance Criteria:**

- Regex replacement works
- Line operations work
- 85% code coverage

**Estimated Time:** 6 hours

---

## Epic 4: CLI Mode

### Task 4.1: CLI Host Setup

**Story Points:** 3  
**Priority:** High  
**Dependencies:** Task 1.2

**Subtasks:**

- [ ] Implement CliHost
- [ ] Setup System.CommandLine
- [ ] Create root command
- [ ] Add global options
- [ ] Add help text
- [ ] Configure exit codes

**Acceptance Criteria:**

- CLI runs
- Help text displays
- Exit codes correct

**Estimated Time:** 6 hours

---

### Task 4.2: Config Commands

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 4.1, Task 2.1

**Subtasks:**

- [ ] Implement `config create`
- [ ] Implement `config add-package`
- [ ] Implement `config add-assembly`
- [ ] Implement `config add-define`
- [ ] Implement `config add-patch`
- [ ] Implement `config remove`
- [ ] Implement `config list`
- [ ] Write integration tests

**Acceptance Criteria:**

- All commands work
- Help text clear
- Error messages helpful
- Tests pass

**Estimated Time:** 10 hours

---

### Task 4.3: Cache Commands

**Story Points:** 3  
**Priority:** High  
**Dependencies:** Task 4.1, Task 2.2

**Subtasks:**

- [ ] Implement `cache populate`
- [ ] Implement `cache add`
- [ ] Implement `cache list`
- [ ] Implement `cache clean`
- [ ] Write integration tests

**Acceptance Criteria:**

- All commands work
- Progress displayed
- Tests pass

**Estimated Time:** 6 hours

---

### Task 4.4: Prepare Commands

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 4.1, Task 2.5

**Subtasks:**

- [ ] Implement `prepare run`
- [ ] Implement `prepare validate`
- [ ] Add dry-run support
- [ ] Add progress display
- [ ] Write integration tests

**Acceptance Criteria:**

- Preparation runs
- Validation works
- Dry-run shows changes
- Progress clear
- Tests pass

**Estimated Time:** 10 hours

---

### Task 4.5: Restore Commands

**Story Points:** 2  
**Priority:** Medium  
**Dependencies:** Task 4.1

**Subtasks:**

- [ ] Implement `restore run`
- [ ] Add git restore support
- [ ] Add backup restore support
- [ ] Write integration tests

**Acceptance Criteria:**

- Git restore works
- Backup restore works
- Tests pass

**Estimated Time:** 4 hours

---

## Epic 5: TUI Mode

### Task 5.1: TUI Host Setup

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 1.2

**Subtasks:**

- [ ] Implement TuiHost
- [ ] Setup Terminal.Gui v2
- [ ] Create MainWindow
- [ ] Add menu bar
- [ ] Add status bar
- [ ] Add keyboard shortcuts
- [ ] Setup ReactiveUI bindings

**Acceptance Criteria:**

- TUI launches
- Window displays
- Menus work
- Keyboard navigation works

**Estimated Time:** 10 hours

---

### Task 5.2: AppState & ViewModels

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 5.1

**Subtasks:**

- [ ] Implement AppState with ReactiveUI
- [ ] Implement MainViewModel
- [ ] Implement CacheViewModel
- [ ] Implement ConfigViewModel
- [ ] Implement ValidationViewModel
- [ ] Setup MessagePipe subscriptions
- [ ] Setup reactive commands

**Acceptance Criteria:**

- State updates reactively
- ViewModels bind to views
- Commands execute
- Messages handled

**Estimated Time:** 10 hours

---

### Task 5.3: CacheManagementView

**Story Points:** 5  
**Priority:** High  
**Dependencies:** Task 5.2

**Subtasks:**

- [ ] Create CacheManagementView
- [ ] Add package list
- [ ] Add assembly list
- [ ] Add buttons (Add, Remove, Populate)
- [ ] Bind to CacheViewModel
- [ ] Handle user input

**Acceptance Criteria:**

- View displays cache items
- Lists update reactively
- Buttons work
- User can manage cache

**Estimated Time:** 10 hours

---

### Task 5.4: ConfigEditorView

**Story Points:** 8  
**Priority:** High  
**Dependencies:** Task 5.2

**Subtasks:**

- [ ] Create ConfigEditorView
- [ ] Add preparation list
- [ ] Add define editor
- [ ] Add patch editor
- [ ] Add asset manipulation editor
- [ ] Bind to ConfigViewModel
- [ ] Handle user input
- [ ] Add validation feedback

**Acceptance Criteria:**

- View displays config
- User can edit all fields
- Changes save
- Validation shows errors

**Estimated Time:** 14 hours

---

### Task 5.5: ValidationView

**Story Points:** 3  
**Priority:** Medium  
**Dependencies:** Task 5.2

**Subtasks:**

- [ ] Create ValidationView
- [ ] Display validation results
- [ ] Show error details
- [ ] Add navigation to errors
- [ ] Bind to ValidationViewModel

**Acceptance Criteria:**

- Validation results display
- Errors clear
- User can navigate to errors

**Estimated Time:** 6 hours

---

## Epic 6: Integration & Testing

### Task 6.1: Nuke Integration

**Story Points:** 3  
**Priority:** High  
**Dependencies:** Task 4.4

**Subtasks:**

- [ ] Create Build.Preparation.cs
- [ ] Implement PrepareClient target
- [ ] Implement RestoreClient target
- [ ] Call tool as external process
- [ ] Handle exit codes
- [ ] Add logging

**Acceptance Criteria:**

- Nuke calls tool successfully
- Exit codes handled
- Logs captured

**Estimated Time:** 6 hours

---

### Task 6.2: End-to-End Tests

**Story Points:** 8  
**Priority:** High  
**Dependencies:** All previous tasks

**Subtasks:**

- [ ] Setup test Unity project
- [ ] Test full preparation flow
- [ ] Test build integration
- [ ] Test restore flow
- [ ] Test error scenarios
- [ ] Test rollback

**Acceptance Criteria:**

- Full flow works
- Errors handled gracefully
- Rollback works
- All tests pass

**Estimated Time:** 14 hours

---

### Task 6.3: Performance Testing

**Story Points:** 3  
**Priority:** Medium  
**Dependencies:** Task 6.2

**Subtasks:**

- [ ] Measure preparation time
- [ ] Measure TUI responsiveness
- [ ] Measure large config handling
- [ ] Optimize bottlenecks
- [ ] Add performance benchmarks

**Acceptance Criteria:**

- Preparation < 30 seconds
- TUI updates < 100ms
- Large configs handled

**Estimated Time:** 6 hours

---

### Task 6.4: Documentation

**Story Points:** 5  
**Priority:** High  
**Dependencies:** All previous tasks

**Subtasks:**

- [ ] Write installation guide
- [ ] Write quick start guide
- [ ] Write CLI reference
- [ ] Write TUI guide
- [ ] Write config format reference
- [ ] Write troubleshooting guide
- [ ] Write architecture docs
- [ ] Add code examples

**Acceptance Criteria:**

- All docs complete
- Examples work
- Clear and concise

**Estimated Time:** 10 hours

---

## Summary

**Total Story Points:** 130  
**Total Estimated Hours:** 242 hours  
**Estimated Duration:** 6 weeks (1 developer) or 3 weeks (2 developers)

### Sprint Breakdown

**Sprint 1 (Week 1):** Epic 1 - Core Infrastructure  
**Sprint 2 (Week 2):** Epic 2 - Services Implementation  
**Sprint 3 (Week 3):** Epic 3 - Code Patchers  
**Sprint 4 (Week 4):** Epic 4 - CLI Mode  
**Sprint 5 (Week 5):** Epic 5 - TUI Mode  
**Sprint 6 (Week 6):** Epic 6 - Integration & Testing

### Critical Path

1. Task 1.1 → 1.2 → 1.3 (Core Infrastructure)
2. Task 2.1 → 2.2 → 2.5 (Services)
3. Task 3.2 → 3.4 (Critical Patchers)
4. Task 4.4 (Prepare Commands)
5. Task 6.1 → 6.2 (Integration)

### Risk Items

- Task 3.4 (UnityAssetPatcher) - High complexity
- Task 5.4 (ConfigEditorView) - Complex UI
- Task 6.2 (E2E Tests) - Dependent on all features
