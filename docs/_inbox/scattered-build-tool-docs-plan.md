---
doc_id: DOC-2025-00194
title: Scattered Build Tool Docs Plan
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [scattered-build-tool-docs-plan]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00144
title: Scattered Build Tool Docs Plan
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [scattered-build-tool-docs-plan]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00141
title: Plan for Scattered Build Tool Documentation
doc_type: plan
status: active
canonical: true
created: 2025-10-17
tags: [docs, build-tool, cleanup, organization]
summary: >
  Plan to organize scattered documentation in build tool package directory.
source:
  author: agent
  agent: claude
  model: sonnet-4.5
---

# Plan for Scattered Build Tool Documentation

## Current Situation

**Location:** `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/`

**Files Found:**
1. `README.md` - 13K (Package overview)
2. `MIGRATION-GUIDE.md` - 9.7K (Migration guide)
3. `TERMINAL_GUI_V2_MIGRATION.md` - 5.7K (Terminal.Gui v2 migration)
4. `TUI_MANUAL_TESTING_GUIDE.md` - 14K (Testing guide)
5. `WAVE_8_COMPLETION.md` - 9.0K (Wave 8 status)
6. `WAVE_9_COMPLETION.md` - 12K (Wave 9 status)
7. `WAVE_9_TEST_PLAN.md` - 7.9K (Test plan)

**Issue:** 6 out of 7 files are "scattered docs" that should be in canonical locations.

## Current Pattern Status

The pattern `"^packages/.*/dotnet~/.*\.md$"` in `scripts/check-docs.ps1` **should** allow these files, but they're still being flagged as scattered.

**Why?** The pattern matches, but these are NOT truly package documentation - they're:
- **Wave completion reports** (belong in docs/archive/)
- **Testing guides** (belong in docs/guides/)
- **Migration guides** (belong in docs/guides/)

Only `README.md` should stay in the package directory.

## Proposed Solution

### Option 1: Move to Canonical Locations (Recommended)

**Rationale:** These documents are project-level documentation that happen to be in the package directory. They belong with the rest of the project docs.

#### Categorization & Moves

| File | Category | Destination | Status |
|------|----------|-------------|--------|
| `README.md` | Package docs | **Keep in place** | ✓ Canonical |
| `MIGRATION-GUIDE.md` | User guide | `docs/guides/build-tool-migration-guide.md` | Move |
| `TERMINAL_GUI_V2_MIGRATION.md` | Dev guide | `docs/guides/terminal-gui-v2-migration.md` | Move |
| `TUI_MANUAL_TESTING_GUIDE.md` | Testing guide | `docs/guides/build-tool-tui-testing-guide.md` | Move |
| `WAVE_8_COMPLETION.md` | Completion report | `docs/archive/build-tool-wave-8-completion.md` | Move |
| `WAVE_9_COMPLETION.md` | Completion report | `docs/archive/build-tool-wave-9-completion.md` | Move |
| `WAVE_9_TEST_PLAN.md` | Test plan | `docs/archive/build-tool-wave-9-test-plan.md` | Move |

### Option 2: Exclude from Scattered Docs Check

**Rationale:** These are legitimately part of the .NET tool package and should be distributed with it.

**Implementation:**
- Keep all files in `packages/.../dotnet~/tool/`
- Update pattern to be more permissive
- Accept that these are not "scattered" but intentional package documentation

**Cons:**
- Duplicates project-level documentation
- Wave completion reports aren't really package docs
- Testing guides should be centralized

### Option 3: Hybrid Approach

**Keep in Package:**
- `README.md` - Package overview
- `MIGRATION-GUIDE.md` - User-facing migration guide

**Move to Canonical Docs:**
- `TUI_MANUAL_TESTING_GUIDE.md` → `docs/guides/`
- `TERMINAL_GUI_V2_MIGRATION.md` → `docs/guides/`
- `WAVE_8_COMPLETION.md` → `docs/archive/`
- `WAVE_9_COMPLETION.md` → `docs/archive/`
- `WAVE_9_TEST_PLAN.md` → `docs/archive/`

