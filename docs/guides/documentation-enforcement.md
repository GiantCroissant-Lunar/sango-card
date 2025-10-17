---
doc_id: DOC-2025-00115
title: Documentation Enforcement
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [documentation-enforcement]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00091
title: Documentation Enforcement Guide
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:

- documentation
- enforcement
- automation
- quality
summary: Automated tools and workflows for preventing scattered documentation and enforcing documentation standards.
supersedes: []
related:
- DOC-2025-00001
- DOC-2025-00002

---

# Documentation Enforcement Guide

Automated tools to prevent scattered documentation and enforce documentation standards.

## Problem

AI agents and developers often create markdown files outside canonical locations, leading to documentation sprawl. Manual checking is unreliable.

## Solution: Multi-Layer Automated Enforcement

### Layer 1: Pre-commit Hook (Local)

**Blocks scattered docs before commit**

The pre-commit hook runs automatically when you try to commit:

```bash
# Automatic - runs on git commit
git add scattered-doc.md
git commit -m "feat: add feature"
# ❌ BLOCKED: scattered-doc.md is not in canonical location
```

**Setup:**

```bash
# Install pre-commit framework
pip install pre-commit
# OR
uv tool install pre-commit

# Install hooks
task setup:hooks
# OR
pre-commit install
```

**Manual run:**

```bash
# Check all files
pre-commit run check-scattered-docs --all-files

# Check staged files only
pre-commit run check-scattered-docs
```

### Layer 2: GitHub Actions CI (Remote)

**Catches scattered docs in pull requests**

The CI workflow runs on every PR and push:

```yaml
# .github/workflows/docs-scattered-check.yml
✅ Checks all markdown files
✅ Comments on PR with fix instructions
✅ Blocks merge if scattered docs found
```

**View results:**

- Check "Actions" tab in GitHub
- PR will have ❌ or ✅ status check
- Automatic comment with fix instructions

### Layer 3: Manual Linter (Anytime)

**Run comprehensive checks anytime**

```bash
# Quick check
task docs:check

# Auto-fix (moves to docs/_inbox/)
task docs:check:fix

# Generate HTML report
task docs:check:report

# Validate front-matter
task docs:validate
```

**Or directly:**

```powershell
# Check for issues
.\scripts\check-docs.ps1

# Auto-fix scattered docs
.\scripts\check-docs.ps1 -Fix

# Generate HTML report
.\scripts\check-docs.ps1 -Report
```

## Allowed Documentation Locations

### ✅ Canonical Locations

**Root Level (Limited):**

- `README.md` - Project overview
- `AGENTS.md` - Agent configuration
- `CLAUDE.md` - Claude instructions
- `CODE_OF_CONDUCT.md` - Standard file
- `CONTRIBUTING.md` - Standard file

**Documentation System:**

- `docs/_inbox/` - New docs (temporary)
- `docs/guides/` - How-to guides
- `docs/specs/` - Specifications
- `docs/rfcs/` - RFCs
- `docs/adrs/` - Architecture decisions
- `docs/plans/` - Plans and milestones
- `docs/findings/` - Research and analysis
- `docs/archive/` - Obsolete docs

**Infrastructure:**

- `*/README.md` - Module documentation
- `*/LICENSE.md` - License files
- `*/CHANGELOG.md` - Change logs
- `build/preparation/**.md` - Unity package docs
- `projects/client/**.md` - Unity project docs
- `packages/**/dotnet~/**` - .NET tool docs

**Configuration:**

- `.agent/**/*.md` - Agent rules
- `.github/**/*.md` - GitHub templates
- `.specify/**/*.md` - Spec-kit files
- `.windsurf/**/*.md` - Windsurf rules
- `.gemini/**/*.md` - Gemini config

### ❌ Blocked Locations

Any markdown file **outside** the allowed locations will be blocked:

```
❌ /BUILD-PLAN.md
❌ /some-doc.md
❌ /infra/scattered-doc.md
❌ /build/nuke/build/API-DOCS.md
```

