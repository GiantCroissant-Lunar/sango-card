# Git Hooks

Git hooks for enforcing code quality and project conventions.

## Available Hooks

### pre-commit.ps1

Enforces partial class interface separation pattern (R-CODE-090).

**What it checks**:
- Classes implementing multiple interfaces must use separate partial class files
- Base file (`ClassName.cs`) should contain only parent class inheritance
- Each interface should be in `ClassName.IInterfaceName.cs`

**Example violation**:
```csharp
// ❌ Build.cs
class Build : NukeBuild, IUnityBuild, IDockerBuild
{
    // ...
}
```

**Correct pattern**:
```csharp
// ✅ Build.cs
partial class Build : NukeBuild
{
    // ...
}

// ✅ Build.UnityBuild.cs (IUnityBuild → UnityBuild, without 'I')
partial class Build : IUnityBuild
{
    // ...
}

// ✅ Build.DockerBuild.cs (IDockerBuild → DockerBuild, without 'I')
partial class Build : IDockerBuild
{
    // ...
}
```

**File Naming**: Interface files use the interface name without the 'I' prefix.

## Installation

### Recommended: Pre-commit Framework

The project uses [pre-commit](https://pre-commit.com/) for managing git hooks:

```bash
# Install pre-commit (choose one)
pip install pre-commit
# OR
uv tool install pre-commit

# Setup hooks (done automatically by `task setup`)
task setup:hooks
# OR manually
pre-commit install
pre-commit install --hook-type commit-msg
```

This approach provides:
- ✅ Cross-platform compatibility
- ✅ Automatic hook updates
- ✅ Multiple hook integration (linting, formatting, secrets detection)
- ✅ Configuration via `.pre-commit-config.yaml`

### Alternative: Direct Hook Installation

**Windows (PowerShell as Administrator)**:
```powershell
New-Item -ItemType SymbolicLink -Path ".git\hooks\pre-commit" -Target "..\..\scripts\git-hooks\pre-commit.ps1"
```

**Linux/macOS**:
```bash
ln -s ../../scripts/git-hooks/pre-commit.ps1 .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

## Bypassing Hooks

If you need to commit code that violates the pattern (e.g., work in progress):

```bash
git commit --no-verify
```

⚠️ **Warning**: Bypassing hooks should be rare. The pattern violations will eventually need to be fixed.

## Testing Hooks

### With pre-commit framework

```bash
# Run all hooks on all files
pre-commit run --all-files

# Run specific hook
pre-commit run partial-class-interface-separation --all-files

# Run on staged files only (normal pre-commit behavior)
pre-commit run
```

### Direct script execution

```powershell
pwsh scripts/git-hooks/pre-commit.ps1 -Verbose
```

## Debugging

If a hook fails unexpectedly:

1. Run the hook script directly with `-Verbose` flag
2. Check the hook output for specific violations
3. Review `docs/CODING-PATTERNS.md` for pattern details
4. If the hook is incorrect, file an issue

## Future Enhancements

- [ ] **Roslyn Analyzer**: Compile-time enforcement (analyzer ID: SANGO1001)
- [ ] **Auto-fix**: Code action to automatically split files
- [ ] **commit-msg hook**: Enforce conventional commit format
- [ ] **pre-push hook**: Run quick tests before push

## References

- **Rule**: R-CODE-090 in `.agent/base/20-rules.md`
- **Documentation**: `docs/CODING-PATTERNS.md`
- **Example**: `build/nuke/build/Build.cs` + `Build.UnityBuild.cs`
