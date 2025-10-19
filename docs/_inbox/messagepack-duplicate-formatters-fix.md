---
title: MessagePack Duplicate Formatter Fix
type: finding
status: active
created: 2025-10-19
tags: [build, messagepack, multi-stage, fix]
related:
  - build/configs/preparation/multi-stage-preparation.json
  - build/configs/preparation/multi-stage-schema.json
---

# MessagePack Duplicate Formatter Fix

## Issue

The Unity build fails with **MsgPack009 errors** due to duplicate MessagePack formatters in the readonly client repository:

```text
error MsgPack009: Multiple formatters for type Shop.Component.ProductDataComponent found
error MsgPack009: Multiple formatters for type Quest.Component.QuestDataComponent found
```

### Root Cause

The MessagePack code generator created duplicate formatters in `projects/client/Assets/Scripts/MessagePackGenerated/`:

1. **Quest duplicates:**
   - `QuestDataComponentFormatter.cs` ✅ (correct)
   - `QuestComponentFormatter.cs` ❌ (duplicate, also formats QuestDataComponent)

2. **Shop duplicates:**
   - `ProductDataComponentFormatter.cs` ✅ (correct)
   - `ShopItemDataComponentFormatter.cs` ❌ (duplicate, also formats ProductDataComponent)

### Impact

- **2 CRITICAL build errors** (12 occurrences)
- **Build failure** - Unity cannot compile due to ambiguous formatter registration
- **Verified by log parser:** `task logs:parse:build` identified these as CRITICAL severity

## Solution

Since `projects/client` is a **readonly git repository** (not a submodule/subtree), we cannot modify it directly. Instead, we handle this **during the build process** using the multi-stage preparation system.

### Implementation

The `multi-stage-preparation.json` config includes `assetManipulations` in the `preBuild` stage:

```json
{
  "injectionStages": {
    "preBuild": {
      "assetManipulations": [
        {
          "type": "delete",
          "target": "projects/client/Assets/Scripts/MessagePackGenerated/MessagePack_Formatters_Quest_Component_QuestComponentFormatter.cs"
        },
        {
          "type": "delete",
          "target": "projects/client/Assets/Scripts/MessagePackGenerated/MessagePack_Formatters_Quest_Component_QuestComponentFormatter.cs.meta"
        },
        {
          "type": "delete",
          "target": "projects/client/Assets/Scripts/MessagePackGenerated/MessagePack_Formatters_Shop_Component_ShopItemDataComponentFormatter.cs"
        },
        {
          "type": "delete",
          "target": "projects/client/Assets/Scripts/MessagePackGenerated/MessagePack_Formatters_Shop_Component_ShopItemDataComponentFormatter.cs.meta"
        }
      ]
    }
  }
}
```

### Build Workflow

1. **PrepareCache** stage: Populate cache from `preparation.json`
2. **InjectPreBuild** stage:
   - Inject build package (`com.contractwork.sangocard.build`)
   - **Delete duplicate formatters** (4 files)
3. **BuildUnity** stage: Build succeeds with no MsgPack009 errors
4. **CleanupPreBuild** stage: Restore client to clean state (git reset)

## Verification

After implementing this fix:

```bash
# Run multi-stage build
task build:unity:multi-stage

# Verify no MsgPack009 errors
task logs:parse:build
```

**Expected result:**

- ✅ 0 errors (down from 2 CRITICAL)
- ✅ Build completes successfully
- ✅ Client repo remains clean (readonly constraint respected)

## Why This Approach?

### ❌ Cannot Do: Direct Modification

```bash
# This violates readonly constraint
cd projects/client
rm MessagePack_Formatters_Quest_Component_QuestComponentFormatter.cs
git commit -m "fix: remove duplicate"
git push  # ❌ READONLY - Cannot push
```

### ✅ Correct: Build-Time Cleanup

- **Respects readonly constraint**: Client repo unchanged
- **Automated**: Runs every build, no manual intervention
- **Reversible**: Cleanup stage restores client to original state
- **Documented**: Clear understanding of what's happening
- **Maintainable**: If upstream fixes duplicates, we just remove our workaround

## References

- **Log Parser Output:** Shows MsgPack009 as CRITICAL severity
- **Build Tool:** `SangoCard.Build.Tool` handles assetManipulations
- **Multi-Stage Config:** `build/configs/preparation/multi-stage-preparation.json`
- **Schema:** `build/configs/preparation/multi-stage-schema.json`

---

**Created:** 2025-10-19  
**Issue Identified By:** Unity Log Parser (`task logs:parse:build`)  
**Solution Implemented:** Multi-stage preparation system
