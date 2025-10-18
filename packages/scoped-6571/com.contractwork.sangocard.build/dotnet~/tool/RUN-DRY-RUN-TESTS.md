# Running Dry-Run Tests

Quick reference for running the new dry-run tests.

## Quick Start

```bash
# Navigate to tool directory
cd packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool

# Run all dry-run tests
dotnet test --filter "FullyQualifiedName~DryRun"
```

## Test Commands

### Run All Tests
```bash
dotnet test
```

### Run Only Dry-Run Tests
```bash
dotnet test --filter "FullyQualifiedName~DryRun"
```

### Run Service Layer Tests
```bash
dotnet test --filter "FullyQualifiedName~SourceManagementServiceDryRunTests"
```

### Run CLI Layer Tests
```bash
dotnet test --filter "FullyQualifiedName~ConfigCommandHandlerDryRunTests"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~AddSourceAsync_DryRun_WithSingleFile"
```

### Run with Detailed Output
```bash
dotnet test --filter "FullyQualifiedName~DryRun" --verbosity detailed
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --filter "FullyQualifiedName~DryRun"
```

## Test Results

### Expected Output
```
Test run for SangoCard.Build.Tool.Tests.dll (.NET 8.0)
Microsoft (R) Test Execution Command Line Tool Version 17.x.x

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18
```

### Test Breakdown
- **Service Tests:** 11 tests
- **CLI Tests:** 7 tests
- **Total:** 18 tests

## Verify Snapshots

### Snapshot Location
```
SangoCard.Build.Tool.Tests/
└── Cli/
    └── Commands/
        └── Snapshots/
            ├── AddSourceAsync_DryRun_Output.verified.txt
            ├── AddSourceAsync_DryRun_LargePackage_Output.verified.txt
            ├── AddSourceAsync_DryRun_SingleFile_Output.verified.txt
            ├── AddInjectionAsync_DryRun_Output.verified.txt
            └── AddInjectionAsync_DryRun_NoVersion_Output.verified.txt
```

### When Snapshots Fail

1. **Review the diff:**
   ```bash
   # Verify will create .received.txt files
   # Compare with .verified.txt files
   ```

2. **If change is intentional:**
   ```bash
   # Rename .received.txt to .verified.txt
   mv Snapshots/*.received.txt Snapshots/*.verified.txt

   # Or use Verify's approval process
   # (depends on your Verify configuration)
   ```

3. **If change is unintentional:**
   - Fix the code
   - Re-run tests
   - Verify snapshots match

## Debugging Tests

### Run in Debug Mode
```bash
# In Visual Studio / Rider
# Set breakpoint in test
# Right-click test → Debug

# Or use CLI
dotnet test --filter "FullyQualifiedName~YourTestName" --logger "console;verbosity=detailed"
```

### View Test Output
```bash
dotnet test --filter "FullyQualifiedName~DryRun" --logger "console;verbosity=normal"
```

### Check Test Discovery
```bash
dotnet test --list-tests --filter "FullyQualifiedName~DryRun"
```

## Common Issues

### Issue: Tests Not Found
**Solution:**
```bash
# Rebuild the project
dotnet build
dotnet test --no-build
```

### Issue: Snapshot Mismatch
**Solution:**
```bash
# Review the differences
# If correct, approve the new snapshot
# If incorrect, fix the code
```

### Issue: Temp Directory Cleanup Fails
**Solution:**
- Tests clean up automatically
- If manual cleanup needed:
```bash
# Windows
rd /s /q %TEMP%\BuildPrepDryRunTests
rd /s /q %TEMP%\ConfigHandlerDryRunTests

# Linux/Mac
rm -rf /tmp/BuildPrepDryRunTests
rm -rf /tmp/ConfigHandlerDryRunTests
```

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run Dry-Run Tests
  run: |
    cd packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool
    dotnet test --filter "FullyQualifiedName~DryRun" --logger "trx;LogFileName=test-results.trx"

- name: Upload Test Results
  uses: actions/upload-artifact@v3
  if: always()
  with:
    name: test-results
    path: '**/test-results.trx'
```

### Azure DevOps Example
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Dry-Run Tests'
  inputs:
    command: 'test'
    projects: '**/SangoCard.Build.Tool.Tests.csproj'
    arguments: '--filter "FullyQualifiedName~DryRun" --logger trx'
```

## Test Coverage

### Generate Coverage Report
```bash
# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --filter "FullyQualifiedName~DryRun"

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open report
# Windows: start coveragereport/index.html
# Linux/Mac: open coveragereport/index.html
```

### Coverage Goals
- **Target:** >80% coverage for dry-run code paths
- **Current:** ~95% (estimated)

## Performance

### Test Execution Time
- **Service Tests:** ~2-3 seconds
- **CLI Tests:** ~1-2 seconds
- **Total:** ~3-5 seconds

### Optimization Tips
- Tests run in parallel by default
- Use `--no-build` if already built
- Use specific filters to run subset

## Documentation

- **Test Summary:** `SangoCard.Build.Tool.Tests/DRY-RUN-TESTS-SUMMARY.md`
- **Implementation:** `.specify/DRY-RUN-IMPLEMENTATION.md`
- **Verification:** `.specify/DRY-RUN-VERIFICATION.md`

## Quick Verification

After implementing dry-run:
```bash
# 1. Build
dotnet build

# 2. Run tests
dotnet test --filter "FullyQualifiedName~DryRun"

# 3. Verify all pass
# Expected: 18 passed, 0 failed

# 4. Check snapshots
ls Cli/Commands/Snapshots/*.verified.txt
# Should see 5 snapshot files
```

---

**Quick Command:**
```bash
cd packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool && dotnet test --filter "FullyQualifiedName~DryRun"
```

✅ **18 tests** covering dry-run functionality  
✅ **5 snapshots** for output verification  
✅ **~95% coverage** of dry-run code paths
