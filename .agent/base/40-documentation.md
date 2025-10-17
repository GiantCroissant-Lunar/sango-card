# Documentation Rules for AI Agents

**Rule ID Prefix:** `R-DOC`

---

## Core Principles

**R-DOC-001: Inbox-First Writing**
All AI agents MUST write new documentation ONLY to `docs/_inbox/`. Never write directly to canonical locations (`docs/specs/`, `docs/rfcs/`, etc.).

**R-DOC-002: Mandatory Front-Matter**
Every markdown file in `docs/` MUST include YAML front-matter with ALL required fields. See `docs/DOCUMENTATION-SCHEMA.md`.

**R-DOC-003: Registry-First Search**
Before creating new documentation, ALWAYS check `docs/index/registry.json` to find existing canonical documents on the same topic.

**R-DOC-004: Update Over Create**
ALWAYS prefer updating an existing canonical document over creating a new one. Only create new docs when the topic is genuinely new.

---

## Required Workflow

### 1. Before Creating Documentation

```bash
# Step 1: Check if topic already exists
cat docs/index/registry.json | grep -i "your-topic"

# Step 2: Search for related docs
grep -r "your-topic" docs/ --include="*.md"
```

If a canonical document exists → Update it instead of creating new.

### 2. Creating New Documentation

If you must create a new document:

**Step 2.1: Write to inbox**

```yaml
# Save to: docs/_inbox/YYYY-MM-DD-your-title--DOC-YYYY-NNNNN.md
---
doc_id: DOC-2025-XXXXX           # Get next ID from registry
title: Your Descriptive Title
doc_type: spec|rfc|adr|plan|finding|guide|glossary|reference
status: draft
canonical: false                 # Always false in inbox
created: 2025-10-17
tags: [relevant, tags, here]
summary: >
  One-line description of what this document covers.
source:
  author: agent
  agent: claude|copilot|windsurf|gemini
  model: sonnet-4.5|gpt-4|etc
  session: session-id-if-available
---

# Your Title

Content here...
```

**Step 2.2: Include metadata**

- Always set `source.author: agent`
- Always set `source.agent` to your agent name
- Include `source.model` if known
- Include `source.session` if available

**Step 2.3: Get next doc_id**

```python
# Pseudo-code to get next ID
import json
registry = json.load(open("docs/index/registry.json"))
max_id = max(int(doc["doc_id"].split("-")[-1]) for doc in registry["docs"] if doc["doc_id"].startswith("DOC-2025-"))
next_id = f"DOC-2025-{max_id + 1:05d}"
```

### 3. Updating Existing Documentation

When updating a canonical document:

**Step 3.1: Verify it's canonical**

```bash
# Check registry first
jq '.docs[] | select(.path == "docs/specs/your-doc.md")' docs/index/registry.json
```

**Step 3.2: Preserve front-matter**

- Do NOT change `doc_id`, `created`, `canonical`
- Update `updated` field to current date
- Add your session to `source` if updating significantly

**Step 3.3: Add update note**

```markdown
## Changelog

### 2025-10-17 (Claude Sonnet 4.5)
- Updated section X with new approach Y
- Added examples for Z
```

---

## Document Types and Locations

| Type       | When to Use                                   | Save To         |
|------------|-----------------------------------------------|-----------------|
| `spec`     | Product/feature specifications                | `_inbox/` first |
| `rfc`      | Proposals needing discussion/decision         | `_inbox/` first |
| `adr`      | Architectural decisions (accepted/rejected)   | `_inbox/` first |
| `plan`     | Implementation plans, milestones, phases      | `_inbox/` first |
| `finding`  | Research results, benchmarks, comparisons     | `_inbox/` first |
| `guide`    | How-tos, tutorials, playbooks, runbooks       | `_inbox/` first |
| `glossary` | Term definitions                              | Update existing |
| `reference`| API docs, technical reference                 | `_inbox/` first |

---

## Anti-Patterns (DO NOT DO THIS)

❌ **Creating versioned duplicates**

```
TOOL-DESIGN.md
TOOL-DESIGN-V2.md
TOOL-DESIGN-V3.md
```

✅ **Instead**: Update `TOOL-DESIGN.md` and use `supersedes` field.

❌ **Creating phase/iteration docs**

