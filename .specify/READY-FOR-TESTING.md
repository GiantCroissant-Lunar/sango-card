# Ready for Testing: Two-Phase Workflow

**Date:** 2025-10-17  
**Status:** ✅ Implementation Complete, Ready for Testing  
**Implementation Time:** ~3 hours

## 🎉 What's Complete

### Agent 2 Work (100% Complete)

✅ **Task 4.4: CLI Modifications**

- New `prepare inject` command with target validation
- Backward-compatible `prepare run` with deprecation warning
- Cache existence validation
- Clear error messages

✅ **Task 6.1: Nuke Integration**

- `Build.Preparation.cs` with 6 targets
- Two-phase workflow automation
- Git reset integration (R-BLD-060)
- Dry-run support

✅ **Documentation**

- Implementation complete document
- Coordination status updated
- Ready for testing checklist

### Agent 1 Work (In Progress)

⏳ **Wave 8: TUI Views** (8-12 hours estimated)

- View stubs created
- Full implementation in progress

## 📋 Testing Checklist

### Phase 1: Cache Population

```bash
# Test cache populate
cd D:\lunar-snake\constract-work\card-projects\sango-card

# Using Nuke
nuke PrepareCache

# Using tool directly
dotnet run --project packages/scoped-6571/.../SangoCard.Build.Tool \
  -- cache populate --source projects/code-quality
```

**Expected:**

- ✅ Files copied to `build/preparation/cache/`
- ✅ No modifications to `projects/client/`
- ✅ Exit code 0
- ✅ Duration < 10 seconds

### Phase 2: Injection

```bash
# Test injection with Nuke
nuke PrepareClient

# Test injection with tool directly
dotnet run --project packages/scoped-6571/.../SangoCard.Build.Tool \
  -- prepare inject --config build/preparation/configs/default.json --target projects/client/
```

**Expected:**

- ✅ Git reset performed on `projects/client/`
- ✅ Files injected from cache
- ✅ Exit code 0
- ✅ Duration < 20 seconds

### Phase 3: Full Workflow

```bash
# Test full build workflow
nuke BuildUnityWithPreparation
```

**Expected:**

- ✅ PrepareCache runs
- ✅ PrepareClient runs (with git reset)
- ✅ BuildUnity runs
- ✅ RestoreClient runs (cleanup)
- ✅ Client project clean after build

### Error Scenarios

#### Test 1: Invalid Target

```bash
dotnet run --project packages/scoped-6571/.../SangoCard.Build.Tool \
  -- prepare inject --config <path> --target projects/wrong/
```

**Expected:**

- ❌ Error: "Target must be 'projects/client/' per R-BLD-060"
- ❌ Exit code 1

#### Test 2: Missing Cache

```bash
# Clear cache first
rm -rf build/preparation/cache/*

# Try to inject
nuke PrepareClient
```

**Expected:**

- ❌ Error: "Cache files not found. Run 'cache populate' first"
- ❌ Exit code 1
- ❌ Lists missing files

#### Test 3: Deprecation Warning

```bash
dotnet run --project packages/scoped-6571/.../SangoCard.Build.Tool \
  -- prepare run --config <path>
```

**Expected:**

- ⚠️ Deprecation warning displayed
- ✅ Still works (redirects to inject)
- ✅ Exit code 0

### Dry-Run Testing

```bash
# Test dry-run
nuke DryRunPreparation
```

**Expected:**

- ✅ Shows what would be injected
- ✅ No actual file modifications
- ✅ Exit code 0

### Validation Testing

```bash
# Test validation
nuke ValidatePreparation
```

**Expected:**

- ✅ Validates config
- ✅ Shows validation results
- ✅ Exit code 0 if valid

## 🔧 Manual Testing Commands

### Quick Test Suite

```bash
# 1. Validate config
nuke ValidatePreparation

# 2. Populate cache
nuke PrepareCache

# 3. Dry-run injection
nuke DryRunPreparation

# 4. Execute injection
nuke PrepareClient

# 5. Restore client
nuke RestoreClient

# 6. Full workflow
nuke BuildUnityWithPreparation
```

