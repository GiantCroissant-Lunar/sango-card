# Coding Patterns

Project-specific coding patterns and conventions for Sango Card.

## Partial Class Interface Separation (R-CODE-090)

### Pattern

When a C# class implements multiple interfaces, organize the code using partial classes with each interface in a separate file.

### Structure

```
ClassName.cs                    // Base class inheritance only
ClassName.InterfaceName1.cs     // First interface (without 'I' prefix)
ClassName.InterfaceName2.cs     // Second interface (without 'I' prefix)
ClassName.InterfaceName3.cs     // Additional interfaces...
```

**File Naming Convention**: Use the interface name without the 'I' prefix.

- Interface: `IUnityBuild` → File: `Build.UnityBuild.cs`
- Interface: `IDockerBuild` → File: `Build.DockerBuild.cs`
- Interface: `IAwsBuild` → File: `Build.AwsBuild.cs`

### Example: Build System

**Build.cs** - Base class inheritance:

```csharp
/// <summary>
/// Main build orchestration class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This class uses partial class pattern for interface segregation.
/// - Build.cs: Contains base NukeBuild inheritance only
/// - Build.IUnityBuild.cs: Contains IUnityBuild interface implementation
/// </summary>
partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    // Core build targets here
}
```

**Build.UnityBuild.cs** - Interface implementation:

```csharp
/// <summary>
/// Unity build interface implementation for Build class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This partial class file contains ONLY the IUnityBuild interface implementation.
/// File naming: Build.UnityBuild.cs (interface name without 'I' prefix)
/// </summary>
partial class Build : IUnityBuild
{
    // Override default interface members here if needed
    // All IUnityBuild-specific properties and methods belong in this file
}
```

### Benefits

1. **Separation of Concerns**: Each interface's implementation is isolated in its own file
2. **Maintainability**: Easier to locate and modify specific interface implementations
3. **Code Navigation**: IDEs can quickly jump to interface-specific code
4. **Testability**: Can mock/test individual interface implementations independently
5. **Team Collaboration**: Reduces merge conflicts when multiple developers work on different interfaces

### Applicability

This pattern applies to:

- ✅ Build scripts with multiple component interfaces (IUnityBuild → UnityBuild.cs, IDockerBuild → DockerBuild.cs, etc.)
- ✅ Component systems with multiple behavioral interfaces
- ✅ Service classes implementing multiple interface contracts
- ✅ Any class with 2+ interface implementations

**Important**: Interface-specific members (properties, methods) must be implemented in the interface-specific partial file, not in the base file.

Does NOT apply to:

- ❌ Classes with only base class inheritance (no interfaces)
- ❌ Classes implementing a single interface (optional, but not required)
- ❌ Unity MonoBehaviours with Unity component interfaces (use composition instead)

### Enforcement

| Layer | Status | Description |
|-------|--------|-------------|
| Agent Rules | ✅ Active | R-CODE-090 in `.agent/base/20-rules.md` |
| EditorConfig | ✅ Active | Documented in `.editorconfig` with comments |
| Code Review | ✅ Active | Manual review process |
| Pre-commit Hook | ✅ Active | Framework-managed via `.pre-commit-config.yaml` |
| Roslyn Analyzer | 🔄 Planned | Compile-time enforcement (see below) |

### Future: Roslyn Analyzer

A custom Roslyn analyzer will be developed to enforce this pattern at compile time:

**Analyzer ID**: `SANGO1001` - "Multiple interfaces should use separate partial class files"

**Detection Logic**:

- Trigger when a class declaration lists multiple interfaces in one file
- Severity: Warning (upgradeable to Error)
- Fix: Code action to split into partial files automatically

**Implementation Location**: `packages/analyzers/Sango.CodeQuality.Analyzers/`

### Pre-commit Hook

The pre-commit hook validates the pattern automatically before commit:

```bash
# Install pre-commit framework
pip install pre-commit
# OR
uv tool install pre-commit

# Setup hooks
task setup:hooks
# OR
pre-commit install
```

**What it checks**:

1. Scans staged C# files for classes implementing multiple interfaces
2. Verifies each interface has a corresponding `ClassName.InterfaceName.cs` file
3. Fails commit if pattern is violated with clear error messages
4. Shows expected file structure for corrections

**Hook Location**: Managed by `.pre-commit-config.yaml`  
**Script**: `scripts/git-hooks/pre-commit.ps1`  
**Documentation**: `docs/PRE-COMMIT.md`

### Migration Guide

For existing code that doesn't follow this pattern:

1. Identify the class and its interfaces
2. Create new partial file: `ClassName.InterfaceName.cs` (without 'I' prefix)
3. Move interface declaration to new file: `partial class ClassName : IInterfaceName`
4. Move interface-specific members (properties, methods) to the new file
5. Update base file to remove interface from declaration
6. Add XML documentation comments referencing R-CODE-090
7. Verify build and tests pass

**Example Migration**:

```csharp
// BEFORE: Build.cs
class Build : NukeBuild, IUnityBuild
{
    // Unity-specific property
    AbsolutePath UnityProjectPath => RootDirectory / "projects/client";

    Target BuildUnity => _ => _.Executes(() => { });
}

// AFTER: Build.cs
partial class Build : NukeBuild
{
    // Core build logic only
}

// AFTER: Build.UnityBuild.cs (NEW FILE)
partial class Build : IUnityBuild
{
    // Unity-specific property - moved here
    AbsolutePath UnityProjectPath => RootDirectory / "projects/client";

    Target BuildUnity => _ => _.Executes(() => { });
}
```

### References

- **Rule ID**: R-CODE-090
- **EditorConfig**: `.editorconfig` (root) and `build/nuke/build/.editorconfig`
- **Example**: `build/nuke/build/Build.cs` + `Build.UnityBuild.cs`
- **Agent Docs**: `.agent/adapters/*.md`
