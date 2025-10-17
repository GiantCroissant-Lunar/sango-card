# TASKS AMENDMENT: Two-Phase Workflow Implementation

**Amendment ID:** TASKS-BPT-001-AMD-001  
**Parent Tasks:** `.specify/tasks/build-preparation-tool-tasks.md`  
**Parent Spec Amendment:** SPEC-BPT-001-AMD-001  
**Status:** Proposed  
**Created:** 2025-10-17

## Summary

Update Task 4.4 (Prepare Commands) and Task 6.1 (Nuke Integration) to implement two-phase workflow.

## Modified Tasks

### Task 4.4: Prepare Commands (MODIFIED)

**Story Points:** 6 (increased from 5)  
**Priority:** High  
**Dependencies:** Task 4.1, Task 2.5

**Subtasks:**

- [x] ~~Implement `prepare run`~~ (DEPRECATED)
- [ ] **NEW:** Rename `prepare run` to `prepare inject`
- [ ] **NEW:** Add `--target` parameter validation
- [ ] **NEW:** Add target path security checks
- [ ] Implement `prepare validate`
- [ ] Add dry-run support for both phases
- [ ] Add progress display
- [ ] Update help text and documentation
- [ ] Write integration tests for two-phase workflow

**Acceptance Criteria:**

- `prepare inject` requires `--target` parameter
- Target path MUST be `projects/client/` (validated)
- Clear error if target path invalid
- Injection validates cache exists
- Dry-run shows what would be injected
- Progress clear for both phases
- Tests pass for two-phase workflow

**Estimated Time:** 12 hours (increased from 10)

**Implementation Details:**

```csharp
// PrepareCommandHandler.cs changes

// OLD (deprecated but kept for backward compat)
public async Task RunAsync(string configRelativePath, ...)
{
    Console.WriteLine("WARNING: 'prepare run' is deprecated. Use 'prepare inject --target projects/client/' instead.");
    await InjectAsync(configRelativePath, "projects/client/", ...);
}

// NEW
public async Task InjectAsync(
    string configRelativePath,
    string targetPath,
    string validationLevel = "full",
    bool verbose = false,
    bool force = false)
{
    // Validate target path
    if (string.IsNullOrWhiteSpace(targetPath))
    {
        Console.Error.WriteLine("Error: --target is required");
        Environment.ExitCode = 1;
        return;
    }

    // Security: Only allow projects/client/ per R-BLD-060
    var normalizedTarget = targetPath.Replace('\\', '/').TrimEnd('/');
    if (normalizedTarget != "projects/client")
    {
        Console.Error.WriteLine($"Error: Target must be 'projects/client/' per R-BLD-060");
        Console.Error.WriteLine($"       Got: {targetPath}");
        Environment.ExitCode = 1;
        return;
    }

    // Validate cache exists
    var config = await _configService.LoadAsync(configRelativePath);

    foreach (var pkg in config.Packages)
    {
        if (!_paths.FileExists(pkg.Source))
        {
            Console.Error.WriteLine($"Error: Cache file not found: {pkg.Source}");
            Console.Error.WriteLine("       Run 'cache populate' first.");
            Environment.ExitCode = 1;
            return;
        }
    }

    // Rest of implementation...
    await _preparationService.InjectAsync(config, targetPath, configRelativePath, dryRun: false, validate: true);
}
```

**Test Cases:**

```csharp
[Fact] void InjectAsync_RequiresTargetParameter()
[Fact] void InjectAsync_ValidatesTargetIsClient()
[Fact] void InjectAsync_RejectsInvalidTarget()
[Fact] void InjectAsync_ValidatesCacheExists()
[Fact] void InjectAsync_SucceedsWithValidTarget()
[Fact] void DryRunAsync_ShowsInjectionPlan()
```

---

### Task 6.1: Nuke Integration (MODIFIED)

**Story Points:** 5 (increased from 3)  
**Priority:** High  
**Dependencies:** Task 4.4 (modified)

**Subtasks:**

