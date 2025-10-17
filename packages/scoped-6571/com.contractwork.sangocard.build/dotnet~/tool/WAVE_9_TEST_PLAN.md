# Wave 9 Test Plan: Integration & Testing

**Date:** 2025-10-17  
**Status:** In Progress

---

## 🎯 Testing Objectives

1. Fix pre-existing test failures (5 failing tests in patchers)
2. Add integration tests for TUI views
3. Create end-to-end workflow tests
4. Perform manual testing of TUI interactions
5. Document test coverage

---

## 📊 Current Test Status

### Existing Tests (Before Wave 9)

**Total Test Files**: 14 files

- ✅ ModelSerializationTests.cs
- ✅ PreparationConfigTests.cs
- ❌ CSharpPatcherSnapshotTests.cs (2 failing)
- ❌ CSharpPatcherTests.cs (2 failing)
- ✅ JsonPatcherTests.cs
- ✅ PatcherTests.cs (1 failing - UnityAssetPatcher)
- ✅ TextPatcherTests.cs
- ✅ UnityAssetPatcherTests.cs
- ✅ CacheServiceTests.cs
- ✅ ConfigServiceTests.cs
- ✅ PreparationServiceTests.cs
- ✅ ValidationServiceTests.cs
- ✅ GitHelperTests.cs
- ✅ PathResolverTests.cs

**Failed Tests Summary**:

1. `UnityAssetPatcher_Replace_ShouldReplaceValue` - PatcherTests.cs
2. `RemoveUsing_RemovesTargetUsingDirective` - CSharpPatcherTests.cs
3. `RemoveBlock_RemovesMatchingBlock` - CSharpPatcherTests.cs
4. `DryRun_PreviewGeneration_SnapshotMatch` - CSharpPatcherSnapshotTests.cs
5. `FallbackToTextMode_NoOperation` - CSharpPatcherSnapshotTests.cs

---

## 🧪 Wave 9 Testing Tasks

### Task 9.1: Fix Pre-existing Test Failures ⏳

**Priority**: High  
**Estimated Time**: 2-3 hours

1. **UnityAssetPatcher Test**
   - Issue: Replace operation failing
   - Root cause: YAML path matching issue
   - Fix: Update UnityAssetPatcher.Replace logic

2. **CSharpPatcher RemoveUsing Test**
   - Issue: Using directive removal not working
   - Root cause: Roslyn syntax tree manipulation
   - Fix: Update RemoveUsing operation

3. **CSharpPatcher RemoveBlock Test**
   - Issue: Block removal not working
   - Root cause: Node identification or removal logic
   - Fix: Update RemoveBlock operation

4. **CSharpPatcher Snapshot Tests (2)**
   - Issue: Snapshot mismatches
   - Root cause: Verification files need updating
   - Fix: Accept new snapshots if behavior is correct

---

### Task 9.2: TUI View Integration Tests ⏳

**Priority**: High  
**Estimated Time**: 3-4 hours

Create integration tests for each TUI view:

**ConfigEditorViewTests.cs**

- ✅ View initialization with services
- ✅ Load configuration flow
- ✅ Create new configuration flow
- ✅ Add package to configuration
- ✅ Add assembly to configuration
- ✅ Add code patch to configuration
- ✅ Save configuration flow
- ✅ MessagePipe message handling

**CacheManagementViewTests.cs**

- ✅ View initialization
- ✅ Populate cache flow
- ✅ List cache items
- ✅ Clean cache flow
- ✅ MessagePipe updates

**ValidationViewTests.cs**

- ✅ View initialization
- ✅ Load and validate config
- ✅ Validation level selection
- ✅ Error/warning display
- ✅ MessagePipe integration

**PreparationExecutionViewTests.cs**

- ✅ View initialization
- ✅ Load configuration
- ✅ Dry-run execution flow
- ✅ Real execution flow
- ✅ Progress tracking
- ✅ File operation tracking
- ✅ MessagePipe integration

---

### Task 9.3: End-to-End Workflow Tests ⏳

**Priority**: Medium  
**Estimated Time**: 2-3 hours

**E2EWorkflowTests.cs**

1. **Complete Preparation Workflow**

   ```
   Create Config → Populate Cache → Validate → Prepare (Dry-Run) → Prepare (Execute)
   ```

2. **Config Round-Trip Workflow**

   ```
   Create → Save → Load → Modify → Save → Validate
   ```

3. **Cache Management Workflow**

   ```
   Populate → List → Validate References → Clean
   ```