## How It Works

### Pre-commit Hook Logic

```powershell
# 1. Get staged .md files
$stagedFiles = git diff --cached --name-only *.md

# 2. Check against allowed patterns
foreach ($file in $stagedFiles) {
    if (NOT matches allowed pattern) {
        ❌ Block commit
        Show fix instructions
    }
}
```

### GitHub Actions Logic

```bash
# 1. Find all .md files
find . -name "*.md"

# 2. Check against allowed patterns
for file in *.md; do
    if ! matches_pattern "$file"; then
        ❌ Fail check
        Comment on PR
    fi
done
```

### Manual Linter Features

```powershell
# Check scattered docs
✓ Find all markdown files
✓ Match against allowed patterns
✓ Report violations

# Auto-fix mode (-Fix)
✓ Move scattered docs to docs/_inbox/
✓ Handle name conflicts
✓ Preserve original content

# Report mode (-Report)
✓ Generate HTML report
✓ Show registry stats
✓ List all issues
```

## Usage Examples

### Example 1: Commit Blocked

```bash
$ git add my-feature-doc.md
$ git commit -m "docs: add feature doc"

ERROR: Scattered documentation detected!

The following markdown files are in non-canonical locations:
  - my-feature-doc.md

Documentation must be in canonical locations:
  - New docs        → docs/_inbox/
  - Guides          → docs/guides/
  ...

To fix:
  1. Move files to proper location (usually docs/_inbox/)
  2. Add YAML front-matter (see docs/DOCUMENTATION-SCHEMA.md)
  3. Or remove from staging: git reset HEAD <file>
```

**Fix:**

```bash
# Move to inbox
git mv my-feature-doc.md docs/_inbox/

# Add front-matter (see DOCUMENTATION-SCHEMA.md)
# Then commit
git commit -m "docs: add feature doc"
✅ Success!
```

### Example 2: Auto-fix Scattered Docs

```bash
$ task docs:check

❌ Found 3 scattered documentation files:
  - BUILD-NOTES.md
  - infra/SETUP.md
  - packages/foo/DESIGN.md

💡 Run with -Fix to automatically move to docs/_inbox/

$ task docs:check:fix

🔧 Auto-fixing: Moving to docs/_inbox/

  Moved: BUILD-NOTES.md → docs/_inbox/BUILD-NOTES.md
  Moved: infra/SETUP.md → docs/_inbox/SETUP.md
  Moved: packages/foo/DESIGN.md → docs/_inbox/DESIGN.md

✅ Fixed! Files moved to docs/_inbox/
   Remember to add front-matter!
```

### Example 3: PR Check

**Pull Request #42:**

```
❌ Documentation - Scattered Docs Check failed

## ❌ Scattered Documentation Detected

This PR contains markdown files in non-canonical locations:
- build/API-NOTES.md

### 📚 Required Locations:
- **New docs** → `docs/_inbox/`
- **Guides** → `docs/guides/`
...

### 🔧 How to Fix:
1. Move scattered markdown files to `docs/_inbox/`
2. Add YAML front-matter (see schema)
3. Run `python scripts/docs_validate.py` locally
4. Push changes
```

## Integration with Development Workflow

### Daily Development

```bash
# 1. Create new documentation
echo "# My Guide" > docs/_inbox/my-guide.md

# 2. Add front-matter (required!)
# See: docs/DOCUMENTATION-SCHEMA.md

# 3. Pre-commit checks it automatically
git add docs/_inbox/my-guide.md
git commit -m "docs: add my guide"
✅ Passes!
```

### CI/CD Pipeline

```yaml
# Automatic in PRs
on:
  pull_request:
    paths: ['**.md']

jobs:
  check-scattered-docs:
    runs-on: ubuntu-latest
    steps:
      - name: Check for scattered docs
        run: # checks all .md files
      - name: Comment on PR
        if: failure()
        # posts fix instructions
```

### Manual Checks

```bash
# Before committing (comprehensive)
task docs:check

# Validate front-matter
task docs:validate

# Generate health report
task docs:check:report
# Opens: docs-health-report.html
```