## Recommended Action: Option 1

### Step 1: Move Completion Reports to Archive

```bash
# Wave completion reports (historical)
mv packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/WAVE_8_COMPLETION.md \
   docs/archive/build-tool-wave-8-completion.md

mv packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/WAVE_9_COMPLETION.md \
   docs/archive/build-tool-wave-9-completion.md

mv packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/WAVE_9_TEST_PLAN.md \
   docs/archive/build-tool-wave-9-test-plan.md
```

### Step 2: Move Guides to docs/guides/

```bash
# User and developer guides
mv packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/MIGRATION-GUIDE.md \
   docs/guides/build-tool-migration-guide.md

mv packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/TERMINAL_GUI_V2_MIGRATION.md \
   docs/guides/terminal-gui-v2-migration.md

mv packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/TUI_MANUAL_TESTING_GUIDE.md \
   docs/guides/build-tool-tui-testing-guide.md
```

### Step 3: Keep README.md

```bash
# This stays - it's the package entry point
packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/README.md
```

### Step 4: Add Front-Matter

After moving, add YAML front-matter to each file:

**Guides:**
```yaml
---
doc_id: DOC-2025-XXXXX
title: Build Tool Migration Guide
doc_type: guide
status: active
canonical: true
created: 2025-10-17
tags: [build-tool, migration, guide]
summary: Migration guide for the build preparation tool
---
```

**Archive:**
```yaml
---
doc_id: DOC-2025-XXXXX
title: Build Tool Wave 9 Completion
doc_type: finding
status: archived
canonical: false
created: 2025-10-17
tags: [build-tool, wave-9, completion]
summary: Wave 9 completion report for build preparation tool
---
```

## Benefits of Option 1

1. **Single Source of Truth** - All project docs in `docs/`
2. **Easier to Find** - Developers know where to look
3. **Better Organization** - Guides separated from completion reports
4. **Proper Archiving** - Wave reports in `docs/archive/`
5. **Clean Package** - Only essential package docs in package dir

## Implementation Commands

```bash
# Option 1: Use task command to auto-fix
task docs:check:fix

# Then manually move to proper locations (not just inbox)
```

Or manually:

```bash
# Create proper filenames and move
cd packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/

# Move to archive
mv WAVE_8_COMPLETION.md ../../../../../docs/archive/build-tool-wave-8-completion.md
mv WAVE_9_COMPLETION.md ../../../../../docs/archive/build-tool-wave-9-completion.md
mv WAVE_9_TEST_PLAN.md ../../../../../docs/archive/build-tool-wave-9-test-plan.md

# Move to guides
mv MIGRATION-GUIDE.md ../../../../../docs/guides/build-tool-migration-guide.md
mv TERMINAL_GUI_V2_MIGRATION.md ../../../../../docs/guides/terminal-gui-v2-migration.md
mv TUI_MANUAL_TESTING_GUIDE.md ../../../../../docs/guides/build-tool-tui-testing-guide.md

# Keep README.md where it is
```

## Verification

After moving:

```bash
# Check no more scattered docs
task docs:check

# Add front-matter
pwsh scripts/add-frontmatter.ps1

# Validate
task docs:validate
```

## Next Steps

1. **Review** this plan with team
2. **Execute** moves (Option 1 recommended)
3. **Add front-matter** to moved files
4. **Update** any internal links that reference old paths
5. **Validate** with `task docs:check`

## Alternative: If Keeping in Package

If you decide these MUST stay in the package directory:

**Update:** `scripts/check-docs.ps1` line 61

Change:
```powershell
"^packages/.*/dotnet~/.*\.md$"
```

To explicitly allow tool subdirectory:
```powershell
"^packages/.*/dotnet~/tool/.*\.md$"
```

**Note:** This keeps scattered docs but acknowledges they're intentional.

## Recommendation

**Choose Option 1** - Move to canonical locations. These are project-level docs that happen to be in a package directory during development. Clean separation benefits everyone.
