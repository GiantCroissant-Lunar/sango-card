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

Git hooks are installed automatically when running:

```bash
task setup
```

This creates a symlink from `.git/hooks/pre-commit` to `scripts/git-hooks/pre-commit.ps1`.

### Manual Installation

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

Run the pre-commit hook manually:

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
