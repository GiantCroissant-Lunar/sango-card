---
doc_id: DOC-2025-00202
title: Build Tool Quick Reference
doc_type: reference
status: active
canonical: true
created: 2025-10-17
tags: [cheatsheet, quick-reference, build-tool]
summary: One-page quick reference card for the Build Preparation Tool with shortcuts, commands, and common patterns
---

# Build Tool Quick Reference

One-page reference for the Build Preparation Tool.

## TUI Function Keys

| Key | Action | Description |
|-----|--------|-------------|
| **F1** | Help | Show help and documentation |
| **F2** | Config Type | Learn about config types |
| **F3** | Config Editor | Edit configurations |
| **F4** | Cache Manager | Manage build cache |
| **F5** | Manual Sources | Quick add sources |
| **F6** | Validation | Validate configs |
| **F7** | Preparation | Run Phase 1 |
| **F10** | Quit | Exit application |

## Navigation

| Key | Action |
|-----|--------|
| **↑/↓** | Navigate menu items |
| **←/→** | Switch tabs/sections |
| **Enter** | Select/Confirm |
| **Esc** | Back/Cancel |
| **Tab** | Next field |

## CLI Commands

### Quick Start

```bash
# Launch TUI
dotnet tool.dll tui

# Validate config
dotnet tool.dll validate --manifest file.json

# Run preparation
dotnet tool.dll prepare run --manifest file.json

# Cache info
dotnet tool.dll cache info
```

### All Commands

| Command | Description |
|---------|-------------|
| `config list` | List all configs |
| `config show --path <file>` | Show config details |
| `config create --type <type> --output <file>` | Create new config |
| `cache list` | List cache contents |
| `cache info` | Show cache statistics |
| `cache clear` | Clear cache |
| `cache verify` | Verify cache integrity |
| `validate --manifest <file>` | Validate manifest |
| `validate --config <file>` | Validate build config |
| `prepare run --manifest <file>` | Run Phase 1 |
| `tui` | Launch Terminal UI |

## Two-Phase System

### Phase 1: Preparation (Source → Cache)

**Config**: `PreparationManifest.json`

```json
{
  "version": "1.0.0",
  "packages": [
    { "name": "...", "sourcePath": "...", "targetFileName": "..." }
  ],
  "assemblies": [
    { "name": "...", "sourcePath": "..." }
  ],
  "assets": [
    { "name": "...", "sourcePath": "..." }
  ]
}
```

**Execute**: CLI `prepare run` or TUI **F7**

### Phase 2: Injection (Cache → Client)

**Config**: `PreparationConfig.json`

```json
{
  "version": "1.0.0",
  "clientPath": "D:/Unity/Client",
  "packages": [
    { "sourceFileName": "...", "targetPath": "Packages" }
  ],
  "assemblies": [
    { "sourceFileName": "...", "targetPath": "Assets/Plugins" }
  ],
  "assets": [
    { "sourcePattern": "...", "targetPath": "...", "operation": "Copy|Move|Delete" }
  ]
}
```

**Execute**: (Coming soon)

## TUI Menu Paths

### Main Menu

```text
File → Quit (F10)

View →
  ├─ Config Type Selection (F2)
  ├─ Config Editor (F3)
  ├─ Cache Manager (F4)
  ├─ Manual Sources (F5)
  ├─ Validation (F6)
  └─ Preparation (F7)

Manage →
  ├─ Preparation Sources  (Phase 1 CRUD)
  └─ Build Injections     (Phase 2 CRUD)

Help →
  ├─ About
  └─ Documentation
```

### Manage Screens

**Preparation Sources** (Phase 1):
- Load/Create/Save manifest
- Add/Edit/Remove: Packages, Assemblies, Assets
- Preview all items

**Build Injections** (Phase 2):
- Load/Create/Save config
- Switch sections: [ Packages ] [ Assemblies ] [ Assets ]
- Add/Edit/Remove per section
- Preview all sections

## Asset Operations

| Operation | Description | Cache Impact |
|-----------|-------------|--------------|
| **Copy** | Copy to client | Unchanged |
| **Move** | Move to client | Item removed |
| **Delete** | Delete from client | Unchanged |

## Glob Patterns

| Pattern | Matches |
|---------|---------|
| `*` | Any characters (no path separator) |
| `**` | Any characters (including path separator) |
| `?` | Single character |
| `[abc]` | Character set |
| `{a,b}` | Alternatives |

**Examples**:
- `**/*.cs` - All C# files
- `Prefabs/**/*` - All files in Prefabs
- `*.{dll,so}` - Native libraries

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `BUILD_PREP_CACHE_DIR` | `~/.sangocard/cache` | Cache location |
| `BUILD_PREP_LOG_LEVEL` | `Information` | Log level |
| `DOTNET_ENVIRONMENT` | `Production` | Environment |

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | General error |
| `2` | Validation failed |
| `3` | File not found |
| `4` | Permission error |

## Common Workflows

### New Project Setup

```bash
# 1. Create manifest (TUI)
dotnet tool.dll tui
# Manage → Preparation Sources → Create New

# 2. Run Phase 1
dotnet tool.dll prepare run --manifest sources.json

# 3. Create build config (TUI)
# Manage → Build Injections → Create New

# 4. Run Phase 2 (when available)
```

### Automated Build

```bash
# Validate
dotnet tool.dll validate --manifest m.json --config c.json

# Phase 1
dotnet tool.dll prepare run --manifest m.json

# Verify
dotnet tool.dll cache verify

# Phase 2 (when available)
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| TUI not rendering | Use modern terminal (Windows Terminal, iTerm2) |
| Source path not found | Update absolute paths in manifest |
| Permission denied | Run with elevated permissions |
| Invalid JSON | Use TUI validation (F6) |

## File Locations

### Cache Structure

```text
~/.sangocard/cache/
├── packages/         # .tgz files
├── assemblies/       # .dll files
└── assets/           # Asset files/folders
```

### Config Search Paths

1. Current directory
2. `./config/`
3. `~/.sangocard/config/`
4. `{git-root}/config/`

## Testing

### Automated Tests

```bash
# Run all tests
.\test-integration\run-integration-tests.ps1

# Quick validation
task build && .\test-integration\run-integration-tests.ps1
```

### Manual Testing

```bash
# Launch TUI
dotnet tool.dll tui

# Follow checklist
# docs/guides/build-tool-integration-testing-checklist.md
```

## Documentation Links

- **User Guide**: `docs/guides/build-tool-user-guide.md`
- **API Reference**: `docs/guides/build-tool-api-reference.md`
- **Test Checklist**: `docs/guides/build-tool-integration-testing-checklist.md`
- **Specification**: `docs/specs/build-preparation-tool.md`

## Support

- GitHub Issues: Report bugs
- TUI Help: Press **F1**
- CLI Help: `--help` flag

---

**Version**: 1.0.0 | **Updated**: 2025-10-17
