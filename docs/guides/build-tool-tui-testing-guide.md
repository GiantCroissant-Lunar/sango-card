---
doc_id: DOC-2025-00170
title: Build Tool Tui Testing Guide
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [build-tool-tui-testing-guide]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00120
title: Build Tool Tui Testing Guide
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [build-tool-tui-testing-guide]
summary: >
  (Add summary here)
source:
  author: system
---
# Manual TUI Testing Guide

**Version**: 1.0  
**Date**: 2025-10-17  
**Tool**: Sango Card Build Preparation Tool - TUI Mode

---

## 🎯 Testing Overview

This guide provides comprehensive manual testing scenarios for the Terminal User Interface (TUI) of the Build Preparation Tool. Each test scenario includes step-by-step instructions, expected results, and pass/fail criteria.

---

## ⚙️ Setup

### Prerequisites

1. .NET 8.0 SDK installed
2. Terminal with 256-color support (Windows Terminal, iTerm2, or similar)
3. Git repository cloned
4. Test data available in `projects/code-quality`

### Launch TUI

```bash
cd packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool
dotnet run -- tui
```

**Expected Result**: TUI launches with Welcome screen, MenuBar, and StatusBar visible

---

## 📋 Test Scenarios

### Scenario 1: Application Launch & Navigation

**Test ID**: TUI-001  
**Priority**: Critical  
**Estimated Time**: 5 minutes

#### Steps

1. Launch TUI mode: `dotnet run -- tui`
2. Verify Welcome screen displays
3. Read the welcome message
4. Press `Alt+F` to open File menu
5. Press `Alt+V` to open View menu
6. Press `Alt+H` to open Help menu
7. Press `F1` for Help
8. Press `F2` to switch to Config Editor
9. Press `F3` to switch to Cache Management
10. Press `F4` to switch to Validation
11. Press `F5` to switch to Preparation
12. Press `F10` and confirm quit

#### Expected Results

- ✅ Welcome screen shows tool information and feature list
- ✅ All menu items are accessible
- ✅ All F-key shortcuts work
- ✅ View switching is smooth without errors
- ✅ Help dialog displays keyboard shortcuts
- ✅ Quit requires confirmation
- ✅ No visual glitches or crashes

#### Pass Criteria

- [ ] All views accessible
- [ ] No crashes or freezes
- [ ] UI elements render correctly

---

### Scenario 2: Config Editor - Create New Configuration

**Test ID**: TUI-002  
**Priority**: Critical  
**Estimated Time**: 10 minutes

#### Steps

1. Launch TUI and navigate to Config Editor (F2)
2. Click "New" button
3. Verify status shows "New configuration created"
4. Click "Add Package" button
5. Enter package details:
   - Name: `com.unity.addressables`
   - Version: `1.21.2`
   - Source: `build/preparation/cache/com.unity.addressables-1.21.2.tgz`
   - Target: `projects/client/Packages/com.unity.addressables-1.21.2.tgz`
6. Click "Add"
7. Verify package appears in "Unity Packages" list
8. Verify frame title shows "Unity Packages (1)"
9. Click "Add Assembly" button
10. Enter assembly details:
    - Name: `Newtonsoft.Json`
    - Source: `build/preparation/cache/Newtonsoft.Json.dll`
    - Target: `projects/client/Assets/Plugins/Newtonsoft.Json.dll`
11. Click "Add"
12. Verify assembly appears in "Assemblies" list
13. Click "Add Patch" button
14. Enter patch details:
    - Type: `CSharp`
    - Target File: `projects/client/Assets/Scripts/Test.cs`
    - Search: `OldMethod`
    - Replace: `NewMethod`
    - Description: `Rename method`
15. Click "Add"
16. Verify patch appears in "Code Patches" list
17. Enter save path: `build/preparation/configs/test-config.json`
18. Click "Save"
19. Verify status shows "Saved: build/preparation/configs/test-config.json"

