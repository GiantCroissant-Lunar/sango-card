---
id: TASK-BLD-PREP-002
title: "Implement Two-Config Architecture & Manual Source Control"
status: pending
priority: high
created: 2025-10-17
updated: 2025-10-17
assignee: unassigned
spec: build-preparation-tool-amendment-002.md
estimated_hours: 40
tags:
  - build
  - preparation
  - enhancement
  - architecture
---

# Task: Implement Two-Config Architecture & Manual Source Control

## Overview

Implement the two-config architecture that separates preparation concerns from build concerns, plus add manual source control for scattered dependencies.

## Related Spec

**Spec:** `.specify/specs/build-preparation-tool-amendment-002.md`

## Objectives

1. ✅ Separate preparation manifest from build injection config
2. ✅ Support manual source addition from any location
3. ✅ Remove path restrictions on targets
4. ✅ Add CLI commands for manual control
5. ✅ Update TUI for two-config workflow
6. ✅ Maintain backward compatibility

## Task Organization

### Wave 1: Foundation (Can be done in parallel by multiple agents)

#### Phase 1.1: Schema Definition (Serial) - Agent A
**Estimated:** 8 hours  
**Dependencies:** None

- [ ] Define `PreparationManifest` schema
  - [ ] Add `id`, `title`, `description` fields
  - [ ] Add `cacheDirectory` field
  - [ ] Add `items` collection
  - [ ] Add validation attributes
- [ ] Update `BuildPreparationConfig` schema
  - [ ] Add `id`, `title` fields
  - [ ] Ensure all paths support absolute/relative
  - [ ] Add validation attributes
- [ ] Create schema validators
- [ ] Write unit tests for schemas

**Deliverables:**
- `PreparationManifest.cs`
- Updated `BuildPreparationConfig.cs`
- Schema validators
- Unit tests (>90% coverage)

**Acceptance:**
- [ ] Schemas compile without errors
- [ ] All validation tests pass
- [ ] JSON serialization/deserialization works

#### Phase 1.2: Core Logic Updates (Parallel) - Agent B
**Estimated:** 8 hours  
**Dependencies:** Phase 1.1 (schemas)

- [ ] Update cache populate logic
  - [ ] Read preparation manifest
  - [ ] Support configurable cache directory
  - [ ] Copy items to cache
  - [ ] Handle errors gracefully
- [ ] Update prepare inject logic
  - [ ] Read build injection config
  - [ ] Support absolute/relative paths
  - [ ] Maintain existing functionality
- [ ] Add path resolution utilities
- [ ] Write integration tests

**Deliverables:**
- Updated `CacheService.cs`
- Updated `PreparationService.cs`
- Path utilities
- Integration tests

**Acceptance:**
- [ ] Can populate cache from new manifest format
- [ ] Can inject from new config format
- [ ] All existing tests still pass

#### Phase 1.3: Migration Tool (Parallel) - Agent C
**Estimated:** 6 hours  
**Dependencies:** Phase 1.1 (schemas)

- [ ] Implement config migration command
  - [ ] Parse old config format
  - [ ] Generate preparation manifest
  - [ ] Generate build injection config
  - [ ] Preserve all functionality
- [ ] Add backward compatibility layer
  - [ ] Auto-detect old vs new format
  - [ ] Show deprecation warnings
- [ ] Write migration tests

**Deliverables:**
- `MigrationCommand.cs`
- Backward compatibility layer
- Migration tests

**Acceptance:**
- [ ] Can migrate existing configs
- [ ] Old configs still work (with warning)
- [ ] Migration is lossless

---

### Wave 2: CLI Commands (Can be done in parallel)

#### Phase 2.1: add-source Command (Parallel) - Agent A
**Estimated:** 4 hours  
**Dependencies:** Wave 1 complete

- [ ] Implement `config add-source` command
  - [ ] Parameter parsing
  - [ ] Source validation
  - [ ] Manifest update logic
  - [ ] Dry-run support
- [ ] Add help text
- [ ] Write command tests

