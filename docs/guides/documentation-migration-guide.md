---
doc_id: DOC-2025-00173
title: Documentation Migration Guide
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [documentation-migration-guide]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00123
title: Documentation Migration Guide
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [documentation-migration-guide]
summary: >
  (Add summary here)
source:
  author: system
---
---

doc_id: DOC-2025-00002
title: Documentation Migration Guide
doc_type: guide
status: active
canonical: true
created: 2025-10-17
tags: [docs, migration, cleanup]
summary: >
  Step-by-step guide for migrating existing scattered documentation to the new structured system
---

# Documentation Migration Guide

This guide helps you migrate existing documentation from the scattered state to the new structured documentation system.

## Current State Analysis

Your project currently has:

- 60+ markdown files scattered across multiple locations
- Versioned duplicates (TOOL-DESIGN.md, TOOL-DESIGN-V2.md, TOOL-DESIGN-V3.md)
- Phase documents (PHASE1-COMPLETE.md, PHASE1-REVISED-COMPLETE.md, PHASE1-FINAL.md)
- Documentation in root, docs/, packages/, .specify/, etc.

## Migration Strategy

### Phase 1: Quick Wins (Do First)

**Goal**: Get immediate benefits by organizing low-hanging fruit.

#### 1.1 Archive Obsolete Docs

Move superseded/obsolete docs to archive:

```bash
# Root-level phase docs (obsolete)
git mv PHASE1-COMPLETE.md docs/archive/
git mv PHASE1-REVISED-COMPLETE.md docs/archive/
git mv PHASE1-FINAL.md docs/archive/
git mv BUILD-PLAN.md docs/archive/
git mv IMPLEMENTATION-PLAN.md docs/archive/
git mv REVISED-APPROACH.md docs/archive/

# Versioned duplicates (keep only latest)
git mv packages/scoped-6571/com.contractwork.sangocard.build/TOOL-DESIGN.md docs/archive/
git mv packages/scoped-6571/com.contractwork.sangocard.build/TOOL-DESIGN-V2.md docs/archive/
# Keep TOOL-DESIGN-V3.md, migrate in Phase 2
```

#### 1.2 Consolidate Task Documents

Merge duplicated task files:

```bash
# .specify/tasks/ has duplicates
# Merge into single canonical task file
git mv .specify/tasks/build-preparation-tool-tasks.md docs/archive/
git mv .specify/tasks/build-preparation-tool-tasks-v2.md docs/archive/
# Keep tasks in .specify/specs/{feature-name}/tasks.md (spec-kit convention)
```

### Phase 2: Structured Migration (Core Docs)

**Goal**: Move important docs to correct locations with front-matter.

#### 2.1 Identify Document Types

For each remaining doc, determine its type:

| Current Location | Type | Target Location |
|------------------|------|-----------------|
| `.specify/specs/*.md` | spec | Keep (spec-kit managed) |
| `docs/rfcs/*.md` | rfc | Keep, add front-matter |
| Root `*-PLAN.md` | plan | `docs/plans/` |
| `packages/*/TOOL-DESIGN*.md` | spec | `docs/specs/` |
| `docs/AGENT-*.md` | guide | `docs/guides/` |
| `docs/MULTI-AGENT-*.md` | guide | `docs/guides/` |
| `.agent/base/*.md` | reference | Keep (agent rules) |

#### 2.2 Add Front-Matter Template

Create a helper script to add front-matter:

```bash
# scripts/add-frontmatter.sh
#!/bin/bash
FILE=$1
DOC_ID=$2
TITLE=$3
TYPE=$4

cat > "$FILE.tmp" <<EOF
---
doc_id: $DOC_ID
title: $TITLE
doc_type: $TYPE
status: active
canonical: true
created: $(date +%Y-%m-%d)
tags: []
summary: >
  TODO: Add summary
---

$(cat "$FILE")
EOF

mv "$FILE.tmp" "$FILE"
```

Usage:

```bash
./scripts/add-frontmatter.sh docs/guides/agent-orchestration.md DOC-2025-00010 "Agent Orchestration Guide" guide
```