#### Expected Results

- ✅ New config creates empty configuration
- ✅ Add dialogs appear and accept input
- ✅ Items appear in lists immediately after adding
- ✅ Frame titles update with correct counts
- ✅ Save succeeds without errors
- ✅ Status messages are clear and accurate

#### Pass Criteria

- [ ] All add operations work
- [ ] UI updates in real-time
- [ ] File saved successfully
- [ ] No validation errors

---

### Scenario 3: Config Editor - Load Existing Configuration

**Test ID**: TUI-003  
**Priority**: High  
**Estimated Time**: 5 minutes

#### Steps

1. Navigate to Config Editor (F2)
2. Enter path to existing config in "Config Path" field
3. Click "Load" button
4. Verify status shows "Loaded: [path]"
5. Verify all lists populate with data:
   - Unity Packages list shows packages
   - Assemblies list shows assemblies
   - Code Patches list shows patches
6. Verify frame titles show correct counts
7. Try loading non-existent file
8. Verify error dialog appears with clear message

#### Expected Results

- ✅ Valid config loads successfully
- ✅ All data displays correctly
- ✅ Counts match file contents
- ✅ Invalid path shows error dialog
- ✅ Error messages are helpful

#### Pass Criteria

- [ ] Successful load of valid config
- [ ] Error handling for invalid config
- [ ] UI reflects loaded data

---

### Scenario 4: Cache Management - Populate Cache

**Test ID**: TUI-004  
**Priority**: Critical  
**Estimated Time**: 10 minutes

#### Steps

1. Navigate to Cache Management (F3)
2. Verify "Source Path" defaults to `projects/code-quality`
3. Verify "Cache Path" defaults to `build/preparation/cache`
4. Click "Populate Cache" button
5. Observe progress bar appears
6. Wait for completion
7. Verify status shows "Success: Added X items to cache"
8. Verify "Cache Contents" list populates with items
9. Verify each item shows: `[Type]    [Name]    [Size]`
10. Verify "Cache: X items, Y bytes" statistics
11. Click "Refresh" button
12. Verify list updates

#### Expected Results

- ✅ Populate operation starts
- ✅ Progress bar shows activity
- ✅ Success message shows item count
- ✅ List displays formatted items
- ✅ Statistics are accurate
- ✅ Refresh works correctly

#### Pass Criteria

- [ ] Cache populates from source
- [ ] Items display correctly
- [ ] Statistics match actual cache
- [ ] No errors during operation

---

### Scenario 5: Cache Management - Clean Cache

**Test ID**: TUI-005  
**Priority**: High  
**Estimated Time**: 3 minutes

#### Steps

1. Ensure cache has items (run Scenario 4 if needed)
2. Navigate to Cache Management (F3)
3. Click "Clean Cache" button
4. Verify confirmation dialog appears
5. Click "Yes" to confirm
6. Verify status shows "Cache cleaned successfully - X items removed"
7. Verify "Cache Contents" list is empty
8. Verify statistics show "Cache: 0 items, 0 B"

#### Expected Results

- ✅ Confirmation dialog prevents accidental deletion
- ✅ Clean operation succeeds
- ✅ All items removed
- ✅ Statistics updated
- ✅ No errors

#### Pass Criteria

- [ ] Confirmation required
- [ ] Cache cleaned completely
- [ ] UI updates correctly

---

### Scenario 6: Validation - Validate Configuration

**Test ID**: TUI-006  
**Priority**: Critical  
**Estimated Time**: 8 minutes

#### Steps

1. Navigate to Validation (F4)
2. Enter path to test config
3. Click "Load" button
4. Verify config loads successfully
5. Select "Schema" validation level (radio button)
6. Click "Validate" button
7. Verify progress bar appears
8. Verify validation completes
9. Verify "Validation Summary" shows results
10. Select "File Existence" level
11. Click "Validate" again
12. Select "Unity Packages" level
13. Click "Validate" again
14. Select "Full" validation level
15. Click "Validate" final time
16. Verify "Errors" and "Warnings" lists populate
17. Verify frame titles show counts

