# Path Resolution Rules

## R-PATH-010: Git Repository Root as Base Path

**Rule:** All file paths in this project MUST be resolved relative to the git repository root.

**Git Root Detection:**

```
Repository Root: D:\lunar-snake\constract-work\card-projects\sango-card\
Marker: .git directory
```

**Path Format:**

- ✅ Correct: `build/preparation/configs/build-preparation.json`
- ✅ Correct: `projects/client/Packages/manifest.json`
- ✅ Correct: `packages/scoped-6571/com.contractwork.sangocard.build/package.json`
- ❌ Wrong: `../preparation/configs/build-preparation.json` (relative to unknown base)
- ❌ Wrong: `D:\absolute\path\to\file.json` (absolute paths in configs)

**Rationale:**

- Eliminates ambiguity between agents
- All agents (human, AI, build tools) agree on same base
- Portable within repository
- Clear error messages when git root not found

## R-PATH-020: Path Resolution Algorithm

**Implementation:**

1. Detect git root by walking up directory tree until `.git` found
2. Store git root as base path
3. Resolve all relative paths from git root
4. Validate git root exists before any path operations
5. Fail fast with clear error if git root not found

**Example:**

```csharp
// Detect git root
var gitRoot = DetectGitRoot(currentPath);
if (gitRoot == null)
    throw new InvalidOperationException("Git root not found. Must be inside repository.");

// Resolve path
var fullPath = Path.Combine(gitRoot, "build/preparation/configs/build-preparation.json");
```

## R-PATH-030: Configuration Files

**Rule:** All configuration files MUST store paths relative to git root.

**Example (build-preparation.json):**

```json
{
  "pathResolution": {
    "strategy": "git-root",
    "gitRoot": null  // Auto-detected, not stored
  },
  "assetManipulations": [
    {
      "source": {
        "path": "build/preparation/unity-packages/org.nuget.microsoft.extensions.logging@xxx"
      },
      "destination": {
        "path": "projects/client/Packages/org.nuget.microsoft.extensions.logging@xxx"
      }
    }
  ]
}
```

## R-PATH-040: Tool and Script Behavior

**Rule:** All tools and scripts MUST:

1. Auto-detect git root on startup
2. Validate git root exists
3. Resolve all paths from git root
4. Report git root in verbose/debug output
5. Fail with clear error if git root not found

**Example Output:**

```
[INFO] Git root detected: D:\lunar-snake\constract-work\card-projects\sango-card
[INFO] Config path: build/preparation/configs/build-preparation.json
[INFO] Resolved to: D:\lunar-snake\constract-work\card-projects\sango-card\build\preparation\configs\build-preparation.json
```

## R-PATH-050: Agent Instructions

**For AI Agents:**
When working with file paths in this repository:

1. Always assume paths are relative to git root
2. Git root is: `D:\lunar-snake\constract-work\card-projects\sango-card\`
3. Never use `../` relative paths in configs or documentation
4. Always use forward slashes `/` in configs (cross-platform)
5. Use backslashes `\` only in Windows-specific commands

**For Human Developers:**

- All paths in documentation are relative to git root
- Use `git rev-parse --show-toplevel` to find git root
- Tools will auto-detect git root, no manual configuration needed

## R-PATH-060: Common Paths Reference

**Quick Reference (all relative to git root):**

```
Git Root:           .
Build System:       build/nuke/
Build Preparation:  build/preparation/
  ├─ Configs:       build/preparation/configs/
  ├─ Cache:         build/preparation/unity-packages/
  └─ Assemblies:    build/preparation/assemblies/
Projects:           projects/
  ├─ Client:        projects/client/
  └─ Code Quality:  projects/code-quality/
Packages:           packages/
  ├─ Cross:         packages/scoped-2151/com.contractwork.sangocard.cross/
  └─ Build:         packages/scoped-6571/com.contractwork.sangocard.build/
Documentation:      docs/
Specifications:     .specify/
```

## R-PATH-070: Error Handling

**Rule:** When git root cannot be detected, tools MUST:

1. Display clear error message
2. Show current working directory
3. Show path where search started
4. Suggest solutions (e.g., "Ensure you're inside the git repository")
5. Exit with non-zero code

**Example Error:**

```
ERROR: Git root not found

Current directory: D:\some\other\path
Search started at: D:\some\other\path\config.json
Searched up to: D:\

This tool must be run from within the git repository.
Please ensure you're inside: D:\lunar-snake\constract-work\card-projects\sango-card\

Or clone the repository if you haven't already.
```

## R-PATH-080: Cross-Platform Compatibility

**Rule:** Path handling MUST be cross-platform compatible.

**Guidelines:**

- Use `Path.Combine()` in C#, `os.path.join()` in Python
- Store paths with forward slashes `/` in configs
- Convert to platform-specific separators at runtime
- Test on Windows, Linux, macOS

**Example:**

```csharp
// Config stores: "build/preparation/configs/build-preparation.json"
// Runtime converts to: "build\preparation\configs\build-preparation.json" (Windows)
// Runtime converts to: "build/preparation/configs/build-preparation.json" (Linux/macOS)
```

## Compliance

**Rule ID:** R-PATH-010 through R-PATH-080
**Category:** Path Resolution
**Enforcement:** Mandatory
**Validation:** Automated via build tools and linters
