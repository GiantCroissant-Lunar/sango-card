# Manual TUI Integration Test Checklist

## Test Environment

- Tool Path: `dotnet packages\scoped-6571\com.contractwork.sangocard.build\dotnet~\tool\SangoCard.Build.Tool\bin\Debug\net8.0\win-x64\SangoCard.Build.Tool.dll tui`
- Test Data: `test-integration` directory
- Date: [Fill in when testing]
- Tester: [Fill in]

## Pre-Test Setup

- [ ] Build completed successfully (`task build`)
- [ ] All test data files exist in `test-integration/`
- [ ] Terminal/console supports ANSI colors

---

## Test Suite 1: Main Menu & Navigation (15 min)

### 1.1 Main Menu Display

- [ ] Menu renders correctly
- [ ] All menu items visible:
  - [ ] Setup Sources
  - [ ] Setup Build
  - [ ] Manage
  - [ ] Exit
- [ ] Colors display properly
- [ ] Status bar shows at bottom

### 1.2 Navigation Controls

- [ ] Arrow Up/Down moves selection
- [ ] Enter selects item
- [ ] Escape returns to previous screen
- [ ] Tab key works (if applicable)

### 1.3 Exit Functionality

- [ ] Select "Exit" exits cleanly
- [ ] No error messages on exit
- [ ] Terminal returns to normal

---

## Test Suite 2: Setup Sources Views (20 min)

### 2.1 Config Type Selection

- [ ] Navigate: Main Menu → Setup Sources
- [ ] Options displayed:
  - [ ] Manual
  - [ ] Auto
  - [ ] Back
- [ ] Each option selects correctly

### 2.2 Manual Sources Selection

- [ ] Navigate: Setup Sources → Manual
- [ ] File browser displays
- [ ] Can navigate directories
- [ ] Can select .json files
- [ ] Back button returns to previous screen

### 2.3 Auto Sources Detection

- [ ] Navigate: Setup Sources → Auto
- [ ] Shows "detecting sources" message
- [ ] Displays detected packages/assemblies
- [ ] Lists all found items
- [ ] Confirmation works

### 2.4 Test with Real Data

- [ ] Use Manual mode
- [ ] Select `test-integration\test-manifest-full.json`
- [ ] Verify file loads correctly
- [ ] Check for success message

---

## Test Suite 3: Setup Build Views (20 min)

### 3.1 Config Type Selection

- [ ] Navigate: Main Menu → Setup Build
- [ ] Options displayed:
  - [ ] Manual
  - [ ] Auto
  - [ ] Back
- [ ] Each option selects correctly

### 3.2 Manual Build Config

- [ ] Navigate: Setup Build → Manual
- [ ] Config file selection works
- [ ] Client directory selection works
- [ ] Both paths validate
- [ ] Confirmation works

### 3.3 Auto Build Detection

- [ ] Navigate: Setup Build → Auto
- [ ] Shows "detecting config" message
- [ ] Displays detected configuration
- [ ] Lists packages/assemblies/assets
- [ ] Confirmation works

### 3.4 Test with Real Data

- [ ] Use Manual mode
- [ ] Select `test-integration\test-config-full.json`
- [ ] Verify config loads correctly
- [ ] Check for success message

---

## Test Suite 4: Preparation Sources Management (30 min)

### 4.1 Load Manifest

- [ ] Navigate: Main Menu → Manage → Preparation Sources
- [ ] Welcome screen displays
- [ ] "Load Manifest" option works
- [ ] File browser appears
- [ ] Select `test-integration\test-manifest-full.json`
- [ ] Manifest loads successfully
- [ ] Preview displays all items

### 4.2 Preview Display

- [ ] Packages section shows (3 items expected)
- [ ] Assemblies section shows (3 items expected)
- [ ] Assets section shows (4 items expected)
- [ ] All paths display correctly
- [ ] Scroll works for long lists

### 4.3 Add Package

- [ ] Select "Add Package"
- [ ] Form displays with fields:
  - [ ] Name
  - [ ] Source Path
  - [ ] Target File Name
- [ ] Fill in test data
- [ ] Save adds to list
- [ ] Preview updates

### 4.4 Edit Package

- [ ] Select existing package
- [ ] Choose "Edit"
- [ ] Form pre-fills with current data
- [ ] Modify a field
- [ ] Save updates the item
- [ ] Preview reflects changes

### 4.5 Remove Package

- [ ] Select existing package
- [ ] Choose "Remove"
- [ ] Confirmation prompt appears
- [ ] Confirm removal
- [ ] Item removed from list
- [ ] Preview updates

