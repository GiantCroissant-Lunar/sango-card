---
doc_id: DOC-2025-00186
title: Terminal Gui V2 Migration
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [terminal-gui-v2-migration]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00136
title: Terminal Gui V2 Migration
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [terminal-gui-v2-migration]
summary: >
  (Add summary here)
source:
  author: system
---
# Terminal.Gui v2 Migration - Completion Summary

## Overview

Successfully migrated the Build Preparation Tool TUI from Terminal.Gui v1 to v2, completing Wave 7-8 of the implementation plan.

## Changes Made

### 1. Package Updates

```xml
<!-- Directory.Packages.props -->
- Terminal.Gui: 1.17.1 → 2.0.0
- Microsoft.CodeAnalysis.CSharp: 4.8.0 → 4.10.0 (required by Terminal.Gui v2)
```

### 2. TuiHost.cs - Complete v2 Migration

**Inheritance Change:**

- `TuiHost` now inherits from `Toplevel` (v2 pattern)
- Views are added directly to the Toplevel instance

**Application Lifecycle:**

```csharp
// v2 Pattern
Application.Init();
try {
    Application.Run(this); // Pass Toplevel instance
} finally {
    Application.Shutdown();
}
```

**MenuBar API Changes:**

```csharp
// v2 Pattern
var menu = new MenuBar() {
    Menus = [
        new MenuBarItem("_File", new MenuItem[] { ... }),
        ...
    ]
};
```

**StatusBar API Changes:**

```csharp
// v2 Pattern
new StatusBar(new Shortcut[] {
    new(Key.F1, "Help", () => ShowHelpDialog()),
    ...
});
```

**Dialog API Changes:**

```csharp
// v2 Pattern
var dialog = new Dialog() { Title = "...", Width = 60, Height = 10 };
var button = new Button() { Text = "OK", IsDefault = true };
button.Accepting += (s, e) => { /* handler */ };
```

**TextField API Changes:**

```csharp
// v2 Pattern
var field = new TextField() {
    Text = "initial value",  // Property, not constructor param
    X = 1,
    Y = 1,
    Width = 40
};
```

### 3. Message Updates

**PreparationMessages.cs:**

```csharp
// Added new messages for TUI
public record PreparationStartedMessage(string? ConfigPath, string ClientPath, bool IsDryRun);
public record PreparationProgressMessage(int CurrentStep, int TotalSteps, string Message);
public record PreparationCompletedMessage(int Copied, int Moved, int Deleted, int Patched, TimeSpan Duration, string? BackupPath);
public record PreparationFailedMessage(string Error, bool WasRolledBack);
```

**ConfigMessages.cs:**

- Uses `FilePath` property (not `Path`)

### 4. View Stubs Created

Created 4 view stub files compatible with Terminal.Gui v2:

- `ValidationView.cs` - Simple placeholder
- `CacheManagementView.cs` - Simple placeholder
- `ConfigEditorView.cs` - Simple placeholder  
- `PreparationExecutionView.cs` - Simple placeholder

All inherit from `View` and use v2 layout API.

### 5. Service Updates

**PreparationService.cs:**

- Updated message creation to include new parameters
- `PreparationStartedMessage` now includes `ClientPath` and `IsDryRun`
- `PreparationCompletedMessage` now includes `Duration` and `BackupPath`

## Build Status

✅ **Compiles Successfully**

- 0 errors
- 2 warnings (async methods without await - pre-existing)

✅ **Runs Successfully**

- TUI launches without errors
- Welcome screen displays correctly
- Menu navigation works
- F-key shortcuts functional
- Placeholder views accessible

## Key Differences: Terminal.Gui v1 vs v2

| Feature | v1 | v2 |
|---------|----|----|
| **Application Init** | `Application.Init()` + `Application.Run()` | `Application.Init()` + `Application.Run(Toplevel)` |
| **Toplevel Pattern** | Add views to `Application.Top` | Inherit from `Toplevel`, add to self |
| **MenuBar** | `new MenuBar(MenuBarItem[])` | `new MenuBar() { Menus = [...] }` |
| **StatusBar** | `new StatusBar(StatusItem[])` | `new StatusBar(Shortcut[])` |
| **Dialog Size** | Constructor params | Properties |
| **TextField Init** | Constructor param for text | `Text` property |
| **Button Events** | `Clicked` event | `Accepting` event |
| **MenuItem** | Constructor with many params | Simplified constructor |

## Next Steps

### Immediate (Wave 8 Completion)

1. **Implement Full View Logic**
   - Add proper controls to each view
   - Integrate with services (ConfigService, CacheService, etc.)
   - Implement MessagePipe reactive updates
   - Add data validation and error handling

2. **Test Interactive Scenarios**
   - Config loading/saving
   - Cache population
   - Validation execution
   - Preparation dry-run

### Future (Wave 9-11)

1. **Testing** - Unit tests for views
2. **Documentation** - User guide, developer docs
3. **CI/CD** - Deployment pipeline

## Migration Checklist

- [x] Update package versions
- [x] Migrate TuiHost to Toplevel pattern
- [x] Update Application lifecycle
- [x] Convert MenuBar API
- [x] Convert StatusBar API
- [x] Convert Dialog API
- [x] Convert TextField API
- [x] Convert Button events
- [x] Create view stubs
- [x] Update messages
- [x] Fix service integrations
- [x] Verify build
- [x] Test runtime
- [ ] Implement full view logic (deferred to next session)

## Testing Notes

The TUI launches successfully with:

- Welcome screen visible
- Menu bar functional (File, View, Help)
- Status bar with F-key shortcuts
- All 4 view stubs accessible via F2-F5
- Dialogs work correctly
- Graceful shutdown on F10/Quit

## Files Modified

1. `Directory.Packages.props` - Package versions
2. `Tui/TuiHost.cs` - Complete v2 migration
3. `Tui/Views/ValidationView.cs` - Created stub
4. `Tui/Views/CacheManagementView.cs` - Created stub
5. `Tui/Views/ConfigEditorView.cs` - Created stub
6. `Tui/Views/PreparationExecutionView.cs` - Created stub
7. `Messages/PreparationMessages.cs` - Added new messages
8. `Core/Services/PreparationService.cs` - Updated message usage
9. `README.md` - Updated status

## Conclusion

✅ Terminal.Gui v2 migration **COMPLETE**
✅ TUI foundation **FULLY FUNCTIONAL**
✅ Application compiles and runs successfully
⏳ Full view implementation deferred to next phase

The migration maintains the same architecture while leveraging Terminal.Gui v2's improved APIs. The foundation is solid for implementing rich interactive views in the next iteration.
