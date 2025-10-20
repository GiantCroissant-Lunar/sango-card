# Build Tool Binary

This folder contains the compiled `SangoCard.Build.Tool.exe` - a self-contained executable used by the build preparation system.

## Not in Source Control

**The `.exe` file is NOT committed to git** (see `.gitignore`). This is intentional because:

- Large binary files (103 MB) bloat the repository
- Build artifacts should be generated, not stored
- Anyone can rebuild it from source

## How to Build

### Quick Command

```bash
task setup:build-tool
```

### With Options

```bash
# Build for different platforms
task setup:build-tool RUNTIME=win-x64     # Windows (default)
task setup:build-tool RUNTIME=linux-x64   # Linux
task setup:build-tool RUNTIME=osx-x64     # macOS Intel
task setup:build-tool RUNTIME=osx-arm64   # macOS Apple Silicon

# Debug build
task setup:build-tool CONFIG=Debug

# Or call Nuke directly
./build/nuke/build.ps1 BuildPreparationTool
```

### Build Time

First build: ~10-15 seconds  
Subsequent builds: ~5-7 seconds (if no changes)

## Output

After building, you'll see:

- `SangoCard.Build.Tool.exe` - Self-contained executable (103 MB)
- `SangoCard.Build.Tool.pdb` - Debug symbols (optional)

## When to Rebuild

Rebuild the tool when:

1. **After cloning** - First-time setup
2. **Source changes** - After modifying code in `dotnet~/tool/SangoCard.Build.Tool/`
3. **Dependency updates** - After updating NuGet packages
4. **Build issues** - If the tool crashes or behaves unexpectedly

## CI/CD Integration

In CI pipelines, add this step before using the preparation system:

```yaml
- name: Build preparation tool
  run: task setup:build-tool
```

## Technical Details

- **Source**: `../dotnet~/tool/SangoCard.Build.Tool/`
- **Project**: `SangoCard.Build.Tool.csproj`
- **Build Type**: Self-contained, single-file
- **Runtime**: .NET 8.0
- **Platforms**: win-x64, linux-x64, osx-x64, osx-arm64

## Troubleshooting

### Tool not found error

```
Error: Could not find SangoCard.Build.Tool.exe
Solution: Run `task setup:build-tool`
```

### Tool version mismatch

```
Warning: Tool may be outdated
Solution: Rebuild with `task setup:build-tool`
```

### Build fails

Check that you have:

- .NET 8.0 SDK installed
- All NuGet packages restored
- No conflicting processes

## See Also

- Build tool source: `../dotnet~/tool/SangoCard.Build.Tool/`
- Documentation: `docs/build-tool-optimization.md`
- Migration notes: `docs/build-tool-nuke-migration.md`