### Verify Git Reset

```bash
# Before injection
cd projects/client
git status  # Should be clean

# After injection
nuke PrepareClient
git status  # Should show modifications

# After restore
nuke RestoreClient
git status  # Should be clean again
```

## 📊 Success Criteria

### Functional Requirements

- [ ] Phase 1 (cache populate) works independently
- [ ] Phase 2 (inject) validates target path
- [ ] Phase 2 validates cache exists
- [ ] Git reset performed before injection
- [ ] Restore cleans up client project
- [ ] Full workflow completes successfully
- [ ] Deprecation warning shows for old command
- [ ] Invalid targets rejected
- [ ] Missing cache detected

### Non-Functional Requirements

- [ ] PrepareCache < 10 seconds
- [ ] PrepareClient < 20 seconds
- [ ] Full workflow < 30 seconds
- [ ] Clear error messages
- [ ] Verbose output helpful
- [ ] Exit codes correct

### Compliance

- [ ] R-BLD-060: Client never modified outside build
- [ ] R-SPEC-010: Follows spec-kit workflow
- [ ] R-CODE-090: Partial class pattern used
- [ ] R-CODE-110: Cross-platform paths

## 🐛 Known Issues

### None Currently

All planned features implemented successfully.

## 📝 Testing Notes

### Environment Setup

**Prerequisites:**

- .NET 8.0 SDK installed
- Git installed
- Unity 6000.2.x installed (for full build)
- Nuke.GlobalTool installed

**Configuration:**

- Default config: `build/preparation/configs/default.json`
- Source: `projects/code-quality`
- Target: `projects/client`
- Cache: `build/preparation/cache`

### Test Data

Create a minimal test config if needed:

```json
{
  "version": "1.0",
  "description": "Test configuration",
  "packages": [],
  "assemblies": [],
  "assetManipulations": [],
  "codePatches": []
}
```

## 🤝 Coordination

### Agent 1 Status

⏳ **Wave 8 TUI Views** - In progress (8-12 hours)

- View stubs created
- Full implementation ongoing
- No conflicts with Agent 2 work

### Joint Testing Plan

**When Agent 1 completes Wave 8:**

1. **Agent 1:** Signal completion
2. **Both:** Review changes
3. **Both:** Run test suite together
4. **Both:** Fix any issues found
5. **Both:** Update documentation
6. **Both:** Mark ready for production

## 📚 Documentation Status

### ✅ Created

- `.specify/specs/build-preparation-tool-amendment-001.md`
- `.specify/tasks/build-preparation-tool-tasks-amendment-001.md`
- `.specify/specs/AMENDMENT-001-SUMMARY.md`
- `.specify/COORDINATION-STATUS.md`
- `.specify/DECISION-NEEDED.md`
- `.specify/IMPLEMENTATION-COMPLETE.md`
- `.specify/READY-FOR-TESTING.md` (this file)

### ⏳ Pending Updates

- `tool/README.md` - Add two-phase workflow section
- `build/nuke/build/Components/README.md` - Add preparation targets
- `.specify/specs/build-preparation-tool.md` - Merge amendment
- `.specify/tasks/build-preparation-tool-tasks.md` - Merge amendment

## 🚀 Next Steps

### Immediate

1. ⏳ Wait for Agent 1 Wave 8 completion
2. 🔜 Schedule joint testing session
3. 🔜 Run test suite
4. 🔜 Fix any issues
5. 🔜 Update documentation

### After Testing

1. Merge amendments into main spec/tasks
2. Update tool README
3. Update Nuke component README
4. Create migration guide
5. Mark ready for production

## 📞 Contact

**Agent 2 Status:** ✅ Available for testing  
**Agent 1 Status:** ⏳ Working on Wave 8  
**Estimated Joint Testing:** After Wave 8 complete (~8-12 hours)

---

**Implementation:** ✅ Complete  
**Testing:** ⏳ Pending  
**Documentation:** ⏳ Pending  
**Production Ready:** 🔜 After testing