## Front-Matter Validation

The pre-commit hook checks **location**.
The validator checks **front-matter**:

```bash
$ task docs:validate

Validating documentation...

Registry generated: docs/index/registry.json
   Total docs: 23
   By type: {'guide': 19, 'finding': 1, ...}

❌ Validation failed:

ERRORS (2):
  [ERROR] docs/guides/my-guide.md: Missing required field 'doc_id'
  [ERROR] docs/guides/my-guide.md: Invalid status 'draft' (must be: active, superseded, ...)

See: docs/DOCUMENTATION-SCHEMA.md
```

## Troubleshooting

### Pre-commit Hook Not Running

```bash
# Check installation
pre-commit --version

# Reinstall hooks
pre-commit install
pre-commit install --hook-type commit-msg

# Test manually
pre-commit run check-scattered-docs --all-files
```

### False Positives

If a file is blocked but should be allowed:

1. Check if it matches allowed patterns (see above)
2. If it's a legitimate exception, update patterns in:
   - `scripts/git-hooks/check-scattered-docs.ps1`
   - `.github/workflows/docs-scattered-check.yml`
   - `scripts/check-docs.ps1`

### Auto-fix Creates Duplicates

The `-Fix` mode handles name conflicts:

```
docs/_inbox/my-doc.md exists
→ Creates: docs/_inbox/my-doc-1.md
```

Manually resolve duplicates by checking `docs/index/registry.json`.

## Configuration Files

### Pre-commit Hook

**File:** `scripts/git-hooks/check-scattered-docs.ps1`

- Runs on commit
- PowerShell script
- Checks staged .md files only

### GitHub Actions

**File:** `.github/workflows/docs-scattered-check.yml`

- Runs on PR/push
- Bash script
- Checks all .md files
- Comments on PR

### Manual Linter

**File:** `scripts/check-docs.ps1`

- Run anytime
- PowerShell script
- Comprehensive checks
- Auto-fix capability
- HTML reports

### Allowed Patterns

All three tools use the **same patterns** for consistency:

```regex
^README\.md$
^docs/
^\.agent/
...
```

Update all three if you need to add exceptions.

## Best Practices

### ✅ DO

- **Always use docs/_inbox/ for new docs**
- **Run `task docs:check` before pushing**
- **Add front-matter immediately** (don't forget!)
- **Check the registry** before creating new docs
- **Use auto-fix for bulk cleanup** (`task docs:check:fix`)

### ❌ DON'T

- **Don't commit without pre-commit hooks** (always run `task setup:hooks`)
- **Don't bypass checks** (fix the issue instead)
- **Don't create docs outside canonical locations**
- **Don't ignore CI failures** (scattered docs block merge)

## Quick Command Reference

```bash
# Setup (one-time)
task setup:hooks

# Manual checks
task docs:check              # Check for scattered docs
task docs:check:fix          # Auto-fix scattered docs
task docs:check:report       # HTML report
task docs:validate           # Validate front-matter

# Pre-commit
pre-commit run check-scattered-docs --all-files

# Direct scripts
.\scripts\check-docs.ps1
.\scripts\check-docs.ps1 -Fix
.\scripts\check-docs.ps1 -Report
python scripts/docs_validate.py
```

## Summary

**Three layers of enforcement:**

1. **Pre-commit Hook** - Blocks at commit time (local)
2. **GitHub Actions** - Blocks at PR time (remote)
3. **Manual Linter** - Check anytime, auto-fix capability

**Result:**

- ✅ Zero scattered docs slip through
- ✅ Automated enforcement
- ✅ Clear fix instructions
- ✅ No manual checking needed
- ✅ Consistent documentation structure

**See Also:**

- [Documentation Schema](DOCUMENTATION-SCHEMA.md) - Front-matter requirements
- [Migration Guide](documentation-migration-guide.md) - Moving existing docs
- [Agent Rules](../.agent/base/40-documentation.md) - Rules for AI agents