#### Expected Results

- ✅ Each validation level executes
- ✅ Progress bar shows during validation
- ✅ Summary displays level, status, counts
- ✅ Errors/warnings show in separate lists
- ✅ Each item shows: `[Code] File: Message`
- ✅ Pass/fail status is clear (✓ or ✗)

#### Pass Criteria

- [ ] All validation levels work
- [ ] Results display correctly
- [ ] Errors are actionable
- [ ] Performance is acceptable (<5s)

---

### Scenario 7: Preparation - Dry Run Execution

**Test ID**: TUI-007  
**Priority**: Critical  
**Estimated Time**: 10 minutes

#### Steps

1. Navigate to Preparation Execution (F5)
2. Enter valid config path
3. Click "Load" button
4. Verify config loads, shows package/assembly/patch counts
5. Verify "Dry Run" checkbox is CHECKED (default)
6. Verify "Validate before execution" checkbox is CHECKED (default)
7. Click "Execute Preparation" button
8. Verify execution log starts updating
9. Verify timestamps appear in log entries
10. Verify operation statistics update:
    - Copied: X
    - Moved: X
    - Deleted: X
    - Patched: X
11. Verify progress bar shows progress
12. Verify individual file operations appear in log
13. Wait for completion
14. Verify final status shows success
15. Verify "=== Preparation DRY-RUN Completed ===" in log

#### Expected Results

- ✅ Dry-run executes without modifying files
- ✅ Log shows timestamped entries
- ✅ Statistics update in real-time
- ✅ File operations logged clearly
- ✅ Completion summary shows counts and duration
- ✅ No actual file modifications

#### Pass Criteria

- [ ] Dry-run completes successfully
- [ ] Log is clear and informative
- [ ] No files modified
- [ ] Statistics accurate

---

### Scenario 8: Preparation - Real Execution (CAREFUL!)

**Test ID**: TUI-008  
**Priority**: High  
**Estimated Time**: 12 minutes  
**WARNING**: This test modifies files!

#### Prerequisites

- Backup of client project
- Test configuration that won't break the project
- Restore plan in place

#### Steps

1. **BACKUP FIRST**: Create backup of `projects/client`
2. Navigate to Preparation Execution (F5)
3. Load test configuration
4. **UNCHECK** "Dry Run" checkbox
5. Keep "Validate before execution" CHECKED
6. Click "Execute Preparation" button
7. Verify confirmation dialog appears
8. Read warning message carefully
9. Click "Yes" to proceed
10. Monitor execution log
11. Verify file operations occur
12. Verify statistics update
13. Wait for completion
14. Verify backup path shown in completion log
15. Check actual files were modified

#### Expected Results

- ✅ Confirmation prevents accidental execution
- ✅ Validation runs first
- ✅ Files actually modified
- ✅ Backup created automatically
- ✅ Backup path logged
- ✅ All operations logged
- ✅ Completion status accurate

#### Post-Test

- [ ] Restore from backup: Click corresponding restore option or manually restore

#### Pass Criteria

- [ ] Confirmation required
- [ ] Files modified as expected
- [ ] Backup created
- [ ] Can restore successfully

---

### Scenario 9: Error Handling

**Test ID**: TUI-009  
**Priority**: High  
**Estimated Time**: 15 minutes

#### Steps

1. **Invalid Config Path**:
   - Try loading non-existent config
   - Verify error dialog shows
   - Verify error message is helpful

2. **Invalid Cache Source**:
   - Set source path to non-existent directory
   - Click "Populate Cache"
   - Verify error handling

3. **Permission Errors**:
   - Try saving config to read-only location
   - Verify error message

4. **Validation Errors**:
   - Load config with missing files
   - Run Full validation
   - Verify errors list populates
   - Verify error messages are actionable

