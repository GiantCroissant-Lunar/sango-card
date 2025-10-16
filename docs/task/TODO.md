# TODO: Roslyn Analyzer for R-CODE-090

## Objective

Create a custom Roslyn analyzer to enforce the partial class interface separation pattern at compile time.

## Analyzer Details

**Analyzer ID**: `SANGO1001`  
**Title**: "Multiple interfaces should use separate partial class files"  
**Category**: CodeQuality  
**Default Severity**: Warning  
**Description**: Classes implementing multiple interfaces should organize each interface in a separate partial class file for better maintainability.

## Detection Logic

The analyzer should:

1. Detect class declarations implementing 2+ interfaces
2. Check if the class is declared as `partial`
3. Verify corresponding `ClassName.IInterfaceName.cs` files exist
4. Report diagnostic if pattern is violated

### Example Violations

```csharp
// Violation 1: Multiple interfaces in single file, not partial
class Build : NukeBuild, IUnityBuild
{
}

// Violation 2: Partial but all interfaces in one file
partial class Build : NukeBuild, IUnityBuild, IDockerBuild
{
}
```

### Valid Patterns

```csharp
// Build.cs - Base file with parent class only
partial class Build : NukeBuild
{
}

// Build.UnityBuild.cs - Interface in separate file (IUnityBuild → UnityBuild)
partial class Build : IUnityBuild
{
}
```

**File Naming Convention**: Interface files use the interface name without the 'I' prefix.

- `IUnityBuild` → `Build.UnityBuild.cs`
- `IDockerBuild` → `Build.DockerBuild.cs`

## Code Fix Provider

The analyzer should include a code fix that:

1. Makes the class `partial` if not already
2. Removes all interface declarations from the base file except the first item (parent class)
3. Creates new files `ClassName.InterfaceName.cs` (without 'I' prefix) for each interface
4. Adds proper using statements to new files
5. Adds XML documentation comments referencing R-CODE-090
6. Moves interface-specific members to appropriate files

## Implementation Structure

```
packages/analyzers/
└── Sango.CodeQuality.Analyzers/
    ├── Sango.CodeQuality.Analyzers.csproj
    ├── PartialClassInterfaceSeparationAnalyzer.cs
    ├── PartialClassInterfaceSeparationCodeFixProvider.cs
    └── Resources.resx (for diagnostic messages)
```

## Project Setup

Create new analyzer project:

```bash
dotnet new analyzer -n Sango.CodeQuality.Analyzers -o packages/analyzers/Sango.CodeQuality.Analyzers
```

Add to solution/build system.

## Testing

Create unit tests in:

```
packages/analyzers/Sango.CodeQuality.Analyzers.Tests/
├── PartialClassInterfaceSeparationAnalyzerTests.cs
└── PartialClassInterfaceSeparationCodeFixProviderTests.cs
```

Test cases:

- ✅ Single interface (no diagnostic)
- ✅ Multiple interfaces with proper separation (no diagnostic)
- ❌ Multiple interfaces in one file (diagnostic)
- ❌ Non-partial class with multiple interfaces (diagnostic)
- ✅ Code fix creates correct file structure
- ✅ Code fix preserves using statements
- ✅ Code fix adds documentation comments

## Configuration

Allow projects to opt-in/opt-out via `.editorconfig`:

```ini
# Enable for build projects
[build/nuke/build/*.cs]
dotnet_diagnostic.SANGO1001.severity = error

# Disable for Unity scripts (use composition instead)
[projects/client/Assets/**/*.cs]
dotnet_diagnostic.SANGO1001.severity = none
```

## Integration

1. Add analyzer package reference to relevant projects
2. Update CI/CD pipeline to fail on analyzer errors
3. Add analyzer to central package management
4. Document in `docs/CODING-PATTERNS.md`

## References

- [Roslyn Analyzer Tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [Analyzer API Reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostics)
- **Rule**: R-CODE-090
- **Pattern Doc**: `docs/CODING-PATTERNS.md`
- **Example**: `build/nuke/build/Build.cs` + `Build.UnityBuild.cs`

## Priority

**Medium** - Pre-commit hook provides runtime enforcement. Analyzer adds compile-time validation.

## Estimated Effort

- Analyzer implementation: 4-6 hours
- Code fix provider: 4-6 hours
- Testing: 3-4 hours
- Documentation: 1-2 hours
- **Total**: 12-18 hours

## Dependencies

- Microsoft.CodeAnalysis.CSharp (Roslyn SDK)
- Microsoft.CodeAnalysis.CSharp.Workspaces
- Microsoft.CodeAnalysis.Testing (for tests)

## Notes

- Consider making this part of a larger analyzer suite
- May want to create multiple analyzers for other R-CODE-xxx rules
- Keep analyzer lightweight for IDE performance
- Follow Roslyn analyzer best practices for performance
