# Python Git Hooks

This directory contains Python implementations of git-hooks for the project.

## Benefits

1. **Cross-platform compatibility**: Works on Windows, Linux, and macOS
2. **Better integration**: Native support with pre-commit framework
3. **Easier maintenance**: Standard Python code
4. **Consistent environment**: Uses same Python version as other project tools
5. **Better testing**: Easy to write unit tests

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

### docs_validate.py

**Hook**: Documentation Front-Matter Validation (R-DOC-002)

Validates all markdown files in `docs/` for:

- Required front-matter fields
- Canonical uniqueness
- Near-duplicate detection
- Status consistency

Generates `docs/index/registry.json` for agent consumption.

**Dependencies**: PyYAML, simhash, rapidfuzz

**Documentation**: `docs/DOCUMENTATION-SCHEMA.md`

### fix_trailing_spaces.py

Removes trailing spaces from specified files while preserving line endings.

**Target files**:

- `.specify/COORDINATION-STATUS.md`
- `git-hooks/python/docs_validate.py`

### add_frontmatter.py

Tool to add YAML front-matter to documentation files that are missing it.

**Usage**: Manual utility for documentation maintenance

**Example**:

```bash
python git-hooks/python/add_frontmatter.py docs/guides/my-guide.md
```

### add_frontmatter_bulk.py

Bulk version of add_frontmatter.py for processing multiple files at once.

**Usage**: Manual utility for documentation maintenance

**Example**:

```bash
python git-hooks/python/add_frontmatter_bulk.py docs/guides/
```

## Usage

These scripts are automatically executed by the pre-commit framework when configured in `.pre-commit-config.yaml`.

### Manual execution

```bash
# Check partial class pattern
python git-hooks/python/check_partial_class_pattern.py

# Check scattered docs
python git-hooks/python/check_scattered_docs.py

# Validate documentation
python git-hooks/python/docs_validate.py

# Fix trailing spaces
python git-hooks/python/fix_trailing_spaces.py

# Add front-matter to docs
python git-hooks/python/add_frontmatter.py <file>
python git-hooks/python/add_frontmatter_bulk.py <directory>
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
- Most scripts use only standard library
- `docs_validate.py` requires: PyYAML, simhash, rapidfuzz (installed automatically by pre-commit)

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