#### 2.3 Migrate Core Documentation

Priority migration list:

**Specs**:

```bash
# Build preparation tool (currently fragmented)
# Consolidate: packages/.../TOOL-DESIGN-V3.md + .specify/specs/build-preparation-tool.md
# → docs/specs/build-preparation-tool.md

# 1. Extract content from both sources
# 2. Add front-matter with doc_id: DOC-2025-00042
# 3. Mark old locations as superseded
# 4. Move old docs to archive
```

**Plans**:

```bash
# Create canonical project phases doc
# Consolidate PHASE1-* into single timeline doc

cat > docs/plans/project-phases.md <<'EOF'
---
doc_id: DOC-2025-00050
title: Project Implementation Phases
doc_type: plan
status: active
canonical: true
created: 2025-10-17
tags: [planning, milestones, phases]
summary: >
  Unified timeline of project implementation phases and milestones.
---

# Project Implementation Phases

## Phase 1: Build System Foundation (Completed 2025-10-16)

[Consolidated content from PHASE1-COMPLETE.md, PHASE1-REVISED-COMPLETE.md, PHASE1-FINAL.md]

### Objectives
- ...

### Outcomes
- ...

## Phase 2: [Next phase]

...
EOF
```

**Guides**:

```bash
# Agent orchestration docs
git mv docs/AGENT-ORCHESTRATION.md docs/guides/agent-orchestration.md
# Add front-matter with doc_id: DOC-2025-00060

git mv docs/MULTI-AGENT-WORKFLOW-SUMMARY.md docs/guides/multi-agent-workflow.md
# Add front-matter with doc_id: DOC-2025-00061

git mv docs/QUICK-START-MULTI-AGENT.md docs/guides/multi-agent-quickstart.md
# Add front-matter with doc_id: DOC-2025-00062
```

**RFCs**:

```bash
# Already in correct location, just add front-matter
# docs/rfcs/RFC-001-build-preparation-tool.md
# Add front-matter with doc_id: RFC-2025-00001 (use RFC- prefix)
```

### Phase 3: Package-Level Docs

**Goal**: Simplify package docs, link to canonical.

#### 3.1 Package README Policy

Each package gets a minimal README:

```markdown
# SangoCard.Build

Build automation package for Unity projects.

## Documentation

- **Specification**: [Build Preparation Tool](../../docs/specs/build-preparation-tool.md)
- **User Guide**: [Build System Guide](../../docs/guides/build-system.md)
- **API Reference**: See XML docs in source files

## Quick Start

[2-3 paragraphs with minimal setup instructions]

For detailed documentation, see links above.
```

**Move detailed content to canonical docs**:

```bash
# Extract from packages/.../TOOL-DESIGN-V3.md
# → Merge into docs/specs/build-preparation-tool.md
# Replace package doc with minimal README pointing to canonical
```

### Phase 4: Agent Rule Updates

**Goal**: Update agent rules to reference new structure.

#### 4.1 Update Constitution

```bash
# .specify/memory/constitution.md
# Add section referencing documentation system
```

#### 4.2 Update Agent Adapters

```bash
# .agent/adapters/claude.md
# Add pointer to R-DOC-xxx rules
# Reference docs/DOCUMENTATION-SCHEMA.md
```

### Phase 5: Generate Initial Registry

**Goal**: Create baseline registry of all migrated docs.

```bash
# Run validation script to generate registry
python git-hooks/python/docs_validate.py

# This creates docs/index/registry.json
# Agents can now query this to find canonical docs
```

## Migration Workflow Example

Let's migrate `TOOL-DESIGN-V3.md` as an example:

### Step 1: Read Current Content

```bash
cat packages/scoped-6571/com.contractwork.sangocard.build/TOOL-DESIGN-V3.md
```

### Step 2: Create Canonical Version