**Deliverables:**
- `AddSourceCommand.cs`
- Help documentation
- Command tests

**Acceptance:**
- [ ] Can add sources to manifest
- [ ] Validates source exists
- [ ] Dry-run shows preview

#### Phase 2.2: add-injection Command (Parallel) - Agent B
**Estimated:** 4 hours  
**Dependencies:** Wave 1 complete

- [ ] Implement `config add-injection` command
  - [ ] Parameter parsing
  - [ ] Cache path validation
  - [ ] Config update logic
  - [ ] Dry-run support
- [ ] Add help text
- [ ] Write command tests

**Deliverables:**
- `AddInjectionCommand.cs`
- Help documentation
- Command tests

**Acceptance:**
- [ ] Can add injections to config
- [ ] Validates cache path exists
- [ ] Dry-run shows preview

#### Phase 2.3: add-batch Command (Parallel) - Agent C
**Estimated:** 6 hours  
**Dependencies:** Wave 1 complete

- [ ] Implement `config add-batch` command
  - [ ] YAML/JSON parsing
  - [ ] Batch processing
  - [ ] Error handling (continue-on-error)
  - [ ] Summary reporting
- [ ] Add help text
- [ ] Write command tests

**Deliverables:**
- `AddBatchCommand.cs`
- YAML parser integration
- Help documentation
- Command tests

**Acceptance:**
- [ ] Can process batch manifests
- [ ] Handles errors gracefully
- [ ] Reports summary correctly

---

### Wave 3: TUI & Polish (Sequential, requires Wave 2)

#### Phase 3.1: TUI Core Updates (Serial) - Agent A
**Estimated:** 4 hours  
**Dependencies:** Wave 2 complete

- [ ] Add "Configuration Type" selection
- [ ] Update main menu
- [ ] Add config listing screens
- [ ] Update navigation flow

**Deliverables:**
- Updated TUI screens
- Navigation logic

**Acceptance:**
- [ ] TUI compiles and runs
- [ ] Navigation is intuitive

#### Phase 3.2: TUI Management Screens (Parallel after 3.1)

##### Phase 3.2a: Preparation Sources Screen - Agent B
**Estimated:** 3 hours  
**Dependencies:** Phase 3.1

- [ ] Add source management screen
- [ ] File browser integration
- [ ] Add/view/remove operations

**Deliverables:**
- Preparation sources screen

##### Phase 3.2b: Build Injections Screen - Agent C
**Estimated:** 3 hours  
**Dependencies:** Phase 3.1

- [ ] Add injection management screen
- [ ] Cache browser integration
- [ ] Add/view/remove operations

**Deliverables:**
- Build injections screen

#### Phase 3.3: Integration & Testing (Serial) - Agent A
**Estimated:** 4 hours  
**Dependencies:** Phase 3.2a, 3.2b

- [ ] Integration testing
- [ ] User acceptance testing
- [ ] Bug fixes
- [ ] Polish

**Deliverables:**
- Tested TUI
- Bug fixes

---

### Wave 4: Documentation (Can start after Wave 2)

#### Phase 4.1: Documentation (Parallel) - Agent D
**Estimated:** 4 hours  
**Dependencies:** Wave 2 complete

- [ ] Update tool README
- [ ] Create migration guide
- [ ] Add usage examples
- [ ] Update workflow docs

**Deliverables:**
- Updated documentation
- Migration guide
- Usage examples

**Acceptance:**
- [ ] Documentation is clear and complete
- [ ] Examples are tested and work

---

## Parallel Execution Plan

### Week 1: Foundation
```
Day 1-2: Wave 1 (Parallel)
  Agent A: Schema Definition (Phase 1.1)
  Agent B: Core Logic Updates (Phase 1.2) - starts after schemas
  Agent C: Migration Tool (Phase 1.3) - starts after schemas

Day 3: Integration & Testing
  All agents: Integrate and test Wave 1
```

