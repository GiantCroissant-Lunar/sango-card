---
title: "V1.0 Build Configuration Deprecation Plan"
type: plan
status: active
created: 2025-10-19
updated: 2025-10-19
tags: [build, deprecation, migration, v1, v2, multi-stage]
---

# V1.0 Configuration Format Deprecation Plan

## Status: DEPRECATED (2025-10-19)

The V1.0 single-file preparation configuration format is **deprecated** and will be removed in a future release.

## Migration Path

### V1.0 Format (DEPRECATED)

```json
{
  "version": "1.0",
  "packages": [...],
  "assemblies": [...],
  "assetManipulations": [...],
  "codePatches": [...]
}
```

### V2.0 Multi-Stage Format (CURRENT)

```json
{
  "version": "2.0",
  "cacheSource": "build/configs/preparation/preparation.json",
  "injectionStages": [
    {
      "name": "preBuild",
      "enabled": true,
      "packages": [...],
      "assemblies": [...],
      "assetManipulations": [...]
    }
  ]
}
```

## Deprecation Timeline

### Phase 1: Soft Deprecation (Current)

- **Date**: 2025-10-19
- **Status**: V1.0 still works but logs deprecation warnings
- **Action Required**: Migrate to V2.0 at your convenience
- **Breaking Changes**: None

### Phase 2: Hard Deprecation (Planned: 2025-11-01)

- **Status**: V1.0 support will show errors but still execute
- **Action Required**: **Must migrate to V2.0** before this date
- **Breaking Changes**: CI/CD pipelines will fail if using V1.0

### Phase 3: Removal (Planned: 2025-12-01)

- **Status**: V1.0 support completely removed
- **Action Required**: N/A (must be migrated by Phase 2)
- **Breaking Changes**: V1.0 configs will not load at all

## Migration Guide

### Automatic Migration

Run the migration tool to convert V1.0 to V2.0:

```bash
dotnet run --project packages/.../SangoCard.Build.Tool -- config migrate \
  --from build/configs/preparation/preparation.json \
  --to build/configs/preparation/multi-stage-preparation.json
```

### Manual Migration

1. **Create V2.0 config structure**:

   ```json
   {
     "version": "2.0",
     "cacheSource": "build/configs/preparation/preparation.json",
     "injectionStages": []
   }
   ```

2. **Move V1.0 content into preBuild stage**:

   ```json
   {
     "version": "2.0",
     "cacheSource": "build/configs/preparation/preparation.json",
     "injectionStages": [
       {
         "name": "preBuild",
         "enabled": true,
         "description": "Migrated from V1.0 config",
         "packages": [...],  // Copy from V1.0
         "assemblies": [...],  // Copy from V1.0
         "assetManipulations": [...],  // Copy from V1.0
         "codePatches": [...]  // Copy from V1.0
       }
     ]
   }
   ```

3. **Update build scripts**:
   - Change `--PreparationConfig` to `--MultiStageConfig`
   - Change `PrepareClient` target to `BuildWithMultiStage`

## Benefits of V2.0

1. **Sequential Phases**: Inject dependencies at different build stages
2. **Better Organization**: Separate test vs. build vs. packaging dependencies
3. **Platform-Specific**: iOS/Android-specific injections
4. **Conditional Execution**: Enable/disable stages per environment
5. **Extensibility**: Easy to add new stages without breaking existing workflow

## Support

For migration assistance, see:

- Schema: `build/configs/preparation/multi-stage-schema.json`
- Example: `build/configs/preparation/multi-stage-preparation.json`
- Docs: `build/configs/preparation/README.md`

## Current V1.0 Usage

The V1.0 `preparation.json` is still used for:

- **Cache population source** (via `cacheSource` in V2.0)
- Legacy reference for package versions

This file will remain but won't be used for direct injection.
