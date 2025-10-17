---
doc_id: DOC-2025-00193
title: Fix Docs Validate Infinite Loop
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [fix-docs-validate-infinite-loop]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00143
title: Fix Docs Validate Infinite Loop
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [fix-docs-validate-infinite-loop]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00140
title: Fix docs-validate Pre-Commit Infinite Loop
doc_type: finding
status: active
canonical: true
created: 2025-10-17
tags: [pre-commit, docs-validate, bugfix, infinite-loop]
summary: >
  Fixed infinite loop in docs-validate pre-commit hook caused by registry.json regeneration.
source:
  author: agent
  agent: claude
  model: sonnet-4.5
---

# Fix docs-validate Pre-Commit Infinite Loop

## Problem

The `docs-validate` pre-commit hook was causing an infinite loop:

1. Hook runs when markdown files in `docs/` are staged
2. Hook **always** regenerates `docs/index/registry.json`
3. Regenerated `registry.json` is modified
4. Modified `registry.json` triggers the hook again
5. **Infinite loop**

### Symptoms

```bash
$ git commit -m "docs: update documentation"
# Hook runs, modifies registry.json
# registry.json shows as "MM" (modified in index and working tree)
# Hook runs again, modifies registry.json again
# ... infinite loop ...

# User forced to bypass with --no-verify
$ git commit --no-verify -m "docs: update documentation"
```

## Root Cause

**File:** `scripts/docs_validate.py` line 46

```python
# Generate registry (even if errors exist)
generate_registry(entries)  # ALWAYS regenerates, even in pre-commit
```

**Pre-commit config:** `.pre-commit-config.yaml` line 119-129

```yaml
- id: docs-validate
  name: Documentation Front-Matter Validation (R-DOC-002)
  entry: python scripts/docs_validate.py  # No flag to skip generation
  language: python
  files: '^docs/.*\.md$'  # Triggers on any docs/*.md change
  pass_filenames: false
```

## Solution

Three-part fix to prevent infinite loop:

### 1. Add `--pre-commit` Flag to Script

**File:** `scripts/docs_validate.py`

Added argument parser and conditional registry generation:

```python
def main():
    import argparse

    parser = argparse.ArgumentParser(description="Validate documentation and generate registry")
    parser.add_argument("--pre-commit", action="store_true",
                       help="Pre-commit mode: validate only, don't regenerate registry")
    args = parser.parse_args()

    # ... validation code ...

    # Generate registry (skip in pre-commit mode to avoid infinite loop)
    if not args.pre_commit:
        generate_registry(entries)
        print()
    else:
        print("(Pre-commit mode: skipping registry regeneration)")
        print()
```

### 2. Update Pre-Commit Hook Configuration

**File:** `.pre-commit-config.yaml`

```yaml
- id: docs-validate
  name: Documentation Front-Matter Validation (R-DOC-002)
  entry: python scripts/docs_validate.py --pre-commit  # Added flag
  language: python
  files: '^docs/.*\.md$'
  exclude: 'docs/index/registry\.json$'  # Exclude registry from triggering
  pass_filenames: false
```

### 3. Mark Registry as Generated File

**File:** `.gitattributes`

```gitattributes
# Generated files
docs/index/registry.json linguist-generated=true
```

## Behavior After Fix

### During Pre-Commit Hook

```bash
$ git add docs/guides/new-guide.md
$ git commit -m "docs: add new guide"

# Hook runs with --pre-commit flag
Validating documentation...
(Pre-commit mode: skipping registry regeneration)

All documentation validated successfully!

# Commit succeeds without modifying registry.json
```

### Manual Registry Regeneration

When you want to regenerate the registry:

```bash
# Via task command (no --pre-commit flag)
task docs:validate

# Or directly
python scripts/docs_validate.py

# Output:
Validating documentation...
Registry generated: docs/index/registry.json
   Total docs: 28
   By type: {'guide': 23, 'finding': 2, 'plan': 1, 'rfc': 1, 'spec': 1}
   By status: {'active': 28}

All documentation validated successfully!
```

## When to Regenerate Registry

The registry should be regenerated:

1. **CI/CD Pipeline** - Always regenerate in CI
2. **After merging PRs** - Update registry with new docs
3. **Manual check** - Run `task docs:validate` to refresh
4. **Before release** - Ensure registry is up-to-date

The registry does **NOT** need regeneration:

- During pre-commit (validation only)
- When committing non-doc changes
- When only editing existing doc content (not front-matter)

## Task Commands

```bash
# Validate docs without regenerating registry (fast)
task lint:staged

# Validate and regenerate registry (full check)
task docs:validate

# Check documentation health
task docs:check
```

## Testing the Fix

```bash
# 1. Make changes to a doc
echo "test" >> docs/guides/task-runner.md

# 2. Stage and commit
git add docs/guides/task-runner.md
git commit -m "docs: update task runner guide"

# Expected: Hook runs, validates, does NOT regenerate registry
# Expected: Commit succeeds

# 3. Manually regenerate registry
task docs:validate

# Expected: Registry updated with latest docs
```

## CI/CD Considerations

In CI pipelines, you **DO** want to regenerate the registry to catch drift:

```yaml
# .github/workflows/docs-check.yml
- name: Validate documentation
  run: python scripts/docs_validate.py  # No --pre-commit flag

- name: Check for registry drift
  run: |
    if git diff --quiet docs/index/registry.json; then
      echo "Registry is up to date"
    else
      echo "ERROR: registry.json is out of sync"
      echo "Run: task docs:validate"
      exit 1
    fi
```

## Related Files

- `scripts/docs_validate.py` - Validation script with --pre-commit flag
- `.pre-commit-config.yaml` - Pre-commit hook configuration
- `.gitattributes` - Mark registry as generated
- `Taskfile.yml` - Task commands for docs validation
- `docs/index/registry.json` - Generated documentation registry

## References

- Issue: Infinite loop during git commit with docs changes
- Solution: Conditional registry generation with --pre-commit flag
- Best Practice: Separate validation (pre-commit) from generation (CI/manual)