4. **Error Handling Workflow**

   ```
   Invalid Config → Validation Fails → User Corrections → Success
   ```

---

### Task 9.4: CLI Command Integration Tests ⏳

**Priority**: Medium  
**Estimated Time**: 2 hours

**CliIntegrationTests.cs**

- Test all 14 CLI commands
- Test command chaining
- Test error handling
- Test help output
- Test parameter validation

---

### Task 9.5: Performance Tests ⏳

**Priority**: Low  
**Estimated Time**: 1-2 hours

**PerformanceTests.cs**

- Cache population performance (1000+ files)
- Validation performance (large configs)
- Preparation execution performance
- Memory usage profiling
- MessagePipe throughput

---

### Task 9.6: Manual Testing Scenarios 📝

**Priority**: High  
**Estimated Time**: 2-3 hours

1. **TUI Navigation Testing**
   - All menu items work
   - F-key shortcuts work
   - View switching works
   - Quit confirmation works

2. **ConfigEditorView Testing**
   - Create new config manually
   - Add multiple packages
   - Add multiple assemblies
   - Add code patches with different types
   - Save to custom path
   - Load existing config
   - Verify UI updates

3. **CacheManagementView Testing**
   - Populate with different source paths
   - Verify item display formatting
   - Check statistics accuracy
   - Clean cache with confirmation
   - Refresh after operations

4. **ValidationView Testing**
   - Load valid config - should pass
   - Load invalid config - should show errors
   - Test each validation level
   - Verify error/warning split display
   - Check error messages are helpful

5. **PreparationExecutionView Testing**
   - Dry-run with valid config
   - Dry-run with invalid config
   - Real execution (careful!)
   - Stop button functionality
   - Log scrolling
   - Statistics accuracy
   - File operation tracking

6. **Stress Testing**
   - Large configuration files
   - Many cache items
   - Long-running preparations
   - Rapid view switching
   - Multiple MessagePipe events

---

## 📈 Test Coverage Goals

### Current Coverage (Wave 1-8)

- Core Models: ~90%
- Core Services: ~85%
- Patchers: ~75% (failing tests)
- Utilities: ~95%
- CLI: ~0% (needs tests)
- TUI: ~0% (needs tests)

### Wave 9 Target Coverage

- Core Models: 90%+ (maintain)
- Core Services: 85%+ (maintain)
- Patchers: 95%+ (fix failures)
- Utilities: 95%+ (maintain)
- CLI: 70%+ (NEW)
- TUI: 60%+ (NEW)
- **Overall**: 80%+

---

## 🔧 Testing Tools & Frameworks

- **xUnit**: Test framework
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library
- **Verify.Xunit**: Snapshot testing
- **Microsoft.NET.Test.Sdk**: Test SDK
- **coverlet.collector**: Code coverage

---

## ✅ Success Criteria

Wave 9 is complete when:

1. ✅ All pre-existing test failures fixed (0/5 remaining)
2. ✅ TUI view integration tests written and passing (4 test files)
3. ✅ End-to-end workflow tests written and passing (1 test file)
4. ✅ CLI integration tests written and passing (1 test file)
5. ✅ All manual testing scenarios documented
6. ✅ Test coverage ≥80% overall
7. ✅ Performance benchmarks documented
8. ✅ No regressions in existing tests

---

## 📝 Test Execution Commands

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test class
dotnet test --filter "FullyQualifiedName~ConfigEditorViewTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run in watch mode (for TDD)
dotnet watch test
```

---

## 🎯 Priority Order

1. **Fix Pre-existing Failures** (Must do first - unblocks everything)
2. **TUI View Integration Tests** (Core functionality)
3. **Manual Testing** (Validate user experience)
4. **End-to-End Workflow Tests** (Integration validation)
5. **CLI Integration Tests** (Medium priority)
6. **Performance Tests** (Nice to have)

---

## 📊 Progress Tracking

- [ ] Task 9.1: Fix Pre-existing Test Failures (0/5 tests fixed)
- [ ] Task 9.2: TUI View Integration Tests (0/4 test files created)
- [ ] Task 9.3: End-to-End Workflow Tests (0/1 test file created)
- [ ] Task 9.4: CLI Command Integration Tests (0/1 test file created)
- [ ] Task 9.5: Performance Tests (0/1 test file created)
- [ ] Task 9.6: Manual Testing Scenarios (0/6 scenarios tested)

**Overall Wave 9 Progress**: 0% (0/6 tasks complete)

---

**Next Steps**: Begin with Task 9.1 - Fix the 5 failing tests
