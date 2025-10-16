# Version Sync Protocol

This document defines how agents synchronize with the base rule version.

## Version Declaration

Every adapter MUST declare expected base version:

```markdown
# {Agent Name} Adapter
Base-Version-Expected: 1.0.0
```

## Sync Check

When an agent starts working in the repository:

1. **Read** `.agent/base/00-index.md` to get current `Version:`
2. **Compare** with adapter's `Base-Version-Expected:`
3. **Act** based on comparison result

## Comparison Outcomes

### ✅ Exact Match
Version matches exactly → Proceed normally.

### ⚠️ Minor/Patch Mismatch
Base version is higher but MAJOR matches (e.g., base 1.2.0, adapter expects 1.0.0):
- **Warning**: Inform user that adapter may not know about new rules
- **Action**: Continue with caution, suggest adapter update
- **Example**: "Note: Rule base updated to 1.2.0, this adapter expects 1.0.0. Some rules may be missing from adapter guidance."

### ❌ Major Mismatch
MAJOR version differs (e.g., base 2.0.0, adapter expects 1.0.0):
- **Error**: Fail closed - do not proceed
- **Action**: Request human intervention to update adapter
- **Example**: "Error: Rule base is 2.0.0 but adapter expects 1.0.0. Major version mismatch indicates incompatible changes. Please update adapter before continuing."

### 🔄 Base Older Than Adapter
Adapter expects higher version than base (unusual):
- **Warning**: Base rules may be out of date
- **Action**: Request user to update base rules
- **Example**: "Warning: Adapter expects 1.2.0 but base is 1.0.0. Base rules may need updating."

## Update Procedure

### Updating Adapters

1. Open `.agent/adapters/{agent}.md`
2. Update `Base-Version-Expected:` to current base version
3. Review new rules in `.agent/base/20-rules.md`
4. Add agent-specific guidance for new rules if needed
5. Test adapter behavior
6. Commit with message: `chore(agent): sync {agent} adapter to base v{version}`

### Updating Base Rules

1. Make changes to `.agent/base/20-rules.md`
2. Increment version in `.agent/base/00-index.md` following semver
3. Document changes in `.agent/meta/changelog.md`
4. Update all adapters' `Base-Version-Expected` if MAJOR change
5. Commit with message: `feat(agent): {description}` or `chore(agent): {description}`

## Semantic Versioning

Following [SemVer 2.0.0](https://semver.org/):

- **MAJOR** (X.0.0): Incompatible API changes (remove rules, change IDs, alter semantics)
- **MINOR** (1.X.0): Add functionality (new rules, new categories)
- **PATCH** (1.0.X): Bug fixes (typos, clarifications, examples)

## Rule ID Immutability

Rule IDs are immutable once published:
- **Never renumber** existing rules
- **Never reuse** retired rule IDs
- **Deprecate** instead of removing (mark "DEPRECATED")
- **Add new** rules with new IDs only

## Example Version History

```
1.0.0 - Initial release
1.1.0 - Added R-PERF-xxx performance rules
1.1.1 - Fixed typo in R-CODE-030 description
1.2.0 - Added R-SPEC-xxx spec-kit integration rules
2.0.0 - Removed deprecated R-OLD-xxx rules, restructured categories
```

## Upgrade Path

When base version increases:

### Patch Update (1.0.0 → 1.0.1)
- Adapters continue working without changes
- Optional: Update adapters to reflect clarifications

### Minor Update (1.0.0 → 1.1.0)
- Adapters continue working (additive changes)
- Recommended: Update adapters to reference new rules
- Timeline: Within 1 week

### Major Update (1.0.0 → 2.0.0)
- Adapters MUST be updated before use
- Required: Update all adapters immediately
- Timeline: Before any work continues

## Automated Checks (Future)

Consider implementing:
- Pre-commit hook to verify version consistency
- CI check that all adapters reference current MAJOR version
- Script to audit adapter/base version mismatches