- [ ] Create `Build.Preparation.cs` partial class
- [ ] Implement `PrepareCache` target (Phase 1)
- [ ] Implement `PrepareClient` target (Phase 2)
- [ ] Implement `RestoreClient` target
- [ ] Add git reset before injection (R-BLD-060)
- [ ] Call tool as external process
- [ ] Handle exit codes
- [ ] Add logging
- [ ] Add dependency chain validation
- [ ] Write integration tests

**Acceptance Criteria:**

- `PrepareCache` populates cache without touching client
- `PrepareClient` performs git reset before injection
- `PrepareClient` depends on `PrepareCache`
- `RestoreClient` performs git reset
- Nuke calls tool successfully
- Exit codes handled correctly
- Logs captured and displayed
- Tests pass for full build workflow

**Estimated Time:** 10 hours (increased from 6)

**Implementation:**

```csharp
// Build.Preparation.cs

partial class Build : IUnityBuild
{
    [Parameter("Path to preparation config")]
    AbsolutePath PreparationConfig => RootDirectory / "build" / "preparation" / "configs" / "default.json";

    AbsolutePath PreparationToolProject => RootDirectory / "packages" / "scoped-6571" /
        "com.contractwork.sangocard.build" / "dotnet~" / "tool" / "SangoCard.Build.Tool" /
        "SangoCard.Build.Tool.csproj";

    AbsolutePath ClientProject => RootDirectory / "projects" / "client";

    /// <summary>
    /// Phase 1: Populate preparation cache (safe anytime)
    /// </summary>
    Target PrepareCache => _ => _
        .Description("Populate preparation cache from code-quality project")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Phase 1: Populating Cache ===");

            DotNetRun(s => s
                .SetProjectFile(PreparationToolProject)
                .SetApplicationArguments(
                    "cache populate " +
                    "--source projects/code-quality " +
                    $"--config {PreparationConfig.ToRelativePath(RootDirectory)}")
                .SetProcessWorkingDirectory(RootDirectory)
            );

            Serilog.Log.Information("Cache population complete");
        });

    /// <summary>
    /// Phase 2: Inject preparation into client (build-time only)
    /// </summary>
    Target PrepareClient => _ => _
        .Description("Inject preparation into Unity client project")
        .DependsOn(PrepareCache)
        .Executes(() =>
        {
            Serilog.Log.Information("=== Phase 2: Injecting to Client ===");

            // R-BLD-060: Reset client before injection
            Serilog.Log.Information("Resetting client project (git reset --hard)...");
            Git("reset --hard", workingDirectory: ClientProject);

            // Inject from cache
            DotNetRun(s => s
                .SetProjectFile(PreparationToolProject)
                .SetApplicationArguments(
                    "prepare inject " +
                    $"--config {PreparationConfig.ToRelativePath(RootDirectory)} " +
                    "--target projects/client/")
                .SetProcessWorkingDirectory(RootDirectory)
            );

            Serilog.Log.Information("Client preparation complete");
        });

    /// <summary>
    /// Restore client to clean state (git reset)
    /// </summary>
    Target RestoreClient => _ => _
        .Description("Restore Unity client project to clean state")
        .Executes(() =>
        {
            Serilog.Log.Information("Restoring client project...");
            Git("reset --hard", workingDirectory: ClientProject);
            Serilog.Log.Information("Client restored");
        });

    /// <summary>
    /// Full build workflow with preparation
    /// </summary>
    Target BuildWithPreparation => _ => _
        .Description("Full Unity build with preparation")
        .DependsOn(PrepareClient)
        .DependsOn(((IUnityBuild)this).BuildUnity)
        .DependsOn(RestoreClient)
        .Executes(() =>
        {
            Serilog.Log.Information("Build with preparation complete");
        });
}
```

**Test Cases:**

```csharp
[Fact] void PrepareCache_DoesNotModifyClient()
[Fact] void PrepareClient_PerformsGitReset()
[Fact] void PrepareClient_DependsOnPrepareCache()
[Fact] void RestoreClient_PerformsGitReset()
[Fact] void BuildWithPreparation_RunsFullWorkflow()
[Fact] void PrepareClient_FailsIfCacheEmpty()
```

---

## New Task: 4.4.1 - Deprecation Support

**Story Points:** 2  
**Priority:** Medium  
**Dependencies:** Task 4.4

**Subtasks:**

