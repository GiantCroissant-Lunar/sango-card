---
doc_id: DOC-2025-00067
title: Pre-Commit Hooks Guide
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:
- pre-commit
- git
- hooks
summary: Guide for setting up and using pre-commit hooks.
---

# Pre-commit Hooks

This project uses [pre-commit](https://pre-commit.com/) to manage git hooks for code quality, consistency, and security.

## What is Pre-commit?

Pre-commit is a framework for managing and maintaining multi-language git hooks. It runs configured checks before you commit code, preventing issues from entering the repository.

## Installation

### 1. Install Pre-commit

Choose one of these methods:

**Using pip**:

```bash
pip install pre-commit
```

**Using uv (recommended for this project)**:

```bash
uv tool install pre-commit
```

**Using package managers**:

```bash
# macOS
brew install pre-commit

# Windows (Chocolatey)
choco install pre-commit

# Windows (Scoop)
scoop install pre-commit
```

### 2. Install Hooks

Run the setup task (recommended):

```bash
task setup
```

Or install hooks manually:

```bash
pre-commit install
pre-commit install --hook-type commit-msg
```

This creates git hooks in `.git/hooks/` that run automatically on commit.

## Configured Hooks

The `.pre-commit-config.yaml` configures the following hooks:

### File Quality Checks

- ✅ **trailing-whitespace**: Remove trailing whitespace
- ✅ **end-of-file-fixer**: Ensure files end with newline
- ✅ **check-yaml**: Validate YAML syntax
- ✅ **check-json**: Validate JSON syntax
- ✅ **check-added-large-files**: Prevent large files (>2MB)
- ✅ **check-merge-conflict**: Detect merge conflict markers
- ✅ **check-case-conflict**: Detect case conflicts
- ✅ **mixed-line-ending**: Enforce LF line endings (except Windows scripts)
- ✅ **detect-private-key**: Prevent committing private keys

### Security

- 🔒 **detect-secrets**: Scan for secrets and credentials
  - Uses `.secrets.baseline` for known false positives
- 🔐 **gitleaks**: Advanced secret scanning with TOML configuration
  - Configuration: `.gitleaks.toml`
  - Detects AWS keys, API tokens, passwords, etc.

### Code Quality

- 📝 **markdownlint**: Lint and format Markdown files
  - Configuration: `.markdownlint.yaml`
- 📋 **yamllint**: Lint YAML files for syntax and style
  - Configuration: `.yamllint.yaml`
- 📄 **yamlfmt**: Format YAML files
- ⚙️ **editorconfig-checker**: Enforce EditorConfig rules

### Project-Specific

- 🔧 **partial-class-interface-separation**: Validate R-CODE-090 pattern
  - Runs `scripts/git-hooks/pre-commit.ps1`
  - Checks C# partial class organization
  - Skipped in CI (PowerShell dependency)

### Commit Messages

- 📨 **conventional-pre-commit**: Enforce Conventional Commits format
  - Format: `type(scope): description`
  - Examples: `feat(auth): add login`, `fix(build): resolve deps`
  - Runs on `commit-msg` hook

## Usage

### Automatic (Recommended)

Hooks run automatically when you commit:

```bash
git add .
git commit -m "feat(api): add user endpoint"
```

If any hook fails:

1. Review the error messages
2. Fix the issues
3. Stage the fixes: `git add .`
4. Commit again

### Manual Execution

Run all hooks on all files:

```bash
pre-commit run --all-files
```

Run specific hook:

```bash
pre-commit run trailing-whitespace --all-files
pre-commit run partial-class-interface-separation --all-files
```

Run on staged files only:

```bash
pre-commit run
```

### Bypassing Hooks

⚠️ **Only for emergencies or WIP commits**:

```bash
git commit --no-verify -m "wip: work in progress"
```

**Note**: Bypassed checks must be fixed before merging to main.

## Configuration Files

| File | Purpose |
|------|---------|
| `.pre-commit-config.yaml` | Main pre-commit configuration |
| `.secrets.baseline` | Baseline for detect-secrets (known false positives) |
| `.gitleaks.toml` | Gitleaks configuration and allowlist |
| `.markdownlint.yaml` | Markdown linting rules |
| `.yamllint.yaml` | YAML linting rules |
| `.editorconfig` | Editor configuration rules |

## Updating Hooks

Pre-commit hooks are versioned. Update to latest versions:

```bash
pre-commit autoupdate
```

This updates the `rev` fields in `.pre-commit-config.yaml`.

## Troubleshooting

### Hook Failed: "Command not found"

Some hooks require specific tools to be installed:

- **pre-commit**: `uv tool install pre-commit`
- **yamllint**: `uv tool install yamllint`
- **gitleaks**: Install via package manager or from <https://github.com/gitleaks/gitleaks>
- **markdownlint**: `npm install -g markdownlint-cli`
- **yamlfmt**: `go install github.com/google/yamlfmt/cmd/yamlfmt@latest`

Most hooks auto-install their dependencies via pre-commit.

### Hook is Slow

First run downloads and caches dependencies. Subsequent runs are fast.

To manually install environments:

```bash
pre-commit install-hooks
```

### Skip Specific Hook

Add to commit message:

```bash
git commit -m "feat: my change" --no-verify
```

Or skip specific hook:

```bash
SKIP=markdownlint git commit -m "feat: my change"
```

### PowerShell Hook Fails on Non-Windows

The `partial-class-interface-separation` hook requires PowerShell. On macOS/Linux:

1. Install PowerShell: <https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell>
2. Or skip the hook: `SKIP=partial-class-interface-separation git commit -m "..."`

The hook is automatically skipped in CI environments.

## CI Integration

Pre-commit hooks run in CI automatically via `.pre-commit-config.yaml`:

```yaml
ci:
  autofix_commit_msg: |
    chore: auto-fixes from pre-commit hooks
  autofix_prs: true
  autoupdate_schedule: quarterly
```

To run locally as CI would:

```bash
pre-commit run --all-files --show-diff-on-failure
```

## Adding New Hooks

1. Edit `.pre-commit-config.yaml`
2. Add new hook under appropriate repo
3. Test: `pre-commit run <hook-id> --all-files`
4. Document in this file

### Example: Adding a New Hook

```yaml
- repo: https://github.com/username/repo
  rev: v1.0.0
  hooks:
    - id: my-hook
      name: My Custom Hook
      entry: path/to/script.sh
      language: system
      files: '\.(ext)$'
      description: What this hook does
```

## Best Practices

1. ✅ **Run hooks before pushing**: `pre-commit run --all-files`
2. ✅ **Keep hooks updated**: `pre-commit autoupdate` monthly
3. ✅ **Fix issues immediately**: Don't bypass hooks unless absolutely necessary
4. ✅ **Add baseline for false positives**: Update `.secrets.baseline` for known safe patterns
5. ✅ **Test new hooks**: Run on all files before committing config changes

## References

- [Pre-commit Documentation](https://pre-commit.com/)
- [Supported Hooks](https://pre-commit.com/hooks.html)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Detect Secrets](https://github.com/Yelp/detect-secrets)
- **R-CODE-090**: Partial class interface separation pattern
- **Scripts**: `scripts/git-hooks/`