5. **Preparation Errors**:
   - Load invalid config
   - Try execution
   - Verify validation catches errors before execution

#### Expected Results

- ✅ All errors caught gracefully
- ✅ No crashes or freezes
- ✅ Error messages are clear and actionable
- ✅ User can recover from errors
- ✅ Logs show error details

#### Pass Criteria

- [ ] No crashes on errors
- [ ] Error messages helpful
- [ ] Can continue after error

---

### Scenario 10: Stress Testing

**Test ID**: TUI-010  
**Priority**: Medium  
**Estimated Time**: 20 minutes

#### Steps

1. **Large Configuration**:
   - Create config with 50+ packages
   - Load in Config Editor
   - Verify performance acceptable

2. **Large Cache**:
   - Populate cache with 1000+ files
   - List in Cache Management
   - Verify list scrolling works
   - Verify statistics accurate

3. **Rapid View Switching**:
   - Quickly switch between all views (F2-F5)
   - Repeat 20 times
   - Verify no memory leaks or slowdown

4. **Long-Running Preparation**:
   - Execute preparation with many operations
   - Monitor for 5+ minutes
   - Verify UI remains responsive
   - Verify log scrolling works

5. **Concurrent Message Events**:
   - Trigger multiple operations
   - Verify MessagePipe handles all events
   - Verify UI updates correctly

#### Expected Results

- ✅ No crashes with large datasets
- ✅ UI remains responsive
- ✅ Memory usage stable
- ✅ All events processed correctly
- ✅ No visual glitches

#### Pass Criteria

- [ ] Handles large datasets
- [ ] No performance degradation
- [ ] No memory leaks
- [ ] UI stable under stress

---

## 📊 Test Results Summary

### Execution Record

| Test ID | Scenario | Status | Tester | Date | Notes |
|---------|----------|--------|--------|------|-------|
| TUI-001 | Launch & Navigation | ⏳ | - | - | - |
| TUI-002 | Create Config | ⏳ | - | - | - |
| TUI-003 | Load Config | ⏳ | - | - | - |
| TUI-004 | Populate Cache | ⏳ | - | - | - |
| TUI-005 | Clean Cache | ⏳ | - | - | - |
| TUI-006 | Validation | ⏳ | - | - | - |
| TUI-007 | Dry Run | ⏳ | - | - | - |
| TUI-008 | Real Execution | ⏳ | - | - | - |
| TUI-009 | Error Handling | ⏳ | - | - | - |
| TUI-010 | Stress Testing | ⏳ | - | - | - |

### Legend

- ⏳ Not Started
- 🔄 In Progress
- ✅ Passed
- ❌ Failed
- ⚠️ Partial Pass

---

## 🐛 Bug Report Template

When you find a bug during testing, report it using this format:

```markdown
### Bug Report

**Test ID**: TUI-XXX
**Severity**: Critical / High / Medium / Low
**Date**: YYYY-MM-DD
**Tester**: [Your Name]

**Description**:
[Clear description of the issue]

**Steps to Reproduce**:
1. Step 1
2. Step 2
3. ...

**Expected Result**:
[What should happen]

**Actual Result**:
[What actually happened]

**Screenshots/Logs**:
[Attach if available]

**Environment**:
- OS: Windows 11 / macOS 14 / Ubuntu 22.04
- Terminal: Windows Terminal / iTerm2 / GNOME Terminal
- .NET Version: 8.0.XXX
```

---

## ✅ Sign-Off

**Test Lead**: _______________  
**Date**: _______________  
**Overall Result**: PASS / FAIL / PARTIAL  
**Blocker Issues**: [List any critical blockers]  
**Ready for Production**: YES / NO

---

**Next Steps After Testing**:

1. Log all bugs found
2. Prioritize fixes
3. Retest after fixes
4. Update test results
5. Document any workarounds
6. Approve for Wave 10 (Documentation)
