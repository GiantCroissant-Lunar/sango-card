# Build Flow Configuration Handover

**Date:** 2025-10-17  
**Session:** Build Flow Configuration Discussion  
**Status:** ✅ Complete - Ready for Implementation

---

## Context

This session focused on understanding and documenting the build preparation configuration system, specifically addressing questions about:

1. Where configuration files are located
2. What configuration is used during builds
3. How the two-phase workflow operates
4. Support for both files and folders in packages/assemblies

---

## Key Findings

### 1. Configuration File Location

**Build Configuration (Used During Builds):**
- **Location:** `build/preparation/configs/default.json` ⚠️ **Does not exist yet - needs to be created**
- **Defined in:** `build/nuke/build/Build.Preparation.cs` line 18
- **Purpose:** Specifies what to inject into `projects/client/` during Phase 2

**Example Configuration (Reference):**
- **Location:** `build/preparation/configs/prep.json` ✅ **Exists**
- **Purpose:** Demo/example showing the configuration structure

### 2. Two-Phase Workflow

#### Phase 1: Cache Population
```bash
task build:prepare:cache
```

**What it does:**
- Scans `projects/code-quality` for packages and assemblies
- **Automatically detects** files (no config needed for gathering)
- Copies discovered items to `build/preparation/cache/`
- Safe to run anytime (doesn't modify `projects/client/`)

**Source:** `projects/code-quality`  
**Destination:** `build/preparation/cache/`

#### Phase 2: Client Injection
```bash
task build:prepare:client
```

**What it does:**
1. Performs `git reset --hard` on `projects/client/` (R-BLD-060)
2. Reads `build/preparation/configs/default.json`
3. Injects packages from cache into `projects/client/Packages/`
4. Injects assemblies from cache into `projects/client/Assets/Plugins/`
5. Applies code patches (if configured)
6. Sets scripting define symbols (if configured)

**IMPORTANT:** Only run during build execution via Task runner (R-BLD-010)

### 3. Configuration Structure

The `default.json` configuration file should follow this structure:

```json
{
  "version": "1.0",
  "description": "Build preparation configuration for Sango Card",
  "packages": [
    {
      "name": "com.example.package",
      "version": "1.0.0",
      "source": "build/preparation/cache/com.example.package-1.0.0.tgz",
      "target": "projects/client/Packages/com.example.package-1.0.0.tgz"
    },
    {
      "name": "com.example.folderpkg",
      "version": "2.0.0",
      "source": "build/preparation/cache/com.example.folderpkg",
      "target": "projects/client/Packages/com.example.folderpkg"
    }
  ],
  "assemblies": [
    {
      "name": "Microsoft.Extensions.DependencyInjection",
      "version": "8.0.0",
      "source": "build/preparation/cache/Microsoft.Extensions.DependencyInjection.dll",
      "target": "projects/client/Assets/Plugins/Microsoft.Extensions.DependencyInjection.dll"
    },
    {
      "name": "MyPluginFolder",
      "version": "1.0.0",
      "source": "build/preparation/cache/MyPluginFolder",
      "target": "projects/client/Assets/Plugins/MyPluginFolder"
    }
  ],
  "assetManipulations": [
    {
      "operation": "Copy",
      "source": "build/preparation/cache/MyAssets",
      "target": "projects/client/Assets/MyAssets",
      "overwrite": true
    }
  ],
  "codePatches": [
    {
      "type": "CSharp",
      "file": "projects/client/Assets/Scripts/Example.cs",
      "search": "OldCode",
      "replace": "NewCode",
      "mode": "Replace",
      "optional": false
    }
  ],
  "scriptingDefineSymbols": {
    "add": ["CUSTOM_DEFINE", "FEATURE_FLAG"],
    "remove": [],
    "platform": null,
    "clearExisting": false
  }
}
```

### 4. File and Folder Support Enhancement

**IMPORTANT:** As of commit `5a92be8`, both packages and assemblies now support:
- ✅ **Single files** (e.g., `.tgz`, `.dll`)
- ✅ **Folders** (copied recursively with all contents)

**Implementation:**
- `PreparationService.cs` automatically detects whether source is file or folder
- Uses `File.Copy()` for files, `CopyDirectory()` for folders
- Fully backward compatible with existing file-only configs

**Use Cases:**
1. Unity packages in folder format (not archived)
2. Plugin folders with dependencies
3. Asset bundles with metadata
4. Native plugins with multiple files

**Documentation:** `.specify/FOLDER-SUPPORT-ENHANCEMENT.md`

---

## Configuration Workflow

### Step 1: Identify What to Inject

Examine `projects/code-quality` to identify:
- Unity packages (`.tgz` files or package folders)
- Assemblies (`.dll` files or plugin folders)
- Assets that need to be copied
- Code that needs patching

### Step 2: Create default.json

Create `build/preparation/configs/default.json` with:
1. List all packages to inject (files or folders)
2. List all assemblies to inject (files or folders)
3. Define any asset manipulations needed
4. Define any code patches required
5. Set scripting define symbols if needed

### Step 3: Populate Cache

```bash
task build:prepare:cache
```

This will scan `projects/code-quality` and populate the cache.

### Step 4: Validate Configuration

```bash
task build:prepare:validate
```

This validates the configuration without executing.

### Step 5: Dry Run (Optional)

```bash
task build:prepare:dry-run
```

Shows what would be changed without actually modifying files.

### Step 6: Build with Preparation

```bash
task build:unity:prepared
```

Full workflow: PrepareCache → PrepareClient → BuildUnity → RestoreClient

---

## Important Rules

### R-BLD-010: Task Runner Usage
**Always use Task runner for build operations:**
- ✅ `task build:prepare:cache`
- ✅ `task build:prepare:client`
- ✅ `task build:unity:prepared`
- ❌ Never use `nuke` commands directly

### R-BLD-060: Client Read-Only
**`projects/client` is read-only except during builds:**
- It's a standalone Git repository
- Only modified during build execution
- Must perform `git reset --hard` before injection
- Automatically restored after build

---

## File Locations Reference

### Configuration
- **Build config:** `build/preparation/configs/default.json` (create this)
- **Example config:** `build/preparation/configs/prep.json` (reference)
- **Nuke integration:** `build/nuke/build/Build.Preparation.cs`
- **Task definitions:** `Taskfile.yml`

### Cache
- **Cache directory:** `build/preparation/cache/`
- **Populated by:** Phase 1 (`task build:prepare:cache`)
- **Source:** `projects/code-quality`

### Documentation
- **Build workflow guide:** `docs/guides/build-preparation-workflow.md`
- **Folder support:** `.specify/FOLDER-SUPPORT-ENHANCEMENT.md`
- **Tool README:** `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/README.md`
- **Migration guide:** `docs/guides/build-tool-migration-guide.md`

### Code
- **PreparationService:** `packages/.../SangoCard.Build.Tool/Core/Services/PreparationService.cs`
- **CacheService:** `packages/.../SangoCard.Build.Tool/Core/Services/CacheService.cs`
- **ConfigService:** `packages/.../SangoCard.Build.Tool/Core/Services/ConfigService.cs`
- **Models:** `packages/.../SangoCard.Build.Tool/Core/Models/PreparationConfig.cs`

---

## Next Steps

### Immediate Actions Required

1. **Create `build/preparation/configs/default.json`**
   - Based on actual needs from `projects/code-quality`
   - Use the structure shown above
   - Start with minimal config and expand as needed

2. **Test the Workflow**
   ```bash
   # Populate cache
   task build:prepare:cache

   # Validate config
   task build:prepare:validate

   # Dry run to preview changes
   task build:prepare:dry-run

   # Full build with preparation
   task build:unity:prepared
   ```

3. **Document Project-Specific Needs**
   - What packages are required from code-quality?
   - What assemblies need to be injected?
   - Are there any code patches needed?
   - What scripting defines are required?

### Future Enhancements (Optional)

1. **Multiple Configurations**
   - Create configs for different build targets
   - Example: `default.json`, `development.json`, `production.json`

2. **Automated Config Generation**
   - Script to scan code-quality and generate config
   - Reduce manual configuration effort

3. **CI/CD Integration**
   - Integrate preparation into CI pipeline
   - Automated validation on PRs

---

## Questions Answered This Session

### Q1: Where is the configuration for gathering references?
**A:** There is NO configuration for gathering (Phase 1). The tool automatically scans `projects/code-quality` and detects all `.tgz` files and `.dll` files recursively.

### Q2: Which configuration is used during the build?
**A:** `build/preparation/configs/default.json` (defined in `Build.Preparation.cs` line 18). This file **does not exist yet** and needs to be created based on project requirements.

### Q3: Can packages/assemblies be folders instead of single files?
**A:** YES! As of commit `5a92be8`, both files and folders are supported. The tool automatically detects the type and handles accordingly.

---

## Related Commits

- `9a4c01f` - Initial spec-kit closure and Task integration
- `80ef867` - Fix pre-commit hook errors
- `5a92be8` - **Add folder support for packages and assemblies**
- `f6dda9a` - Reorganize build tool documentation
- `529a02d` - Update frontmatter and registry
- `75dcf98` - Add utility scripts
- `8baecfe` - Update gitattributes
- `3f34e58` - Allow conventional commits without scope

---

## Contact & Resources

**Documentation:**
- Build workflow: `docs/guides/build-preparation-workflow.md`
- Spec-kit guide: `docs/guides/spec-kit.md`
- Task runner: `docs/guides/task-runner.md`

**Specifications:**
- Main spec: `.specify/specs/build-preparation-tool.md`
- Amendment 001: `.specify/specs/build-preparation-tool-amendment-001.md`

**Status:**
- Coordination: `.specify/COORDINATION-STATUS.md`
- Handover: `.specify/HANDOVER.md`

---

**Status:** ✅ Ready for next session  
**Action Required:** Create `build/preparation/configs/default.json` based on project needs
