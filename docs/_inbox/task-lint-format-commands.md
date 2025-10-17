---
doc_id: DOC-2025-00139
title: Task Lint and Format Commands Reference
doc_type: guide
status: active
canonical: true
created: 2025-10-17
tags: [task, lint, format, pre-commit, tooling]
summary: >
  Quick reference for task commands that run linting and formatting (mirrors pre-commit hooks).
source:
  author: agent
  agent: claude
  model: sonnet-4.5
---

# Task Lint and Format Commands Reference

## Overview

The project uses `task` commands to run all linting and formatting operations. These commands mirror what the pre-commit hooks do, allowing you to check and fix issues before committing.

## Quick Reference

### Linting Commands

```bash
# Run ALL linters (same as pre-commit hooks)
task lint

# Run only on staged files
task lint:staged

# Lint specific file types
task lint:markdown        # Lint markdown files
task lint:yaml           # Lint YAML files
task lint:secrets        # Check for secrets/sensitive data
task lint:csharp         # Check C# partial class patterns (R-CODE-090)
```

### Formatting Commands

```bash
# Auto-fix ALL formatting issues
task format

# Format specific file types
task format:markdown     # Auto-fix markdown formatting
task format:yaml        # Auto-format YAML files
```

### All-in-One Check

```bash
# Run all checks (lint + docs validation)
task check
```

## Detailed Usage

### `task lint` / `task lint:all`

Runs all pre-commit hooks on all files in the repository.

**What it checks:**
- Trailing whitespace
- End-of-file issues
- YAML syntax
- JSON syntax
- Large files (>2MB)
- Merge conflicts
- Private keys / secrets
- Markdown formatting
- EditorConfig compliance
- C# partial class patterns
- Documentation structure
- And more...

**Usage:**
```bash
task lint
```

**Exit codes:**
- `0` - All checks passed
- `1` - One or more checks failed

---

### `task lint:staged`