### Week 2: CLI Commands
```
Day 1-2: Wave 2 (Parallel)
  Agent A: add-source Command (Phase 2.1)
  Agent B: add-injection Command (Phase 2.2)
  Agent C: add-batch Command (Phase 2.3)
  Agent D: Documentation (Phase 4.1) - can start

Day 3: Integration & Testing
  All agents: Integrate and test Wave 2
```

### Week 3: TUI & Polish
```
Day 1: Wave 3 Phase 3.1 (Serial)
  Agent A: TUI Core Updates

Day 2: Wave 3 Phase 3.2 (Parallel)
  Agent B: Preparation Sources Screen
  Agent C: Build Injections Screen

Day 3-4: Wave 3 Phase 3.3 (Serial)
  Agent A: Integration & Testing

Day 5: Final Polish
  All agents: Final review and polish
```

## Agent Assignment

### Agent A (Lead)
- Schema Definition (Phase 1.1)
- TUI Core Updates (Phase 3.1)
- Integration & Testing (Phase 3.3)
- Overall coordination

### Agent B
- Core Logic Updates (Phase 1.2)
- add-injection Command (Phase 2.2)
- Preparation Sources Screen (Phase 3.2a)

### Agent C
- Migration Tool (Phase 1.3)
- add-batch Command (Phase 2.3)
- Build Injections Screen (Phase 3.2b)

### Agent D
- Documentation (Phase 4.1)
- Testing support
- User acceptance testing

## Technical Details

### New File Structure

```
build/preparation/
├── sources/              # NEW: Preparation manifests
│   ├── default.json
│   ├── external-deps.json
│   └── custom-packages.json
├── configs/              # EXISTING: Build injection configs
│   ├── default.json      # Updated schema
│   ├── production.json
│   └── development.json
└── cache/                # EXISTING: Cached items
    ├── com.example.package/
    ├── Polly.8.6.2/
    └── ...
```

### Config Schemas

#### Preparation Manifest

```csharp
public class PreparationManifest
{
    [Required]
    public string Version { get; set; } = "1.0";

    [Required]
    [RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$")]  // kebab-case
    public string Id { get; set; } = "";

    [Required]
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string CacheDirectory { get; set; } = "build/preparation/cache";

    public List<SourceItem> Items { get; set; } = new();
}

public class SourceItem
{
    [Required]
    public string Source { get; set; } = "";      // Any path (absolute or relative)

    [Required]
    public string CacheAs { get; set; } = "";     // Name in cache

    [Required]
    public SourceType Type { get; set; }          // package, assembly, asset
}

public enum SourceType
{
    Package,
    Assembly,
    Asset
}
```

#### Build Injection Config (Updated)

```csharp
public class BuildPreparationConfig
{
    [Required]
    public string Version { get; set; } = "1.0";

    [Required]
    [RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$")]  // kebab-case
    public string Id { get; set; } = "";

    [Required]
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    // Existing fields
    public List<PackageEntry> Packages { get; set; } = new();
    public List<AssemblyEntry> Assemblies { get; set; } = new();
    public List<AssetManipulation> AssetManipulations { get; set; } = new();
    public List<CodePatch> CodePatches { get; set; } = new();
    public ScriptingDefineSymbols ScriptingDefineSymbols { get; set; } = new();
}

// Note: PackageEntry and AssemblyEntry now allow any target path
public class PackageEntry
{
    public string Name { get; set; } = "";
    public string? Version { get; set; }
    public string Source { get; set; } = "";      // Any path (cache or absolute)
    public string Target { get; set; } = "";      // Any path in client (no restrictions)
}
```

### Command Implementations

#### `config add-source`

```csharp
public class AddSourceCommand : ICommand
{
    [Option("--source", Required = true)]
    public string Source { get; set; } = "";

    [Option("--cache-as", Required = true)]
    public string CacheAs { get; set; } = "";

    [Option("--type", Required = true)]
    public SourceType Type { get; set; }

    [Option("--manifest", Required = true)]
    public string Manifest { get; set; } = "";

    [Option("--dry-run")]
    public bool DryRun { get; set; }

    public async Task<int> ExecuteAsync()
    {
        // 1. Validate source exists
        // 2. Load manifest
        // 3. Copy source to cache (if not dry-run)
        // 4. Add to manifest
        // 5. Save manifest (if not dry-run)
        // 6. Report success
    }
}
```

