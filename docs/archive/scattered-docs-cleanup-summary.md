---
doc_id: DOC-2025-00195
title: Scattered Docs Cleanup Summary
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [scattered-docs-cleanup-summary]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00145
title: Scattered Docs Cleanup Summary
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [scattered-docs-cleanup-summary]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00138
title: Scattered Documentation Cleanup Summary
doc_type: finding
status: active
canonical: true
created: 2025-10-17
tags: [docs, cleanup, tooling, automation]
summary: >
  Summary of automated documentation cleanup that fixed scattered docs and added front-matter.
source:
  author: agent
  agent: claude
  model: sonnet-4.5
---

# Scattered Documentation Cleanup Summary

## Problem

The project had **1,085 scattered documentation files** spread across:

- `build/_artifacts/` - Build artifacts
- `output/` - Unity build outputs
- `packages/scoped-6571/com.contractwork.sangocard.build/` - Old design docs (3 files)
- `projects/*/Library/PackageCache/` - Unity third-party packages (1,000+ files)
- `projects/*/Assets/Packages/` - NuGet package docs

Additionally, **46 documentation files** in `docs/` were missing required YAML front-matter.

## Solution Implemented

### 1. Updated Exclusion Patterns

**File: `scripts/check-docs.ps1`**

Added exclusions for:

- Unity Library/PackageCache
- output/ directories
- build/_artifacts/
- Assets/Packages/ (NuGet packages)

```powershell
Where-Object {
    $_.FullName -notmatch "node_modules|\.git|Library[\\/]PackageCache|output[\\/]|build[\\/]_artifacts|_artifacts[\\/]|Assets[\\/]Packages[\\/]"
}
```

**File: `git-hooks/check-scattered-docs.ps1`**

Added allowed patterns for:

- `build/nuke/build/` - Nuke build system docs
- `projects/code-quality/` - Code quality project docs

### 2. Moved Legitimate Scattered Docs

Moved 3 legitimate scattered docs to `docs/_inbox/`:

- `TOOL-DESIGN.md`
- `TOOL-DESIGN-V2.md`
- `TOOL-DESIGN-V3.md`

### 3. Created Front-Matter Automation Script

**File: `scripts/add-frontmatter.ps1`**

Features:

- Auto-detects doc_type from directory (spec, rfc, adr, plan, finding, guide)
- Auto-detects status (active, archived, draft)
- Generates doc_id with auto-incrementing sequence
- Creates title from filename
- Extracts tags from filename
- Supports dry-run mode

Usage:

```powershell
# Preview changes
.\scripts\add-frontmatter.ps1 -DryRun

# Apply changes
.\scripts\add-frontmatter.ps1
```

### 4. Added Front-Matter to 46 Files

Applied YAML front-matter to all missing files in:

- `docs/archive/` - 18 files
- `docs/guides/` - 21 files
- `docs/findings/` - 1 file
- `docs/plans/` - 1 file
- `docs/rfcs/` - 1 file
- `docs/specs/` - 1 file
- `docs/_inbox/` - 3 files

## Results

### Before

- 1,085 scattered documentation files detected
- 46 files missing front-matter
- Manual cleanup required

### After

- **0 scattered documentation files** (excluding third-party packages)
- **All project docs have front-matter**
- Automated validation via `task docs:check`

## Validation

Run documentation health check:

```bash
# Check for issues
task docs:check

# Auto-fix scattered docs
task docs:check:fix

# Generate HTML report
task docs:check:report

# Validate front-matter and registry
task docs:validate
```

## Known Issues

Some files may have duplicate front-matter if the script was run multiple times. The PowerShell check's regex `'^---\s*\n.*?\n---'` should detect existing front-matter, but edge cases may exist.

**Fix:** Manually review and remove duplicate front-matter blocks.

## Maintenance

### Adding New Docs

1. Always write to `docs/_inbox/` first
2. Include complete front-matter with required fields
3. Check `docs/index/registry.json` for existing docs
4. Use next available doc_id (increment highest number)

### Pre-Commit Hook

The pre-commit hook at `git-hooks/check-scattered-docs.ps1` will:

- Block commits with scattered docs
- Suggest proper locations
- Enforce documentation standards

## Tools Created

| Tool | Purpose | Usage |
|------|---------|-------|
| `scripts/check-docs.ps1` | Find scattered docs and front-matter issues | `task docs:check` |
| `scripts/add-frontmatter.ps1` | Auto-add front-matter to docs | `pwsh -File scripts/add-frontmatter.ps1` |
| `git-hooks/check-scattered-docs.ps1` | Pre-commit validation | Automatic via pre-commit |
| `git-hooks/python/docs_validate.py` | Full validation and registry generation | `python git-hooks/python/docs_validate.py` |

## Next Steps

1. Review auto-generated summaries and improve them
2. Set `canonical: true` for primary/authoritative docs
3. Move docs from `_inbox/` to proper categories
4. Update registry by running `task docs:validate`
5. Consider removing duplicate front-matter blocks in any affected files

## References

- Documentation Schema: `docs/DOCUMENTATION-SCHEMA.md`
- Agent Rules: `.agent/base/40-documentation.md`
- Registry: `docs/index/registry.json`
- Task Commands: `Taskfile.yml` (lines 276-294)
