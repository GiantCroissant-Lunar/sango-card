# Python Git Hooks

This directory contains Python implementations of git-hooks that were previously written in PowerShell.

## Migration

The following PowerShell scripts have been converted to Python:

| PowerShell Script | Python Script | Status |
|------------------|---------------|--------|
| `pre-commit.ps1` | `check_partial_class_pattern.py` | ✅ Migrated |
| `check-scattered-docs.ps1` | `check_scattered_docs.py` | ✅ Migrated |
| `../fix-trailing-spaces.ps1` | `fix_trailing_spaces.py` | ✅ Migrated |

## Benefits of Python Migration

1. **Cross-platform compatibility**: Works on Windows, Linux, and macOS without requiring PowerShell
2. **Better integration**: Native support with pre-commit framework
3. **Easier maintenance**: More developers are familiar with Python
4. **Consistent environment**: Uses same Python version as other project tools
5. **Better testing**: Easier to write unit tests for Python code

## Scripts

### check_partial_class_pattern.py

Enforces the partial class interface separation pattern (R-CODE-090).

**Rule**: Classes implementing multiple interfaces must follow this pattern:

- Base file (`ClassName.cs`) contains only parent class inheritance
- Each interface in separate file (`ClassName.IInterfaceName.cs`)

**Documentation**: `docs/CODING-PATTERNS.md`

### check_scattered_docs.py

Detects markdown files in non-canonical locations (R-DOC-001).

**Allowed locations**:

- `docs/_inbox/` - New documentation
- `docs/guides/` - User guides
- `docs/specs/` - Specifications
- `docs/rfcs/` - Request for Comments
- `docs/adrs/` - Architecture Decision Records
- `docs/plans/` - Project plans
- `docs/findings/` - Research findings
- `docs/archive/` - Obsolete documentation
- Root level README, CHANGELOG, etc.
- Agent directories (`.agent/`, `.specify/`, etc.)

**Documentation**: `docs/DOCUMENTATION-SCHEMA.md`, `.agent/base/40-documentation.md`

### fix_trailing_spaces.py

Removes trailing spaces from specified files while preserving line endings.

**Target files**:

- `.specify/COORDINATION-STATUS.md`
- `scripts/docs_validate.py`

## Usage

These scripts are automatically executed by the pre-commit framework when configured in `.pre-commit-config.yaml`.

### Manual execution

```bash
# Check partial class pattern
python scripts/git-hooks/python/check_partial_class_pattern.py

# Check scattered docs
python scripts/git-hooks/python/check_scattered_docs.py

# Fix trailing spaces
python scripts/git-hooks/python/fix_trailing_spaces.py
```

### Installation

The hooks are installed via the pre-commit framework:

```bash
# Install pre-commit
uv tool install pre-commit

# Install hooks
pre-commit install

# Run all hooks manually
pre-commit run --all-files
```

## Requirements

- Python 3.11+
- No external dependencies (uses only standard library)

## Legacy PowerShell Scripts

The original PowerShell scripts are kept in the parent directory for reference:

- `../pre-commit.ps1`
- `../check-scattered-docs.ps1`
- `../../fix-trailing-spaces.ps1`

These can be removed once the Python migration is verified in production.

## Testing

To test the hooks without committing:

```bash
# Stage some files
git add <files>

# Run specific hook
pre-commit run partial-class-interface-separation
pre-commit run check-scattered-docs

# Run all hooks
pre-commit run --all-files
```

## Contributing

When adding new git-hooks:

1. Write the hook in Python (not PowerShell)
2. Place it in this directory
3. Add configuration to `.pre-commit-config.yaml`
4. Update this README
5. Add unit tests if the logic is complex

## References

- [pre-commit framework](https://pre-commit.com/)
- Project constitution: `.specify/memory/constitution.md`
- Base rules: `.agent/base/20-rules.md`