### Migration Strategy

**For existing users:**

1. **Automatic migration tool:**
   ```bash
   dotnet run -- config migrate \
     --old-config build/preparation/configs/default.json \
     --source-manifest build/preparation/sources/default.json \
     --injection-config build/preparation/configs/default-new.json
   ```

2. **Migration logic:**
   - Parse old config
   - Extract source locations (if any)
   - Create preparation manifest with cache sources
   - Create new injection config with all build operations
   - Preserve all existing functionality

3. **Backward compatibility:**
   - Old config format still works (deprecated warning)
   - Tool auto-detects config type
   - Gradual migration path

### Testing Strategy

#### Unit Tests

- Config schema validation
- Command parameter parsing
- Path normalization
- File copy operations
- Manifest updates

#### Integration Tests

- End-to-end add-source flow
- End-to-end add-injection flow
- Batch operations
- Migration from old config
- Mixed config types

#### Manual Testing

- Add sources from various locations (C:/, D:/, UNC, relative)
- Add injections with custom targets
- Batch manifest processing
- TUI workflows
- Migration scenarios

## Dependencies

### Tools/Libraries

- System.CommandLine (existing)
- YamlDotNet (for YAML manifest support)
- Existing validation infrastructure

### External

- None

## Risks & Mitigation

### Risk 1: Breaking Changes

**Mitigation:**
- Maintain backward compatibility
- Provide migration tool
- Clear deprecation warnings
- Comprehensive documentation

### Risk 2: Complex Migration

**Mitigation:**
- Automated migration tool
- Step-by-step guide
- Support for gradual migration
- Rollback instructions

### Risk 3: User Confusion

**Mitigation:**
- Clear naming (sources vs configs)
- Separate directories
- Updated TUI with clear labels
- Video tutorial

## Implementation Order

1. **Week 1: Core Architecture**
   - Day 1-2: Define schemas
   - Day 3-4: Update cache/inject logic
   - Day 5: Migration tool

2. **Week 2: CLI Commands**
   - Day 1-2: add-source command
   - Day 3: add-injection command
   - Day 4: add-batch command
   - Day 5: Testing & documentation

3. **Week 3: TUI & Polish**
   - Day 1-2: TUI updates
   - Day 3: Integration testing
   - Day 4: Documentation
   - Day 5: User testing & fixes

## Success Metrics

- [ ] All unit tests pass (>80% coverage)
- [ ] All integration tests pass
- [ ] Migration tool successfully migrates existing configs
- [ ] TUI workflows are intuitive (user testing)
- [ ] Documentation is complete and clear
- [ ] No regressions in existing functionality

## Notes

### Design Decisions

1. **Two configs instead of one:** Clean separation of concerns, allows reusing cache with different build configs
2. **No path restrictions:** Maximum flexibility for users
3. **Backward compatibility:** Smooth migration path
4. **CLI-first approach:** TUI follows CLI design

### Future Enhancements

- Auto-generate injection config from preparation manifest
- Dependency resolution
- Version conflict detection
- Source update tracking

## Checklist

- [ ] Spec reviewed and approved
- [ ] Technical design reviewed
- [ ] Implementation complete
- [ ] Unit tests written and passing
- [ ] Integration tests written and passing
- [ ] Documentation updated
- [ ] Migration guide created
- [ ] User testing completed
- [ ] Code reviewed
- [ ] Merged to main branch

## Time Tracking

- **Estimated:** 40 hours
- **Actual:** _TBD_
- **Variance:** _TBD_

## Related Tasks

- None (first task for this spec)

## References

- **Spec:** `build-preparation-tool-amendment-002.md`
- **Parent Spec:** `build-preparation-tool.md`
- **Amendment 001:** `build-preparation-tool-amendment-001.md`
