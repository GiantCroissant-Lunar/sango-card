---
title: Report Build Component
type: guide
status: active
created: 2025-10-18
tags:
  - nuke
  - build
  - reporting
  - dotnet
---

# Report Build Component

Nuke build component for SangoCard Reporting libraries.

## Overview

The `IReportBuild` component provides automated build, test, and packaging for the reporting system located at `dotnet/report/`.

## Projects

- **Reporting.Abstractions** - Core abstractions (.NET Standard 2.1 + .NET 6.0)
- **Reporting.Core** - Report engine implementation (.NET 6.0)
- **Tests** - Unit tests (44 tests total)

## Available Targets

### Core Targets

#### `CleanReport`

Clean report build artifacts (bin/obj directories).

```bash
nuke CleanReport
```

#### `RestoreReport`

Restore NuGet packages for report solution.

```bash
nuke RestoreReport
```

#### `BuildReport`

Build all report libraries (Abstractions + Core).

```bash
nuke BuildReport
```

**Dependencies:** `RestoreReport`

#### `TestReport`

Run unit tests for report libraries (44 tests).

```bash
nuke TestReport
```

**Dependencies:** `BuildReport`

#### `PackReport`

Pack report libraries as NuGet packages.

```bash
nuke PackReport
```

**Dependencies:** `BuildReport`

**Output:** `build/_artifacts/<version>/report/*.nupkg`

### Publishing Targets

#### `PublishReportAbstractions`

Publish Reporting.Abstractions for distribution.

```bash
nuke PublishReportAbstractions
```

**Output:** `build/_artifacts/<version>/report/Reporting.Abstractions/`

#### `PublishReportCore`

Publish Reporting.Core for distribution.

```bash
nuke PublishReportCore
```

**Output:** `build/_artifacts/<version>/report/Reporting.Core/`

#### `PublishReport`

Publish all report libraries.

```bash
nuke PublishReport
```

**Dependencies:** `PublishReportAbstractions`, `PublishReportCore`

### Pipeline Target

#### `ReportFull`

Full report build pipeline: clean → build → test → pack.

```bash
nuke ReportFull
```

**Dependencies:** `CleanReport`, `BuildReport`, `TestReport`, `PackReport`

**Use this for:** Complete build verification before commits.

## Parameters

### Configuration

```bash
# Debug build (default for local)
nuke BuildReport --ReportConfiguration Debug

# Release build (default for CI)
nuke BuildReport --ReportConfiguration Release
```

### Versioning

```bash
# Use specific version suffix
nuke PackReport --ReportVersionSuffix 1.2.3

# Use CI build number
nuke PackReport --ReportVersionSuffix ci-1234

# Use GitVersion (automatic)
nuke PackReport
```

### Custom Paths

```bash
# Custom solution path
nuke BuildReport --ReportSolutionPath "path/to/custom.sln"

# Custom output directory
nuke PackReport --ReportOutput "custom/output/path"

# Custom artifacts root
nuke PackReport --ReportArtifactsRoot "custom/artifacts"
```

### Framework Override

```bash
# Build for specific framework
nuke BuildReport --ReportFramework net6.0

# Publish for specific framework
nuke PublishReport --ReportFramework netstandard2.1
```

## Common Workflows

### Local Development

```bash
# Quick build and test
nuke BuildReport TestReport

# Full pipeline with packages
nuke ReportFull
```

### CI/CD Pipeline

```bash
# Clean build with version
nuke ReportFull --ReportConfiguration Release --ReportVersionSuffix ci-$BUILD_NUMBER

# Publish for distribution
nuke PublishReport --ReportConfiguration Release
```

### Package Creation

```bash
# Create NuGet packages
nuke PackReport --ReportConfiguration Release

# Output: build/_artifacts/<version>/report/
#   - SangoCard.Reporting.Abstractions.<version>.nupkg
#   - SangoCard.Reporting.Core.<version>.nupkg
```

### Testing Only

```bash
# Run tests without rebuild
nuke TestReport

# Run tests with fresh build
nuke CleanReport BuildReport TestReport
```

## Output Structure

```
build/_artifacts/
└── <version>/              # e.g., "1.2.3" or "local"
    └── report/
        ├── *.nupkg                          # NuGet packages (from PackReport)
        ├── Reporting.Abstractions/          # Published binaries (from PublishReport)
        │   ├── netstandard2.1/
        │   └── net6.0/
        └── Reporting.Core/                  # Published binaries (from PublishReport)
            └── net6.0/
```

## Integration with Other Components

### Combined with DotNet Build

```bash
# Build both report and other .NET projects
nuke BuildDotNet BuildReport
```

### Combined with Unity Build

```bash
# Full build pipeline
nuke ReportFull UnityBuild
```

### Combined with Preparation

```bash
# Build report, then prepare for Unity
nuke BuildReport PrepareUnity
```

## Versioning Strategy

### Automatic (GitVersion)

When no version suffix is provided, GitVersion is used:

```bash
nuke PackReport
# Uses GitVersion.NuGetVersionV2 (e.g., "1.2.3-beta.4")
```

### Manual Version

```bash
nuke PackReport --ReportVersionSuffix 1.2.3
# Packages: SangoCard.Reporting.*.1.2.3.nupkg
```

