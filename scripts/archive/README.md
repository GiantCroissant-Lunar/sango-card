# Archived Scripts

One-time migration and cleanup scripts that are no longer actively used but kept for reference.

## Scripts

### fix-duplicate-frontmatter.ps1

**Purpose**: One-time cleanup of duplicate YAML front-matter in documentation files

**Status**: Completed - no longer needed

**Usage** (historical):

```powershell
# Preview fixes
.\fix-duplicate-frontmatter.ps1 -DryRun

# Fix duplicates
.\fix-duplicate-frontmatter.ps1
```

### migrate_terraform_docs.py

**Purpose**: Migrate terraform documentation to canonical docs structure

**Status**: Migration completed

**Usage** (historical):

```bash
python migrate_terraform_docs.py
```

### move-build-tool-docs.ps1

**Purpose**: Reorganize build tool documentation

**Status**: Reorganization completed

**Usage** (historical):

```powershell
.\move-build-tool-docs.ps1
```

## Why Archive?

These scripts were created for specific one-time tasks:

- Database migrations
- Documentation reorganizations
- Code cleanups
- Structure refactoring

Once the task is complete, the scripts are archived rather than deleted:

- **Historical reference**: Understand what changes were made
- **Rollback capability**: Can be adapted if needed
- **Learning resource**: Examples of migration patterns
- **Audit trail**: Documentation of project evolution

## When to Use Archived Scripts

❌ **Don't**:

- Run these scripts on current codebase
- Include in CI/CD pipelines
- Reference in active documentation

✅ **Do**:

- Reference for similar future migrations
- Study as examples of scripting patterns
- Keep for historical context
- Adapt/fork for new one-time tasks

## Active Scripts

For current automation and tools, see:

- `scripts/` - Active installation and setup scripts
- `git-hooks/` - Pre-commit hooks and validators
- `.github/scripts/` - GitHub automation