### 4.6 Add Assembly

- [ ] Same tests as Package (4.3-4.5)
- [ ] Fields: Name, Source Path

### 4.7 Add Asset

- [ ] Same tests as Package (4.3-4.5)
- [ ] Fields: Name, Source Path

### 4.8 Save Manifest

- [ ] Make changes to manifest
- [ ] Select "Save Manifest"
- [ ] File path prompt appears
- [ ] Enter: `test-integration\test-manifest-edited.json`
- [ ] Confirm save
- [ ] Success message displays
- [ ] Verify file exists on disk
- [ ] Validate JSON is well-formed

### 4.9 Create New Manifest

- [ ] Select "Create New"
- [ ] Confirm existing data cleared
- [ ] Add new items
- [ ] Save to new file
- [ ] Verify file created

---

## Test Suite 5: Build Injections Management (35 min)

### 5.1 Load Config

- [ ] Navigate: Main Menu → Manage → Build Injections
- [ ] Welcome screen displays
- [ ] "Load Config" option works
- [ ] File browser appears
- [ ] Select `test-integration\test-config-full.json`
- [ ] Config loads successfully
- [ ] Preview displays all items

### 5.2 Section Switching

- [ ] Default section is Packages
- [ ] Switch to Assemblies section
  - [ ] Display updates correctly
  - [ ] Shows 3 assemblies
- [ ] Switch to Assets section
  - [ ] Display updates correctly
  - [ ] Shows 4 assets
- [ ] Switch back to Packages
  - [ ] Shows 3 packages
- [ ] All section data preserved

### 5.3 Packages Section - Add

- [ ] Select "Add Package"
- [ ] Form displays:
  - [ ] Source File Name
  - [ ] Target Path
- [ ] Fill in test data
- [ ] Save adds to list
- [ ] Preview updates

### 5.4 Packages Section - Edit

- [ ] Select existing package
- [ ] Choose "Edit"
- [ ] Form pre-fills correctly
- [ ] Modify fields
- [ ] Save updates
- [ ] Preview reflects changes

### 5.5 Packages Section - Remove

- [ ] Select existing package
- [ ] Choose "Remove"
- [ ] Confirmation appears
- [ ] Confirm removal
- [ ] Item removed
- [ ] Preview updates

### 5.6 Assemblies Section - CRUD

- [ ] Repeat tests 5.3-5.5 for Assemblies
- [ ] Fields: Source File Name, Target Path
- [ ] Verify all operations work

### 5.7 Assets Section - Add

- [ ] Select "Add Asset"
- [ ] Form displays:
  - [ ] Source Pattern
  - [ ] Target Path
  - [ ] Operation (Copy/Move/Delete)
- [ ] Fill in test data
- [ ] Select operation type
- [ ] Save adds to list
- [ ] Preview updates

### 5.8 Assets Section - Edit

- [ ] Select existing asset
- [ ] Choose "Edit"
- [ ] Form pre-fills correctly
- [ ] Change operation type
- [ ] Save updates
- [ ] Preview reflects changes

### 5.9 Assets Section - Remove

- [ ] Select existing asset
- [ ] Choose "Remove"
- [ ] Confirmation appears
- [ ] Confirm removal
- [ ] Item removed
- [ ] Preview updates

### 5.10 Save Config

- [ ] Make changes across all sections
- [ ] Select "Save Config"
- [ ] File path prompt appears
- [ ] Enter: `test-integration\test-config-edited.json`
- [ ] Confirm save
- [ ] Success message displays
- [ ] Verify file exists
- [ ] Validate JSON is well-formed
- [ ] Check all sections saved correctly

### 5.11 Create New Config

- [ ] Select "Create New"
- [ ] Confirm all sections cleared
- [ ] Add items to each section
- [ ] Save to new file
- [ ] Verify file created
- [ ] Verify all sections in file

---

## Test Suite 6: Error Handling (20 min)

### 6.1 Invalid Manifest File

- [ ] Navigate: Manage → Preparation Sources → Load
- [ ] Select `test-integration\test-manifest-invalid.json`
- [ ] Error message displays
- [ ] Error is descriptive
- [ ] Can return to menu
- [ ] No crash

### 6.2 Invalid Config File

- [ ] Navigate: Manage → Build Injections → Load
- [ ] Select `test-integration\test-config-invalid.json`
- [ ] Error message displays
- [ ] Error is descriptive
- [ ] Can return to menu
- [ ] No crash

### 6.3 Non-existent File