### CI Build Number

```bash
nuke PackReport --ReportVersionSuffix ci-$BUILD_NUMBER
# Packages: SangoCard.Reporting.*.ci-1234.nupkg
```

## Error Handling

### Solution Not Found

```
Report solution not found: D:\...\dotnet\report\SangoCard.sln
```

**Solution:** Verify the report directory exists and contains `SangoCard.sln`.

### Project Not Found

```
Reporting.Abstractions project not found: D:\...\Reporting.Abstractions.csproj
```

**Solution:** Ensure the project structure is intact.

### Build Failures

```bash
# Clean and retry
nuke CleanReport BuildReport
```

### Test Failures

```bash
# Run tests with detailed output
dotnet test dotnet/report/SangoCard.sln --verbosity detailed
```

## Architecture Notes

### Component Design (R-CODE-090)

- **Interface:** `Components/IReportBuild.cs` - Reusable component
- **Implementation:** `Build.ReportBuild.cs` - Partial class integration
- **Separation:** Improves code organization and maintainability

### Default Paths

- **Solution:** `dotnet/report/SangoCard.sln`
- **Abstractions:** `dotnet/report/Reporting.Abstractions/`
- **Core:** `dotnet/report/Reporting.Core/`
- **Tests:** `dotnet/report/tests/`
- **Output:** `build/_artifacts/<version>/report/`

### Target Dependencies

```
ReportFull
├── CleanReport
├── BuildReport
│   └── RestoreReport
├── TestReport
│   └── BuildReport
└── PackReport
    └── BuildReport

PublishReport
├── PublishReportAbstractions
│   └── BuildReport
└── PublishReportCore
    └── BuildReport
```

## Examples

### Example 1: Local Development Build

```bash
# Build and test
nuke BuildReport TestReport

# Expected output:
# [INFO] Restoring report solution: D:\...\dotnet\report\SangoCard.sln
# [INFO] Building report solution: D:\...\dotnet\report\SangoCard.sln
# [INFO] Report solution built successfully.
# [INFO] Running report tests...
# [INFO] Report tests completed.
```

### Example 2: Release Package Creation

```bash
# Full pipeline with release configuration
nuke ReportFull --ReportConfiguration Release --ReportVersionSuffix 1.0.0

# Expected output:
# [INFO] Cleaning report artifacts...
# [INFO] Building report solution...
# [INFO] Running report tests...
# [INFO] Packing report libraries to: D:\...\build\_artifacts\1.0.0\report
# [INFO] Generated packages:
#   - SangoCard.Reporting.Abstractions.1.0.0.nupkg (12,345 bytes)
#   - SangoCard.Reporting.Core.1.0.0.nupkg (45,678 bytes)
```

### Example 3: CI/CD Integration

```yaml
# GitHub Actions
- name: Build Report Libraries
  run: nuke ReportFull --ReportConfiguration Release --ReportVersionSuffix ci-${{ github.run_number }}

# Azure DevOps
- script: nuke ReportFull --ReportConfiguration Release --ReportVersionSuffix ci-$(Build.BuildNumber)
  displayName: 'Build Report Libraries'
```

### Example 4: Publish for Distribution

```bash
# Publish both libraries
nuke PublishReport --ReportConfiguration Release

# Output structure:
# build/_artifacts/local/report/
# ├── Reporting.Abstractions/
# │   ├── netstandard2.1/
# │   │   └── SangoCard.Reporting.Abstractions.dll
# │   └── net6.0/
# │       └── SangoCard.Reporting.Abstractions.dll
# └── Reporting.Core/
#     └── net6.0/
#         ├── SangoCard.Reporting.Core.dll
#         └── (dependencies)
```

## Troubleshooting

### Issue: Tests Fail

**Check:**

1. Run tests directly: `dotnet test dotnet/report/SangoCard.sln`
2. Check test output for specific failures
3. Verify all dependencies are restored

### Issue: Pack Creates Wrong Version

**Check:**

1. Verify `--ReportVersionSuffix` parameter
2. Check GitVersion configuration
3. Inspect `.csproj` files for hardcoded versions

### Issue: Output Directory Not Found

**Check:**

1. Verify `--ReportOutput` parameter
2. Ensure parent directories exist
3. Check file system permissions

## Related Documentation

- **Report Libraries:** `dotnet/report/README.md`
- **Project Summary:** `dotnet/report/PROJECT-SUMMARY.md`
- **Usage Examples:** `dotnet/report/USAGE-EXAMPLES.md`
- **Nuke Build:** `build/nuke/README.md`

## Quick Reference

```bash
# Development
nuke BuildReport                    # Build only
nuke TestReport                     # Build + Test
nuke ReportFull                     # Full pipeline

# Packaging
nuke PackReport                     # Create NuGet packages
nuke PublishReport                  # Publish binaries

# CI/CD
nuke ReportFull --ReportConfiguration Release --ReportVersionSuffix ci-1234

# Custom
nuke BuildReport --ReportFramework net6.0
nuke PackReport --ReportOutput custom/path
```

---

**Component:** IReportBuild  
**Location:** `build/nuke/build/Components/IReportBuild.cs`  
**Implementation:** `build/nuke/build/Build.ReportBuild.cs`  
**Status:** ✅ Ready to use
