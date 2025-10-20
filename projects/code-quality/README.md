# Code Quality Project - One Type Per File Enforcement

This directory contains ReSharper/Rider settings and tooling to enforce the **one type per file** code quality rule for the Sango Card project.

## Overview

The code quality enforcement includes:

1. **ReSharper Settings** - IDE inspections and code style
2. **JetBrains CLI Tools** - Command-line inspection and cleanup
3. **Python Detection Script** - Find files violating the rule
4. **Task Commands** - Integrated workflow via Taskfile

## Files

- `code-quality.sln.DotSettings` - ReSharper settings for the solution
  - Marks `MultipleTypeMembers` as **ERROR**
  - Enforces explicit access modifiers
  - Configures code style and naming conventions

- `code-quality.sln.DotSettings.xml` - Cleanup profile for `jb cleanupcode`
  - Auto-fixes formatting, usings, modifiers
  - **Note:** Cannot automatically split multiple types (manual IDE action required)

## Usage

### Detection (Find Violations)

**Using Python script (recommended):**

```bash
# Scan current directory (console output)
task lint:dotnet:find-multiple-types

# Scan specific directory
task lint:dotnet:find-multiple-types PATH=projects/code-quality

# Generate markdown report
task lint:dotnet:find-multiple-types FORMAT=markdown OUTPUT=output/violations.md

# Code-quality project quick check
task lint:dotnet:find-multiple-types:code-quality
```

**Using JetBrains InspectCode:**

```bash
# Run inspection on code-quality project
task lint:dotnet:inspectcode:code-quality

# Check XML output
cat output/code-quality-inspect.xml
```

### Cleanup (Auto-fix What's Possible)

**Auto-fix formatting, usings, modifiers:**

```bash
# Run cleanupcode on code-quality project
task lint:dotnet:cleanupcode:code-quality

# Or on any solution
task lint:dotnet:cleanupcode SOLUTION=dotnet/SangoCard.sln
```

**⚠️ Important:** `cleanupcode` **CANNOT** automatically split multiple types into separate files. This must be done manually in Rider.

### Manual Fix (Splitting Types)

**In JetBrains Rider:**

1. Open file with multiple types
2. Place cursor on a type name (e.g., class name)
3. Press **Alt+Enter** (Windows/Linux) or **Option+Enter** (macOS)
4. Select **"Move to separate file"**
5. Rider will create a new file with the type
6. Repeat until only one type remains in original file

**Recommended workflow:**

```bash
# 1. Find violations
task lint:dotnet:find-multiple-types:code-quality

# 2. Open report
cat output/code-quality-multiple-types.md

# 3. Fix each file in Rider using Alt+Enter

# 4. Run cleanup on fixed files
task lint:dotnet:cleanupcode:code-quality

# 5. Verify no violations remain
task lint:dotnet:find-multiple-types:code-quality
```

## What Gets Detected

The scanner detects top-level types:

- ✅ `class` declarations
- ✅ `interface` declarations
- ✅ `struct` declarations
- ✅ `enum` declarations
- ✅ `delegate` declarations
- ✅ `record` declarations

**Excluded:**

- ❌ Nested types (types inside other types)
- ❌ Files in `obj/`, `bin/`, `Library/`, `Temp/`, `.git/`

## Integration with CI/CD

Add to your CI pipeline:

```bash
# Exit with error if violations found
python scripts/find_multiple_types.py projects/code-quality --errors-only
```

Or via Task:

```bash
task lint:dotnet:find-multiple-types PATH=projects/code-quality
```

## ReSharper Settings Details

### Code Inspections (ERROR level)

- `MultipleTypeMembers` - File contains multiple types

### Code Style

- Explicit access modifiers required (`public`, `private`, etc.)
- 120-character line wrap limit
- Using directives optimized and sorted
- System namespaces placed first

### Naming Conventions

- Private fields: `camelCase`
- Public/protected fields: `PascalCase`
- Constants: `PascalCase`

## Command Reference

| Command | Description |
|---------|-------------|
| `task lint:dotnet:find-multiple-types` | Find violations (console) |
| `task lint:dotnet:find-multiple-types:code-quality` | Find violations (markdown report) |
| `task lint:dotnet:inspectcode:code-quality` | Run JetBrains InspectCode |
| `task lint:dotnet:cleanupcode:code-quality` | Auto-fix formatting/usings |

## Tools Required

- **Python 3.8+** - For detection script
- **JetBrains CLI** (`jb`) - For inspectcode/cleanupcode
  - Install: `dotnet tool install -g JetBrains.ReSharper.GlobalTools`
  - Verify: `jb inspectcode --version`

## Why One Type Per File?

**Benefits:**

1. **Better version control** - Smaller, focused diffs
2. **Easier navigation** - File name matches type name
3. **Reduced merge conflicts** - Less chance of simultaneous edits
4. **IDE performance** - Faster file search and indexing
5. **Code review** - Clearer, more focused reviews

**Industry standard:**

- Recommended by Microsoft C# coding conventions
- Default behavior of Rider's "Create Type" action
- Enforced by many static analysis tools

## Limitations

**What can be automated:**

- ✅ Detection of violations
- ✅ Reporting (console, JSON, markdown)
- ✅ Code style cleanup (formatting, usings)
- ✅ CI/CD integration

**What requires manual action:**

- ❌ Splitting types into separate files
  - **Reason:** This is a structural refactoring requiring semantic understanding
  - **Workaround:** Use Rider's Alt+Enter quick-fix

## Troubleshooting

**Script doesn't detect types:**

- Check file encoding (should be UTF-8)
- Verify types are top-level (not nested)
- Check for unusual syntax patterns

**InspectCode doesn't report violations:**

- Ensure `.DotSettings` file exists in solution directory
- Verify `--settings` parameter points to correct file
- Check SEVERITY level (should be ERROR or WARNING)

**CleanupCode doesn't split types:**

- This is expected - use Rider IDE instead
- CleanupCode only handles formatting and simple refactorings

## Future Enhancements

Potential improvements:

- [ ] Auto-splitter using Roslyn (complex, risky)
- [ ] Pre-commit hook integration
- [ ] VS Code extension integration
- [ ] Batch processing script for Rider CLI
- [ ] Integration with `dotnet format`

## References

- [JetBrains InspectCode Documentation](https://www.jetbrains.com/help/resharper/InspectCode.html)
- [JetBrains CleanupCode Documentation](https://www.jetbrains.com/help/resharper/CleanupCode.html)
- [ReSharper Settings Layers](https://www.jetbrains.com/help/resharper/Sharing_Configuration_Options.html)
- [C# Coding Conventions (Microsoft)](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