```
PHASE1-COMPLETE.md
PHASE1-REVISED-COMPLETE.md
PHASE1-FINAL.md
```

✅ **Instead**: Single canonical `docs/plans/project-phases.md` with status updates.

❌ **Scattered findings**

```
/root/ANALYSIS.md
/packages/foo/RESEARCH.md
/build/INVESTIGATION.md
```

✅ **Instead**: All findings in `docs/_inbox/` → promoted to `docs/findings/`.

❌ **Creating docs without checking registry**

```
# Agent creates docs/architecture/new-system.md
# But docs/adrs/new-system-design.md already exists!
```

✅ **Instead**: Check registry first, update existing doc.

---

## Validation and CI

All documentation is validated by CI on every PR:

- ✅ Required front-matter present
- ✅ Valid `doc_type` and `status` values
- ✅ Only one `canonical: true` per concept
- ✅ No near-duplicates in `_inbox/` vs corpus
- ✅ Dead link checking
- ✅ Markdown linting

If your PR fails CI validation:

1. Read the error message carefully
2. Fix front-matter in affected files
3. See `docs/DOCUMENTATION-SCHEMA.md` for schema
4. For duplicates, merge content into canonical doc

---

## Special Cases

### Updating Agent Rule Files

Rule files in `.agent/base/`, `.agent/adapters/` are exempt from front-matter requirements but should still:

- Follow markdown linting rules
- Use clear section headers
- Include rule IDs (R-XXX-NNN)

### Package-Specific Docs

Package-level docs (`packages/*/README.md`, `packages/*/TOOL-DESIGN.md`) should:

- Include front-matter if length > 100 lines
- Link to canonical docs in `docs/` for full details
- Be concise (< 50 lines ideally)

### Templates

Templates in `.specify/templates/` do NOT need front-matter.

---

## Examples

### Good: Creating Finding Document

```yaml
# docs/_inbox/2025-10-17-roslyn-patch-benchmark--DOC-2025-00099.md
---
doc_id: DOC-2025-00099
title: Roslyn Patch Performance Benchmark
doc_type: finding
status: draft
canonical: false
created: 2025-10-17
tags: [roslyn, performance, build]
summary: >
  Benchmark results comparing Roslyn syntax tree patching approaches.
source:
  author: agent
  agent: claude
  model: sonnet-4.5
  session: abc123xyz
---

# Roslyn Patch Performance Benchmark

Tested 3 approaches to patching C# syntax trees...
```

### Good: Updating Canonical Spec

```yaml
# docs/specs/build-preparation-tool.md (existing canonical)
---
doc_id: DOC-2025-00042
title: Build Preparation Tool Specification
doc_type: spec
status: active
canonical: true
created: 2025-10-15
updated: 2025-10-17  # ← Added update date
tags: [build, unity, tooling]
summary: >
  Automated build preparation tool for Unity projects.
source:
  author: agent
  agent: claude
  model: sonnet-4.5
---

# Build Preparation Tool Specification

## Changelog

### 2025-10-17 (Claude Sonnet 4.5, session: abc123)
- Added caching strategy section
- Updated file structure diagram

...
```

---

## Summary Checklist

Before saving documentation, verify:

- [ ] Checked `docs/index/registry.json` for existing docs
- [ ] Saving to `docs/_inbox/` (not canonical location)
- [ ] All required front-matter fields present
- [ ] `doc_id` follows `PREFIX-YYYY-NNNNN` format
- [ ] `doc_type` is valid value
- [ ] `status` is valid value
- [ ] `canonical: false` (always in inbox)
- [ ] `source` metadata includes agent info
- [ ] `tags` are lowercase and relevant
- [ ] `summary` is clear and concise
- [ ] Content is well-structured with headers
- [ ] Links use relative paths

---

## References

- **Schema Definition**: `docs/DOCUMENTATION-SCHEMA.md`
- **Validation Script**: `scripts/docs_validate.py`
- **CI Workflow**: `.github/workflows/docs-guard.yml`
- **Registry**: `docs/index/registry.json` (auto-generated)

---

**Last Updated**: 2025-10-17
**Rule Version**: 1.0
**Applies To**: All AI agents (Claude, Copilot, Windsurf, Gemini, etc.)
