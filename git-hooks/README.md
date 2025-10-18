# Git Hooks

Git hooks for enforcing code quality and project conventions.

## Quick Start

```bash
# Option 1: Use task commands (recommended)
task setup:dotnet-tools          # Install dotnet format, Roslynator, InspectCode
task setup:hooks                 # Install pre-commit hooks
task lint:dotnet                 # Run all .NET code quality checks
task format:dotnet               # Auto-fix C# formatting

# Option 2: Manual installation
dotnet tool install -g roslynator.dotnet.cli
dotnet tool install -g JetBrains.ReSharper.GlobalTools  # Optional
pre-commit install

# Run all hooks
pre-commit run --all-files

# Run specific hook
task lint:dotnet:format          # Check formatting
task lint:dotnet:roslynator      # Run Roslynator
task lint:dotnet:inspectcode     # Run InspectCode

# Or use pre-commit directly
pre-commit run dotnet-format --all-files
pre-commit run roslynator --all-files

# Skip hooks for a commit
SKIP=inspectcode git commit -m "message"
```

## Available Hooks

### .NET Code Quality Hooks

#### dotnet format

**Script**: `dotnet/dotnet-format.sh`

Automatically formats C# code using `dotnet format` to ensure consistent code style.

**What it does**:

- Runs on all staged `.cs` files
- Verifies code formatting compliance
- Auto-fixes formatting issues and re-stages files
- Uses the solution or project file configuration

**Requirements**: .NET SDK with `dotnet format` command

**Usage**:

```bash
# Run manually on all files
dotnet format

# Run via pre-commit
pre-commit run dotnet-format --all-files
```

#### Roslynator Analysis

**Script**: `dotnet/roslynator.sh`

Analyzes C# code using Roslynator to detect code quality issues and suggest improvements.

**What it checks**:

- Code style violations
- Potential bugs and code smells
- Performance improvements
- Best practice violations

**Requirements**:

```bash
dotnet tool install -g roslynator.dotnet.cli
```

**Usage**:

```bash
# Install Roslynator CLI
dotnet tool install -g roslynator.dotnet.cli

# Run manually
roslynator analyze YourSolution.sln

# Run via pre-commit
pre-commit run roslynator --all-files
```

#### JetBrains InspectCode

**Script**: `dotnet/inspectcode.sh`

Runs JetBrains ReSharper command-line code inspection to detect code quality issues.

**What it checks**:

- ReSharper code inspections (1000+ built-in inspections)
- Code quality issues
- Best practices violations
- Configurable severity levels (ERROR, WARNING, SUGGESTION, HINT)

**Requirements**:

Requires .NET Core 3.1.0 or later. Install as a .NET global tool:

```bash
dotnet tool install -g JetBrains.ReSharper.GlobalTools
```

After installation, the `jb` command will be available with `inspectcode` as a subcommand.

**Usage**:

```bash
# Install JetBrains ReSharper Global Tools (version 2025.2.3+)
dotnet tool install -g JetBrains.ReSharper.GlobalTools

# Verify installation
jb inspectcode --version

# Run manually on solution
jb inspectcode YourSolution.sln

# Run with options
jb inspectcode YourSolution.sln --output=report.xml --severity=WARNING

# Run via pre-commit
pre-commit run inspectcode --all-files
```

**Configuration**: Add `.DotSettings` files to customize inspection rules:

- `YourSolution.sln.DotSettings` - Solution-wide settings
- `YourProject.csproj.DotSettings` - Project-specific settings
- Layer settings for team-shared or personal preferences

**Note**: ReSharper command line tools share the `jb` command, which provides both `inspectcode` and `cleanupcode` functionality.

### Partial Class Interface Separation (R-CODE-090)

**Script**: `python/check_partial_class_pattern.py`

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

**Installing .NET Tool Dependencies**:

The .NET hooks require additional tools to be installed:

```bash
# dotnet format (included with .NET SDK - no extra install needed)

# Roslynator CLI
dotnet tool install -g roslynator.dotnet.cli

# JetBrains ReSharper Global Tools (optional - can disable hook if not needed)
dotnet tool install -g JetBrains.ReSharper.GlobalTools

# Verify installations
dotnet format --version
roslynator --version
jb inspectcode --version
```

If you prefer not to install all tools, you can disable specific hooks in `.pre-commit-config.yaml` by commenting them out.

### Alternative: Direct Hook Installation

Not recommended. Use the pre-commit framework instead (see above).

## Bypassing Hooks

If you need to commit code that violates the pattern (e.g., work in progress):

```bash
git commit --no-verify
```

⚠️ **Warning**: Bypassing hooks should be rare. The pattern violations will eventually need to be fixed.

## Testing Hooks

### With task commands (recommended)

```bash
# Install tools
task setup:dotnet-tools

# Run all .NET code quality checks
task lint:dotnet

# Run specific checks
task lint:dotnet:format         # dotnet format
task lint:dotnet:roslynator     # Roslynator analysis
task lint:dotnet:inspectcode    # JetBrains InspectCode

# Auto-fix formatting
task format:dotnet

# Run all pre-commit hooks
task lint:all

# Run only staged files
task lint:staged
```

### With pre-commit framework

```bash
# Run all hooks on all files
pre-commit run --all-files

# Run specific hook
pre-commit run dotnet-format --all-files
pre-commit run roslynator --all-files
pre-commit run inspectcode --all-files
pre-commit run partial-class-interface-separation --all-files

# Run on staged files only (normal pre-commit behavior)
pre-commit run
```

### Direct script execution

```bash
# .NET tools (direct)
dotnet format YourSolution.sln --verify-no-changes
roslynator analyze YourSolution.sln
jb inspectcode YourSolution.sln

# Python hooks (direct)
python git-hooks/python/check_partial_class_pattern.py
python git-hooks/python/check_scattered_docs.py
python git-hooks/python/docs_validate.py
```

## Debugging

If a hook fails unexpectedly:

1. Run the hook script directly with `-Verbose` flag
2. Check the hook output for specific violations
3. Review `docs/CODING-PATTERNS.md` for pattern details
4. If the hook is incorrect, file an issue

## Configuration

### Enabling/Disabling Hooks

You can enable or disable specific hooks by editing `.pre-commit-config.yaml`:

```yaml
# To disable a hook temporarily, comment it out
# - id: roslynator
#   name: Roslynator Analysis
#   ...

# Or set SKIP environment variable
SKIP=roslynator,inspectcode git commit -m "message"
```

### Customizing .NET Tools

#### dotnet format

Create or modify `.editorconfig` in your solution root to customize formatting rules:

```ini
[*.cs]
indent_size = 4
insert_final_newline = true
```

#### Roslynator

Configure via `.roslynator.config` or project file:

```xml
<PropertyGroup>
  <RoslynatorAnalyzersEnabled>true</RoslynatorAnalyzersEnabled>
</PropertyGroup>
```

#### JetBrains InspectCode

Add `.DotSettings` files to your solution:

- `YourSolution.sln.DotSettings` - Solution-wide settings
- `YourProject.csproj.DotSettings` - Project-specific settings

## Future Enhancements

- [x] **dotnet format**: Code formatting enforcement
- [x] **Roslynator**: Code quality analysis
- [x] **JetBrains InspectCode**: ReSharper-based inspections
- [ ] **Roslyn Analyzer**: Compile-time enforcement (analyzer ID: SANGO1001)
- [ ] **Auto-fix**: Code action to automatically split files
- [x] **commit-msg hook**: Enforce conventional commit format (implemented)
- [ ] **pre-push hook**: Run quick tests before push

## References

- **Rule**: R-CODE-090 in `.agent/base/20-rules.md`
- **Documentation**: `docs/CODING-PATTERNS.md`
- **Example**: `build/nuke/build/Build.cs` + `Build.UnityBuild.cs`
