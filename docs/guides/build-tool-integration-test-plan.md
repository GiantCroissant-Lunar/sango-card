# Wave 3.3: Integration Testing & Polish - Test Plan

**Duration**: 4 hours
**Goal**: End-to-end testing of all 8 TUI views with complete Phase 1 & 2 workflows

## Test Coverage

### Phase 1: All 8 Views Functional Tests (1.5 hours)

#### 1. Main Menu View ✅
- [ ] Menu renders correctly
- [ ] All menu items are selectable
- [ ] Navigation works (arrow keys + enter)
- [ ] Exit functionality works

#### 2. Config Type Selection View ✅
- [ ] Config type selection works
- [ ] Navigation back to main menu works
- [ ] Transitions to correct downstream views

#### 3. Manual Sources View ✅
- [ ] File selection works
- [ ] Path validation works
- [ ] Directory traversal works
- [ ] Selection confirms properly

#### 4. Auto Sources View ✅
- [ ] Auto-detection displays results
- [ ] Package listing works
- [ ] Confirmation works
- [ ] Navigation works

#### 5. Manual Build Config View ✅
- [ ] File selection works
- [ ] Client directory selection works
- [ ] Path validation works

#### 6. Auto Build Config View ✅
- [ ] Auto-detection works
- [ ] Results display correctly
- [ ] Confirmation works

#### 7. Preparation Sources Management View ✅
- [ ] Load manifest functionality
- [ ] Create new manifest
- [ ] Add/Edit/Remove items
- [ ] Save functionality
- [ ] Preview display

#### 8. Build Injections Management View ✅
- [ ] Load config functionality
- [ ] Create new config
- [ ] Section switching (Packages/Assemblies/Assets)
- [ ] Add/Edit/Remove per section
- [ ] Save functionality
- [ ] Preview display

### Phase 2: End-to-End Workflow Tests (1.5 hours)

#### Workflow 1: Complete Phase 1 Setup (Manual)
- [ ] Launch TUI
- [ ] Select "Setup Sources" → Manual
- [ ] Select preparation manifest file
- [ ] Go to "Manage → Preparation Sources"
- [ ] Add package source
- [ ] Add assembly source
- [ ] Add asset source
- [ ] Save manifest
- [ ] Run preparation command
- [ ] Verify cache contents

#### Workflow 2: Complete Phase 1 Setup (Auto)
- [ ] Launch TUI
- [ ] Select "Setup Sources" → Auto
- [ ] Review auto-detected sources
- [ ] Confirm selection
- [ ] Go to "Manage → Preparation Sources"
- [ ] Review loaded manifest
- [ ] Make edits if needed
- [ ] Save manifest
- [ ] Run preparation command
- [ ] Verify cache contents

#### Workflow 3: Complete Phase 2 Setup (Manual)
- [ ] Launch TUI (with Phase 1 complete)
- [ ] Select "Setup Build" → Manual
- [ ] Select preparation config file
- [ ] Select client directory
- [ ] Go to "Manage → Build Injections"
- [ ] Add package injection
- [ ] Add assembly injection
- [ ] Add asset copy operation
- [ ] Save config
- [ ] Run injection command
- [ ] Verify client changes

#### Workflow 4: Complete Phase 2 Setup (Auto)
- [ ] Launch TUI (with Phase 1 complete)
- [ ] Select "Setup Build" → Auto
- [ ] Review auto-detected config
- [ ] Confirm selection
- [ ] Go to "Manage → Build Injections"
- [ ] Review loaded config
- [ ] Make edits if needed
- [ ] Save config
- [ ] Run injection command
- [ ] Verify client changes

#### Workflow 5: Round-Trip Test (Full Pipeline)
- [ ] Clean environment
- [ ] Phase 1: Auto-detect sources → Run preparation
- [ ] Phase 2: Auto-detect config → Run injection
- [ ] Verify complete pipeline works
- [ ] Check all artifacts in place

### Phase 3: Error Handling & Edge Cases (0.5 hours)

#### Error Scenarios
- [ ] Invalid file paths
- [ ] Missing directories
- [ ] Corrupted JSON files
- [ ] Empty manifests/configs
- [ ] Duplicate entries
- [ ] Permission errors
- [ ] Cancellation handling
- [ ] Invalid JSON in configs

#### Edge Cases
- [ ] Very long file paths
- [ ] Special characters in paths
- [ ] Large number of items (50+)
- [ ] Empty sections
- [ ] Rapid navigation/input
- [ ] Screen resize during operation

### Phase 4: Polish & Bug Fixes (0.5 hours)

#### UI/UX Polish
- [ ] Consistent navigation patterns
- [ ] Clear status messages
- [ ] Proper error messages
- [ ] Loading indicators
- [ ] Smooth transitions
- [ ] Help text accuracy

#### Performance
- [ ] Fast startup time
- [ ] Responsive navigation
- [ ] Efficient rendering
- [ ] No memory leaks

## Test Environment Setup

### Prerequisites
```bash
# Build the tool
task build

# Create test directories
mkdir -p test-data/phase1-manual
mkdir -p test-data/phase1-auto
mkdir -p test-data/phase2-manual
mkdir -p test-data/phase2-auto
mkdir -p test-data/client-test
```

### Test Data Files

Create sample manifests and configs for testing:

1. **test-manifest-minimal.json** - Minimal valid manifest
2. **test-manifest-full.json** - Full featured manifest
3. **test-manifest-invalid.json** - Invalid JSON for error testing
4. **test-config-minimal.json** - Minimal valid config
5. **test-config-full.json** - Full featured config
6. **test-config-invalid.json** - Invalid JSON for error testing

## Success Criteria

- ✅ All 8 views render and function correctly
- ✅ All 5 end-to-end workflows complete successfully
- ✅ Error handling works for all scenarios
- ✅ Edge cases handled gracefully
- ✅ No crashes or hangs
- ✅ Performance is acceptable (<2s for all operations)
- ✅ User experience is smooth and intuitive

## Bug Tracking

### Critical Bugs (Must Fix)
- [ ] None found yet

### High Priority (Should Fix)
- [ ] None found yet

### Medium Priority (Nice to Fix)
- [ ] None found yet

### Low Priority (Future Enhancement)
- [ ] None found yet

## Test Log

### Session 1: [Date/Time]
- Tester: [Name]
- Environment: [OS/Terminal]
- Results: [Summary]

---

**Next Steps After Testing:**
1. Fix all critical and high priority bugs
2. Update documentation based on findings
3. Create user guide with screenshots
4. Proceed to Wave 4: Documentation
