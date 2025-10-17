# .specify Directory

Project specification and coordination workspace.

## Directory Structure

```
.specify/
├── specs/                    # Technical specifications
│   └── *.md                 # Spec documents (SPEC-*)
│
├── tasks/                    # Implementation tasks
│   └── *.md                 # Task documents (TASK-*)
│
├── memory/                   # Project memory and constitution
│   └── constitution.md      # Project constitution
│
├── status/                   # Status and coordination documents
│   ├── handovers/           # Handover documents between agents/sessions
│   ├── decisions/           # Decision documents
│   ├── completions/         # Completion reports
│   └── COORDINATION-STATUS.md
│
├── summaries/                # Amendment and feature summaries
│   └── AMENDMENT-*-SUMMARY.md
│
├── scripts/                  # Utility scripts
│   └── powershell/          # PowerShell scripts
│
└── templates/                # Document templates
```

## Document Types

### Specifications (`specs/`)
Technical specifications for features and enhancements.

**Naming:** `<feature-name>.md` or `<feature-name>-amendment-NNN.md`

**Example:**
- `build-preparation-tool.md`
- `build-preparation-tool-amendment-001.md`
- `build-preparation-tool-amendment-002.md`

### Tasks (`tasks/`)
Implementation tasks derived from specifications.

**Naming:** `TASK-<ID>.md` or `TASK-<FEATURE>-NNN.md`

**Example:**
- `TASK-BLD-PREP-001.md`
- `TASK-BLD-PREP-002.md`

### Status Documents (`status/`)

#### Handovers (`status/handovers/`)
Documents for transferring context between agents or sessions.

**Example:**
- `HANDOVER.md` - General handover
- `BUILD-FLOW-CONFIG-HANDOVER.md` - Specific feature handover

#### Decisions (`status/decisions/`)
Documents requiring decisions or containing decision records.

**Example:**
- `DECISION-NEEDED.md`

#### Completions (`status/completions/`)
Reports of completed work, waves, or phases.

**Example:**
- `BOTH-AGENTS-COMPLETE.md`
- `WAVE-10-11-COMPLETE.md`
- `IMPLEMENTATION-COMPLETE.md`
- `SPEC-KIT-COMPLETION.md`

### Summaries (`summaries/`)
Quick reference summaries for amendments and major features.

**Naming:** `AMENDMENT-NNN-SUMMARY.md`

**Example:**
- `AMENDMENT-002-SUMMARY.md`

### Memory (`memory/`)
Project constitution and long-term memory.

**Files:**
- `constitution.md` - Project principles and rules

### Scripts (`scripts/`)
Utility scripts for project automation.

**Structure:**
- `powershell/` - PowerShell scripts
- Other script types as needed

### Templates (`templates/`)
Document templates for consistency.

## Usage Guidelines

### Creating New Documents

1. **Specification:**
   ```bash
   # Create in specs/
   touch .specify/specs/my-feature.md
   ```

2. **Task:**
   ```bash
   # Create in tasks/
   touch .specify/tasks/TASK-MY-FEATURE-001.md
   ```

3. **Status Update:**
   ```bash
   # Create in appropriate status subdirectory
   touch .specify/status/completions/PHASE-1-COMPLETE.md
   ```

4. **Summary:**
   ```bash
   # Create in summaries/
   touch .specify/summaries/AMENDMENT-003-SUMMARY.md
   ```

### Moving Documents

When reorganizing, use `git mv` to preserve history:

```bash
git mv old-location.md new-location.md
```

### Archiving

Completed or obsolete documents should be moved to appropriate subdirectories or archived:

```bash
# Move to completions
git mv .specify/ACTIVE-WORK.md .specify/status/completions/

# Or archive in docs/archive/
git mv .specify/OLD-DOC.md docs/archive/
```

## Maintenance

- **Keep root clean:** Only README should be at root
- **Use subdirectories:** All documents belong in subdirectories
- **Consistent naming:** Follow naming conventions
- **Git history:** Use `git mv` for moves to preserve history

## Related Documentation

- **Project Docs:** `docs/` - General project documentation
- **ADRs:** `docs/adrs/` - Architecture Decision Records
- **Archive:** `docs/archive/` - Archived documents