- [ ] Keep `prepare run` command with deprecation warning
- [ ] Redirect to `prepare inject` with default target
- [ ] Add migration guide to help text
- [ ] Update documentation with deprecation notice
- [ ] Plan removal for next major version

**Acceptance Criteria:**

- `prepare run` shows deprecation warning
- `prepare run` still works (backward compat)
- Help text explains migration path
- Documentation updated

**Estimated Time:** 4 hours

**Implementation:**

```csharp
private Command CreatePrepareCommand()
{
    var prepare = new Command("prepare", "Execute build preparation");

    // NEW: inject command (recommended)
    var inject = new Command("inject", "Inject preparation into target project");
    var configOption = new Option<string>(
        aliases: new[] { "--config", "-c" },
        description: "Configuration file path (relative to git root)"
    ) { IsRequired = true };
    var targetOption = new Option<string>(
        aliases: new[] { "--target", "-t" },
        description: "Target project path (must be 'projects/client/')"
    ) { IsRequired = true };

    inject.AddOption(configOption);
    inject.AddOption(targetOption);
    inject.SetHandler(async (string config, string target) =>
    {
        var handler = _host.Services.GetRequiredService<PrepareCommandHandler>();
        await handler.InjectAsync(config, target);
    }, configOption, targetOption);

    // DEPRECATED: run command (backward compatibility)
    var run = new Command("run", "[DEPRECATED] Use 'inject --target projects/client/' instead");
    run.AddOption(configOption);
    run.SetHandler(async (string config) =>
    {
        Console.WriteLine("⚠️  WARNING: 'prepare run' is deprecated.");
        Console.WriteLine("   Use 'prepare inject --target projects/client/' instead.");
        Console.WriteLine("   This command will be removed in the next major version.");
        Console.WriteLine();

        var handler = _host.Services.GetRequiredService<PrepareCommandHandler>();
        await handler.InjectAsync(config, "projects/client/");
    }, configOption);

    prepare.AddCommand(inject);
    prepare.AddCommand(run);
    return prepare;
}
```

---

## Updated Task Dependencies

### Critical Path (Updated)

1. Task 1.1 → 1.2 → 1.3 (Core Infrastructure) ✅ Complete
2. Task 2.1 → 2.2 → 2.5 (Services) ✅ Complete
3. Task 3.2 → 3.4 (Critical Patchers) ✅ Complete
4. **Task 4.4 (Modified) - Prepare Commands** ⏳ In Progress
5. **Task 4.4.1 (New) - Deprecation Support** 🆕 New
6. **Task 6.1 (Modified) - Nuke Integration** ⏳ Ready to Start

### Task Timeline

**Week 4 (Current):**

- Task 4.4 modifications (2 days)
- Task 4.4.1 deprecation support (1 day)

**Week 5:**

- Task 6.1 Nuke integration (2 days)
- Integration testing (1 day)
- Documentation updates (1 day)

## Testing Strategy

### Unit Tests

- PrepareCommandHandler target validation
- CacheService isolation (never touches client)
- Path security checks

### Integration Tests

- Two-phase workflow end-to-end
- Cache populate → inject → build
- Error handling for missing cache
- Invalid target rejection

### E2E Tests

- Full Nuke build with PrepareCache → PrepareClient → BuildUnity → RestoreClient
- Git reset verification
- Rollback scenarios

## Documentation Updates

### Required Updates

- [ ] `build-preparation-tool.md` (spec)
- [ ] `build-preparation-tool-tasks.md` (tasks)
- [ ] `RFC-001-build-preparation-tool.md`
- [ ] `tool/README.md`
- [ ] Nuke component README
- [ ] Migration guide (new)

### New Documentation

- [ ] Two-phase workflow guide
- [ ] Safe usage patterns
- [ ] Troubleshooting guide

## Success Criteria

- [ ] All modified tasks complete
- [ ] Two-phase workflow functional
- [ ] `projects/client/` never modified outside build
- [ ] All tests passing
- [ ] Documentation updated
- [ ] Migration path clear

## Approval

**Amendment Author:** Build System Team  
**Reviewed By:** [Pending]  
**Approved By:** [Pending]  
**Date:** [Pending]
