# Build Preparation Config Design

## Overview

This document explains the design decisions behind the build preparation configuration system, particularly why we have separate `packages`, `assemblies`, and `assetManipulations` sections.

## Configuration Structure

### Phase 1: preparation.json (Cache Population)

```json
{
  "packages": [],      // Unity packages to extract
  "assemblies": []     // NuGet assemblies to extract
}
```

### Phase 2: injection.json (Client Injection)

```json
{
  "packages": [],              // Unity packages to inject
  "assemblies": [],            // NuGet assemblies to inject
  "assetManipulations": [],    // Generic file operations
  "codePatches": [],           // In-place code modifications
  "scriptingDefineSymbols": {} // Compiler symbols
}
```

## Design Question: Why Not Use Only `assetManipulations`?

### The Question

Since `assetManipulations` supports `copy`, `move`, and `delete` operations, why do we need separate `packages` and `assemblies` arrays? Couldn't we just use:

```json
{
  "assetManipulations": [
    {
      "type": "copy",
      "source": "build/preparation/cache/com.cysharp.unitask",
      "target": "projects/client/Packages/com.cysharp.unitask"
    }
    // ... 92 more operations
  ]
}
```

### The Answer: Semantic Separation

We maintain separate sections for **semantic clarity** and **functional benefits**.

## Rationale

### 1. Semantic Intent & Self-Documentation

**`packages` and `assemblies`** explicitly declare:

- ✅ "These are the **dependencies** this build needs"
- ✅ Clear **inventory** of what's being injected
- ✅ **Self-documenting**: Shows exactly which packages/versions are required
- ✅ **Audit trail**: Easy to see what changed between configs

**`assetManipulations`** are generic file operations:

- ⚠️ Could be anything: textures, configs, scripts, dependencies, etc.
- ⚠️ Less clear what the **purpose** is
- ⚠️ Harder to **audit** dependencies
- ⚠️ No semantic meaning: "Is this a package or just files?"

**Example Comparison:**

```json
// Clear intent - this is a dependency
{
  "packages": [
    {
      "name": "com.cysharp.unitask",
      "version": "2.5.8",
      "source": "build/preparation/cache/com.cysharp.unitask",
      "target": "projects/client/Packages/com.cysharp.unitask"
    }
  ]
}

// Unclear intent - just a file copy
{
  "assetManipulations": [
    {
      "type": "copy",
      "source": "build/preparation/cache/com.cysharp.unitask",
      "target": "projects/client/Packages/com.cysharp.unitask"
    }
  ]
}
```

### 2. Validation & Reporting

**With Separate Sections:**

- ✅ **Dependency tracking**: "This build uses MessagePack 3.1.3"
- ✅ **Version management**: Easy to see all package versions at a glance
- ✅ **Conflict detection**: Can check if multiple versions of same package exist
- ✅ **Report generation**: Dry-run reports show packages separately from generic file ops
- ✅ **Statistics**: "65 packages, 27 assemblies, 12 deletions, 1 code patch"

**With Only `assetManipulations`:**

- ❌ Just "105 file operations" - no breakdown
- ❌ Can't easily answer "what packages does this build use?"
- ❌ Harder to detect version conflicts
- ❌ No clear dependency inventory

**Report Example:**

```markdown
## Operations

### Packages: 65 items
- com.cysharp.unitask (2.5.8)
- com.cysharp.messagepipe (1.8.1)
...

### Assemblies: 27 items
- MessagePack (3.1.3)
- MemoryPack (1.21.4)
...

### Asset Operations: 12 items
- Delete: MessagePack.3.1.1
- Delete: MessagePack.3.1.1.meta
...
```

vs.

```markdown
## Operations: 105 file operations
- Copy: com.cysharp.unitask
- Copy: MessagePack.3.1.3
- Delete: MessagePack.3.1.1
...
```

### 3. Different Validation Rules

**Packages/Assemblies:**

- Must have `name`, `version`, `source`, `target`
- Source must exist (validated in dry-run)
- Can check for Unity `package.json` structure
- Can validate assembly DLL existence
- Version format validation (`X.Y.Z`)

**AssetManipulations:**