- [ ] Try to load file that doesn't exist
- [ ] Appropriate error shown
- [ ] Can recover

### 6.4 Invalid Paths

- [ ] Try to enter invalid characters in paths
- [ ] Validation catches errors
- [ ] Error message helpful

### 6.5 Empty Fields

- [ ] Try to save with empty required fields
- [ ] Validation prevents save
- [ ] Error message shown

### 6.6 Duplicate Entries

- [ ] Try to add duplicate item
- [ ] Check if validation catches it
- [ ] Or allows and shows both

---

## Test Suite 7: Edge Cases (15 min)

### 7.1 Long File Paths

- [ ] Test with very long file paths (>200 chars)
- [ ] Display handles correctly
- [ ] No truncation issues
- [ ] Scroll works if needed

### 7.2 Special Characters

- [ ] Test paths with spaces
- [ ] Test paths with special chars (@, #, etc.)
- [ ] Verify handling

### 7.3 Large Number of Items

- [ ] Load manifest/config with 50+ items
- [ ] Scroll works correctly
- [ ] Performance is acceptable
- [ ] All items accessible

### 7.4 Rapid Input

- [ ] Press keys rapidly
- [ ] No UI corruption
- [ ] Navigation stays correct
- [ ] No crashes

### 7.5 Screen Resize

- [ ] Resize terminal while TUI running
- [ ] UI adapts or handles gracefully
- [ ] No crashes

---

## Test Suite 8: End-to-End Workflows (30 min)

### 8.1 Complete Phase 1 Flow (Manual)

1. [ ] Launch TUI
2. [ ] Setup Sources → Manual
3. [ ] Select test manifest
4. [ ] Manage → Preparation Sources
5. [ ] Add one item of each type
6. [ ] Save manifest
7. [ ] Exit TUI
8. [ ] Verify saved file
9. [ ] Run CLI prepare command
10. [ ] Verify cache populated

### 8.2 Complete Phase 1 Flow (Auto)

1. [ ] Launch TUI
2. [ ] Setup Sources → Auto
3. [ ] Review detected sources
4. [ ] Confirm
5. [ ] Manage → Preparation Sources
6. [ ] Verify loaded data
7. [ ] Make small edit
8. [ ] Save
9. [ ] Exit TUI
10. [ ] Run CLI prepare command

### 8.3 Complete Phase 2 Flow (Manual)

1. [ ] Ensure Phase 1 complete
2. [ ] Launch TUI
3. [ ] Setup Build → Manual
4. [ ] Select config and client dir
5. [ ] Manage → Build Injections
6. [ ] Add items to each section
7. [ ] Save config
8. [ ] Exit TUI
9. [ ] Verify saved file
10. [ ] Run CLI inject command
11. [ ] Verify client updated

### 8.4 Complete Phase 2 Flow (Auto)

1. [ ] Ensure Phase 1 complete
2. [ ] Launch TUI
3. [ ] Setup Build → Auto
4. [ ] Review detected config
5. [ ] Confirm
6. [ ] Manage → Build Injections
7. [ ] Verify all sections
8. [ ] Make edits
9. [ ] Save
10. [ ] Exit TUI
11. [ ] Run CLI inject command

### 8.5 Full Pipeline Test

1. [ ] Clean environment
2. [ ] Phase 1: Auto setup and prepare
3. [ ] Phase 2: Auto setup and inject
4. [ ] Verify complete pipeline
5. [ ] Check all artifacts
6. [ ] Validate functionality

---

## Test Results Summary

### Statistics

- Total Test Cases: [Count from above]
- Passed: [Count]
- Failed: [Count]
- Blocked: [Count]
- Notes: [Any observations]

### Critical Issues Found

1. [Issue description]
2. [Issue description]

### High Priority Issues

1. [Issue description]
2. [Issue description]

### Medium/Low Priority Issues

1. [Issue description]
2. [Issue description]

### Performance Notes

- Startup time: [X seconds]
- Navigation responsiveness: [Good/Fair/Poor]
- Large list handling: [Good/Fair/Poor]
- Memory usage: [Observation]

### UI/UX Notes

- Visual clarity: [Good/Fair/Poor]
- Error messages: [Good/Fair/Poor]
- Navigation intuitiveness: [Good/Fair/Poor]
- Help text adequacy: [Good/Fair/Poor]

### Recommendations

1. [Recommendation]
2. [Recommendation]
3. [Recommendation]

---

## Sign-off

Tester: _______________

Date: _______________

Status: [ ] APPROVED  [ ] APPROVED WITH NOTES  [ ] NOT APPROVED

Notes:

---