```bash
# Create new canonical doc
cat > docs/specs/build-preparation-tool.md <<'EOF'
---
doc_id: DOC-2025-00042
title: Build Preparation Tool Specification
doc_type: spec
status: active
canonical: true
supersedes: []
created: 2025-10-16
updated: 2025-10-17
tags: [build, unity, tooling, roslyn]
summary: >
  Automated build preparation tool for Unity projects with Roslyn-based code patching.
source:
  author: human
related: [RFC-2025-00001]
---

# Build Preparation Tool Specification

[Consolidated content from TOOL-DESIGN-V3.md + .specify/specs/build-preparation-tool.md]

## Overview

...

## Architecture

...

## Changelog

### 2025-10-17
- Consolidated from multiple sources
- Migrated to canonical docs structure

### 2025-10-16
- Original V3 design (TOOL-DESIGN-V3.md)

### 2025-10-15
- V2 design (TOOL-DESIGN-V2.md)

### 2025-10-14
- Initial design (TOOL-DESIGN.md)
EOF
```

### Step 3: Update Package Doc

```bash
# Replace package doc with minimal version
cat > packages/scoped-6571/com.contractwork.sangocard.build/README.md <<'EOF'
# SangoCard.Build

Build preparation package for Unity projects.

## Documentation

Full specification: [Build Preparation Tool](../../../docs/specs/build-preparation-tool.md)

## Quick Start

Install via Unity Package Manager scoped registry.

For detailed usage, see the specification linked above.
EOF
```

### Step 4: Archive Old Versions

```bash
git mv packages/scoped-6571/com.contractwork.sangocard.build/TOOL-DESIGN.md docs/archive/
git mv packages/scoped-6571/com.contractwork.sangocard.build/TOOL-DESIGN-V2.md docs/archive/
git mv packages/scoped-6571/com.contractwork.sangocard.build/TOOL-DESIGN-V3.md docs/archive/
```

### Step 5: Validate

```bash
python git-hooks/python/docs_validate.py
```

### Step 6: Commit

```bash
git add -A
git commit -m "docs: migrate build preparation tool to canonical structure

- Consolidated TOOL-DESIGN-V*.md into canonical spec
- Added proper front-matter (DOC-2025-00042)
- Archived old versions
- Updated package README to reference canonical doc

Ref: DOC-2025-00042"
```

## Automated Migration Script

For bulk migration, use this script:

```python
#!/usr/bin/env python3
# scripts/migrate_docs.py

import pathlib
import yaml
from datetime import date

MIGRATIONS = [
    {
        "source": "docs/AGENT-ORCHESTRATION.md",
        "target": "docs/guides/agent-orchestration.md",
        "doc_id": "DOC-2025-00060",
        "title": "Agent Orchestration Guide",
        "doc_type": "guide",
        "tags": ["agents", "orchestration", "multi-agent"],
    },
    {
        "source": "docs/MULTI-AGENT-WORKFLOW-SUMMARY.md",
        "target": "docs/guides/multi-agent-workflow.md",
        "doc_id": "DOC-2025-00061",
        "title": "Multi-Agent Workflow Summary",
        "doc_type": "guide",
        "tags": ["agents", "workflow", "multi-agent"],
    },
    # Add more migrations...
]

def add_frontmatter(content: str, meta: dict) -> str:
    frontmatter = yaml.dump(meta, default_flow_style=False, sort_keys=False)
    return f"---\n{frontmatter}---\n\n{content}"

def migrate_doc(migration: dict):
    source = pathlib.Path(migration["source"])
    target = pathlib.Path(migration["target"])

    # Read original content
    content = source.read_text(encoding="utf-8")

    # Strip existing front-matter if present
    if content.startswith("---"):
        _, content = content.split("---", 2)[1:]
        content = content.strip()

    # Build new front-matter
    meta = {
        "doc_id": migration["doc_id"],
        "title": migration["title"],
        "doc_type": migration["doc_type"],
        "status": "active",
        "canonical": True,
        "created": str(date.today()),
        "tags": migration["tags"],
        "summary": f"TODO: Add summary for {migration['title']}",
    }

    # Write with front-matter
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(add_frontmatter(content, meta), encoding="utf-8")

    print(f"✅ Migrated: {source} → {target}")

if __name__ == "__main__":
    for migration in MIGRATIONS:
        migrate_doc(migration)
```