- Generic file/folder operations
- Different validation rules (delete doesn't need source)
- No version tracking needed
- No package structure validation

### 4. Build Tool Integration & Future Features

**Current Benefits:**

- Generate dependency reports
- Validate package versions
- Check source existence
- Track what changed between builds

**Future Possibilities:**

- Auto-generate Unity `manifest.json` from `packages[]`
- Create dependency graphs
- Check for security vulnerabilities by package name/version
- Auto-update package versions
- Detect circular dependencies
- License compliance checking
- SBOM (Software Bill of Materials) generation

**Example Future Feature:**

```bash
# Generate dependency graph
.\build.ps1 GenerateDependencyGraph --config injection.json

# Check for security vulnerabilities
.\build.ps1 CheckVulnerabilities --config injection.json

# Update all packages to latest versions
.\build.ps1 UpdatePackages --config injection.json
```

### 5. Separation of Concerns

**`packages` / `assemblies`** = **What dependencies we need**

- Declarative: "I need these packages"
- Stable: Changes when dependencies change
- Business logic: What the application needs

**`assetManipulations`** = **Special operations**

- Imperative: "Do these file operations"
- Tactical: Changes when build process changes
- Build logic: How to prepare the environment

**`codePatches`** = **Code modifications**

- Surgical: "Fix these specific issues"
- Temporary: Should eventually be fixed upstream
- Workarounds: Known issues with dependencies

## Alternative Design Considered

### Unified Approach with Metadata

```json
{
  "operations": [
    {
      "type": "copy",
      "source": "build/preparation/cache/com.cysharp.unitask",
      "target": "projects/client/Packages/com.cysharp.unitask",
      "metadata": {
        "itemType": "package",
        "name": "com.cysharp.unitask",
        "version": "2.5.8"
      }
    },
    {
      "type": "delete",
      "target": "projects/client/Assets/Packages/MessagePack.3.1.1",
      "metadata": {
        "itemType": "cleanup",
        "reason": "Replaced by 3.1.3"
      }
    }
  ]
}
```

**Pros:**

- ✅ Single unified structure
- ✅ Simpler schema
- ✅ Flexible metadata

**Cons:**

- ❌ Less clear at a glance
- ❌ Metadata is optional - easy to forget
- ❌ Harder to validate (mixed types in one array)
- ❌ More verbose (metadata repeated)
- ❌ Loses semantic separation

## Decision: Keep Current Design

**Recommendation:** Maintain separate `packages`, `assemblies`, and `assetManipulations` sections.

**Reasons:**

1. **Clarity**: Immediate understanding of what's a dependency vs. what's a file operation
2. **Validation**: Different validation rules for different item types
3. **Reporting**: Better dry-run reports and documentation
4. **Future-proof**: Enables advanced features (dependency graphs, security scanning, SBOM)
5. **Maintainability**: Easier to understand and modify configs
6. **Tooling**: Enables specialized tools for dependency management

## Usage Guidelines

### When to Use `packages` / `assemblies`

Use these for:

- ✅ Unity packages (with package.json)
- ✅ NuGet assemblies (DLLs)
- ✅ Third-party dependencies
- ✅ Anything with a name and version

### When to Use `assetManipulations`

Use these for:

- ✅ Deleting old versions
- ✅ Moving files to different locations
- ✅ Copying non-package assets (textures, configs, etc.)
- ✅ Cleanup operations
- ✅ File reorganization

### When to Use `codePatches`

Use these for:

- ✅ Fixing bugs in third-party code
- ✅ Removing unwanted dependencies from manifest.json
- ✅ Temporary workarounds
- ✅ Configuration adjustments

## Example: Complete Workflow

```json
{
  "packages": [
    // Phase 1: Declare what we need
    {
      "name": "com.cysharp.unitask",
      "version": "2.5.8",
      "source": "build/preparation/cache/com.cysharp.unitask",
      "target": "projects/client/Packages/com.cysharp.unitask"
    }
  ],
  "assetManipulations": [
    // Phase 2: Clean up old versions
    {
      "type": "delete",
      "target": "projects/client/Assets/Packages/MessagePack.3.1.1"
    }
  ],
  "codePatches": [
    // Phase 3: Fix issues
    {
      "file": "projects/client/Packages/manifest.json",
      "patches": [
        {
          "search": "\"com.old.package\": \"...\",\n",
          "replace": ""
        }
      ]
    }
  ]
}
```

## Conclusion

The current design prioritizes **clarity**, **maintainability**, and **future extensibility** over simplicity. While it's possible to unify everything into a single `assetManipulations` array, the semantic separation provides significant benefits for understanding, validating, and reporting on build dependencies.

## Related Documentation

- [README.md](./README.md) - Config usage guide
- [../../nuke/build/Schemas/preparation.schema.json](../../nuke/build/Schemas/preparation.schema.json) - Phase 1 schema
- [../../nuke/build/Schemas/injection.schema.json](../../nuke/build/Schemas/injection.schema.json) - Phase 2 schema
- [../../nuke/build/Components/IConfigBuild.cs](../../nuke/build/Components/IConfigBuild.cs) - Config validation component
