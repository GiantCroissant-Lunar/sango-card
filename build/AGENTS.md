# Build System Agent Instructions

This file contains build-specific agent instructions. For general project rules, see `../AGENTS.md`.

## Build System Rules

All agents working on the build system must follow these additional constraints:

### R-BUILD-LOCAL-FIRST: Local Build Verification Required

**Always ensure local builds succeed before moving to remote/CI builds.**

- **NEVER** push changes or trigger CI/CD without verifying local build success
- **ALWAYS** run `task clean && task build` locally before committing build changes
- **ALWAYS** test build scripts locally before modifying CI/CD workflows
- If local build fails, fix it before attempting remote builds
- Remote builds are expensive and should not be used for debugging

**Workflow:**

1. Make build system changes locally
2. Run `task clean` to ensure clean state
3. Run `task build` to verify build succeeds
4. Run `task test` if applicable
5. Only after local success: commit and push
6. Monitor CI/CD for environment-specific issues only

**Rationale:**

- Local builds are faster and cheaper than CI/CD
- Debugging build failures locally is more efficient
- Reduces CI/CD minutes usage and costs
- Prevents polluting build history with broken builds
- Enables faster iteration cycles

### Build System Context

- **Build Tool:** Nuke (.NET build automation)
- **Task Runner:** Task (Go-based task runner for convenience)
- **CI/CD:** GitHub Actions with self-hosted runners for Unity builds
- **Artifacts:** Versioned outputs in `build/_artifacts/{version}/`

### Related Rules

- **R-BLD-010:** Use Task runner for all build operations
- **R-BLD-030:** Verify build succeeds locally before committing
- **R-CICD-010:** Prefer Ubuntu runners for cost efficiency
- **R-CICD-020:** Use self-hosted runners for Unity builds

## Reference

For complete agent rules and project guidelines, see:

- **Root Agent Instructions:** `../AGENTS.md`
- **Base Rules:** `../.agent/base/20-rules.md`
- **Build System Rules:** `../.agent/base/20-rules.md` (R-BLD-xxx section)