Runs pre-commit hooks only on staged files (what you're about to commit).

**When to use:**
- Quick check before committing
- Faster than checking all files
- Mirrors what pre-commit hooks will do

**Usage:**
```bash
git add .
task lint:staged
```

---

### `task lint:markdown`

Lints markdown files using markdownlint.

**What it checks:**
- Heading styles
- List formatting
- Line length
- Trailing spaces
- And more...

**Config:** `.markdownlint.json`

**Usage:**
```bash
task lint:markdown
```

---

### `task lint:yaml`

Lints YAML files for syntax and style issues.

**What it checks:**
- Valid YAML syntax
- Indentation (2 spaces)
- Line length
- Trailing spaces
- Document start markers

**Config:** `.yamllint.yaml`

**Usage:**
```bash
task lint:yaml
```

---

### `task lint:secrets`

Scans for secrets, credentials, and sensitive data.

**What it detects:**
- API keys
- Private keys
- Passwords
- Access tokens
- AWS credentials
- And more...

**Tools used:**
- `detect-secrets`
- `gitleaks`

**Usage:**
```bash
task lint:secrets
```

**Note:** This will NOT check encrypted files (`.encrypted`, `.sops.json`)

---

### `task lint:csharp`

Validates C# partial class patterns (rule R-CODE-090).

**What it checks:**
- Multi-interface classes must use partial classes
- Each interface implementation in separate file
- Proper file naming conventions

**Usage:**
```bash
task lint:csharp
```

---

### `task format` / `task format:all`

Auto-fixes all formatting issues.

**What it fixes:**
- Trailing whitespace
- Missing newlines at end of files
- Mixed line endings (CRLF/LF)
- Markdown formatting
- YAML formatting

**Usage:**
```bash
task format
```

**Safe to run:** Yes - changes are auto-fixable and reversible with git

---

### `task format:markdown`

Auto-fixes markdown formatting issues.

**What it fixes:**
- Heading styles
- List indentation
- Code block formatting
- Trailing spaces

**Usage:**
```bash
task format:markdown
```

---

### `task format:yaml`

Auto-formats YAML files according to `.yamllint.yaml` config.

**What it fixes:**
- Indentation (2 spaces)
- Line length
- Trailing spaces
- Consistent quoting

**Usage:**
```bash
task format:yaml
```

---

### `task check`

Runs all checks (linting + documentation validation).

**What it does:**
1. Runs all linters (`task lint:all`)
2. Checks documentation structure (`task docs:check`)
3. Reports success or failure

**Usage:**
```bash
task check
```

**When to use:**
- Before creating a pull request
- After making significant changes
- In CI/CD pipeline
- Before committing (more thorough than `lint:staged`)

---

## Workflow Examples

### Before Committing

```bash
# Quick check of staged files
git add .
task lint:staged

# Fix any issues
task format

# Check again
task lint:staged

# Commit
git commit -m "feat: add new feature"
```

---

### Before Creating PR

```bash
# Run full check suite
task check

# If issues found, fix them
task format
task docs:check:fix

# Run check again
task check

# Push
git push
```

---

### After Pulling Changes

```bash
# Check for any formatting issues introduced
task lint

# Auto-fix if needed
task format
```

---

### Weekly Maintenance

```bash
# Run comprehensive check
task check

# Update pre-commit hooks
pre-commit autoupdate

# Test updated hooks
task lint
```

---

## Relationship to Pre-Commit Hooks

| Task Command | Pre-Commit Hook Equivalent | Notes |
|--------------|---------------------------|-------|
| `task lint` | `pre-commit run --all-files` | Runs all hooks |
| `task lint:staged` | `pre-commit run` | Runs on staged files only |
| `task lint:markdown` | `pre-commit run markdownlint --all-files` | Markdown only |
| `task lint:yaml` | `pre-commit run yamllint --all-files` | YAML only |
| `task lint:secrets` | `pre-commit run detect-secrets --all-files` | Secrets detection |
| `task format` | Auto-fix hooks | Multiple formatters |
| `task check` | Custom - lint + docs | Comprehensive check |

---

## Configuration Files

| File | Purpose |
|------|---------|
| `.pre-commit-config.yaml` | Pre-commit hooks configuration |
| `.markdownlint.json` | Markdown linting rules |
| `.yamllint.yaml` | YAML linting rules |
| `.editorconfig` | Editor formatting rules |
| `.gitleaks.toml` | Secret scanning rules |
| `.secrets.baseline` | Known secrets baseline |

---

## Troubleshooting

### "pre-commit not found"

Install pre-commit:
```bash
pip install pre-commit
# OR
uv tool install pre-commit

# Then setup hooks
task setup:hooks
```

---

### Lint fails but no errors shown

Some hooks may fail silently. Run with verbose output:
```bash
pre-commit run --all-files --verbose
```

---

### Format doesn't fix all issues

Some issues require manual fixing:
- Complex C# pattern violations
- Documentation structure issues
- Secrets that need to be removed

Run `task lint` to see remaining issues.

---

### Hooks are slow

Run specific checks instead of all:
```bash
task lint:markdown  # Faster than task lint
```

Or run only on staged files:
```bash
task lint:staged
```

---

## Best Practices

1. **Before Every Commit:**
   ```bash
   task lint:staged
   ```

2. **Before Every PR:**
   ```bash
   task check
   ```

3. **After Making Changes:**
   ```bash
   task format
   ```

4. **Weekly:**
   ```bash
   task lint        # Full check
   ```

5. **Use Auto-Fix:**
   - Always try `task format` before manually fixing
   - Only manually fix what auto-fix can't handle

6. **Keep Hooks Updated:**
   ```bash
   pre-commit autoupdate
   task lint  # Test updated hooks
   ```

---

## See Also

- Pre-commit hooks guide: `docs/guides/pre-commit.md`
- Task runner guide: `docs/guides/task-runner.md`
- Documentation health: `task docs:check`
- Project rules: `.agent/base/20-rules.md`

---

## Quick Command Cheat Sheet

```bash
# Most Common
task lint:staged        # Before commit
task format            # Fix formatting
task check             # Before PR

# Specific Checks
task lint:markdown     # Markdown only
task lint:yaml         # YAML only
task lint:secrets      # Security check

# Documentation
task docs:check        # Check docs structure
task docs:check:fix    # Auto-fix docs

# Full Suite
task lint              # All linters
task check             # Everything
```
