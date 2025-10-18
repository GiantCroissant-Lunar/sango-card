# GitHub Automation Scripts

Scripts for automating GitHub operations, organized by language.

## Structure

```text
.github/scripts/
├── README.md
├── powershell/          (PowerShell scripts)
│   ├── create-issues.ps1
│   └── generate-issues.ps1
├── python/              (Python scripts - reserved)
└── nodejs/              (Node.js scripts - reserved)
```

## PowerShell Scripts

### create-issues.ps1

**Location**: `powershell/create-issues.ps1`

Creates GitHub issues from generated issue files.

**Requirements**:

- GitHub CLI (`gh`) installed and authenticated

**Usage**:

```powershell
# Dry run - preview what would be created
.\.github\scripts\powershell\create-issues.ps1 -DryRun

# Create issues from .github/issues/ directory
.\.github\scripts\powershell\create-issues.ps1

# Specify repository
.\.github\scripts\powershell\create-issues.ps1 -Repository "owner/repo"

# Add to milestone
.\.github\scripts\powershell\create-issues.ps1 -Milestone "v1.0"
```

**Parameters**:

- `-IssueDir` - Directory containing issue files (default: `.github/issues`)
- `-Repository` - Target repository (e.g., "owner/repo")
- `-DryRun` - Preview without creating issues
- `-Milestone` - Add issues to milestone
- `-Project` - Add issues to project

### generate-issues.ps1

**Location**: `powershell/generate-issues.ps1`

Generates GitHub issue files from spec-kit task markdown.

**Usage**:

```powershell
# Dry run - preview generated issues
.\.github\scripts\powershell\generate-issues.ps1 -TaskFile path/to/task.md -DryRun

# Generate issue files
.\.github\scripts\powershell\generate-issues.ps1 -TaskFile path/to/task.md

# Custom output directory
.\.github\scripts\powershell\generate-issues.ps1 -TaskFile path/to/task.md -OutputDir custom/path
```

**Parameters**:

- `-TaskFile` - Path to spec-kit task markdown file (required)
- `-OutputDir` - Output directory for issue files (default: `.github/issues`)
- `-DryRun` - Preview without creating files

## Workflow

1. **Generate issues** from spec-kit tasks:

   ```powershell
   .\.github\scripts\powershell\generate-issues.ps1 -TaskFile .specify/tasks/my-task.md
   ```

2. **Review** generated issue files in `.github/issues/`

3. **Create issues** on GitHub:

   ```powershell
   .\.github\scripts\powershell\create-issues.ps1
   ```

## Integration with Spec-Kit

These scripts support the spec-driven development workflow:

- Extract tasks from `.specify/tasks/` markdown files
- Generate structured issue files
- Bulk create issues on GitHub
- Link to original spec-kit tasks

## References

- GitHub CLI: <https://cli.github.com/>
- Spec-Kit: `.specify/README.md`