Run:

```bash
python scripts/migrate_docs.py
```

## Post-Migration Verification

### 1. Run Validation

```bash
python git-hooks/python/docs_validate.py
```

Expected output:

```
✅ Registry generated: docs/index/registry.json
   Total docs: 45
   By type: {'spec': 8, 'guide': 15, 'rfc': 3, 'plan': 5, ...}
   By status: {'active': 40, 'draft': 3, 'archived': 2}

✅ All documentation validated successfully!
```

### 2. Check for Orphaned Docs

```bash
# Find markdown files not in standard locations
find . -name "*.md" \
  -not -path "./docs/*" \
  -not -path "./node_modules/*" \
  -not -path "./.git/*" \
  -not -path "./.agent/*" \
  -not -path "./.specify/*" \
  -not -path "./build/*" \
  -not -path "./packages/*/README.md" \
  -not -path "./README.md" \
  -not -path "./CONTRIBUTING.md" \
  -not -path "./CODE_OF_CONDUCT.md"
```

### 3. Update Links

Find and update internal links to point to new canonical locations:

```bash
# Find broken links
grep -r "docs/AGENT-ORCHESTRATION.md" . --include="*.md"
# Replace with: docs/guides/agent-orchestration.md
```

### 4. Test CI

Create a test PR to verify CI workflows work:

```bash
git checkout -b test/docs-validation
git add -A
git commit -m "test: verify docs validation CI"
git push origin test/docs-validation
# Create PR and check GitHub Actions
```

## Ongoing Maintenance

### Daily Triage (5-10 minutes)

```bash
# 1. Check inbox
ls docs/_inbox/

# 2. For each doc in inbox:
#    - Review quality
#    - Check for duplicates via registry
#    - Merge into canonical OR promote to new canonical
#    - Move to appropriate category
#    - Archive old version if superseding

# 3. Regenerate registry
python git-hooks/python/docs_validate.py
```

### Weekly Cleanup

```bash
# Find docs without front-matter
grep -L "^---" docs/**/*.md | grep -v "/_inbox/" | grep -v "/archive/"

# Find near-duplicates
python git-hooks/python/docs_validate.py | grep "Near-duplicate"

# Review archive for docs that can be deleted permanently
ls docs/archive/
```

## Troubleshooting

### "Missing front-matter" Error

Add required fields:

```yaml
---
doc_id: DOC-2025-XXXXX  # Get next from registry
title: Your Title
doc_type: spec          # Valid types: spec, rfc, adr, plan, finding, guide, glossary, reference
status: active          # Valid: draft, active, superseded, rejected, archived
canonical: true         # Boolean
created: 2025-10-17     # ISO date
tags: [tag1, tag2]      # List
summary: >              # Multi-line string
  One-line description.
---
```

### "Multiple canonical docs" Error

Only one doc can have `canonical: true` per concept:

```bash
# Find conflict
jq '.docs[] | select(.canonical == true) | .path' docs/index/registry.json | sort

# Fix: Set older version to canonical: false, status: superseded
```

### "Near-duplicate" Warning

Merge content into existing canonical doc instead of creating new:

```bash
# 1. Find canonical version
jq '.docs[] | select(.canonical == true and .title | contains("your topic"))' docs/index/registry.json

# 2. Merge inbox content into canonical doc
# 3. Move inbox doc to archive
```

## Next Steps

After migration:

1. ✅ Train team/agents on new system (share this guide)
2. ✅ Update `.agent/base/40-documentation.md` with migration notes
3. ✅ Add documentation system to onboarding checklist
4. ✅ Schedule monthly review of archive (delete old docs after 90 days)
5. ✅ Consider MkDocs/Docusaurus for public rendering (optional)

## References

- **Schema**: `docs/DOCUMENTATION-SCHEMA.md`
- **Agent Rules**: `.agent/base/40-documentation.md`
- **Validation**: `git-hooks/python/docs_validate.py`
- **CI**: `.github/workflows/docs-guard.yml`
