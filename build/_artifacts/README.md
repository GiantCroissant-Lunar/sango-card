# Build Artifacts Directory

This directory contains versioned build artifacts following **R-BLD-070**.

## Structure

```
build/_artifacts/
├── .gitkeep
├── README.md
├── {version}/          # e.g., 1.0.0, 1.0.0-beta.1, 0.1.0-alpha.5
│   ├── SangoCard.apk   # Android build
│   ├── SangoCard.exe   # Windows standalone
│   ├── version.json    # Version manifest
│   └── ...
└── {version}/
    └── ...
```

## Versioning with GitVersion

Versions are determined by [GitVersion](https://gitversion.net/) based on Git history and commit messages.

### Branch Versioning

- **main**: Release versions (e.g., `1.0.0`, `1.2.3`)
- **develop**: Alpha versions (e.g., `1.0.0-alpha.1`)
- **feature/xyz**: Feature versions (e.g., `1.0.0-xyz.1`)
- **release/1.x**: Beta versions (e.g., `1.0.0-beta.1`)
- **hotfix/xyz**: Hotfix betas (e.g., `1.0.1-beta.1`)

### Semantic Versioning via Commits

Control version bumps with commit message tags:

```bash
# Major version bump (breaking changes)
git commit -m "feat: new API +semver:major"
# 1.0.0 → 2.0.0

# Minor version bump (new features)
git commit -m "feat: add card system +semver:minor"
# 1.0.0 → 1.1.0

# Patch version bump (bug fixes)
git commit -m "fix: resolve crash +semver:patch"
# 1.0.0 → 1.0.1

# No version bump
git commit -m "docs: update README +semver:none"
# 1.0.0 → 1.0.0
```

## Build Process

When building, scripts **MUST**:

1. Query GitVersion for current version:
   ```bash
   dotnet gitversion /showvariable SemVer
   # Output: 1.0.0-beta.1
   ```

2. Create versioned directory:
   ```bash
   mkdir -p build/_artifacts/1.0.0-beta.1
   ```

3. Build to versioned path:
   ```bash
   # Unity build outputs to:
   build/_artifacts/1.0.0-beta.1/SangoCard.apk
   ```

4. Generate version manifest:
   ```json
   {
     "version": "1.0.0-beta.1",
     "buildTime": "2025-10-16T16:40:00Z",
     "commit": "1ec20ff",
     "branch": "release/1.0"
   }
   ```

## Running Built Executables

**Always** use the versioned path:

```bash
# ✅ Correct
./build/_artifacts/1.0.0/SangoCard.exe

# ❌ Wrong - artifacts are never in root
./build/_artifacts/SangoCard.exe
```

## Example Usage

```bash
# Get current version
VERSION=$(dotnet gitversion /showvariable SemVer)

# Run the build
./build/_artifacts/${VERSION}/SangoCard.exe

# Compare versions
./build/_artifacts/1.0.0/SangoCard.exe &     # Old version
./build/_artifacts/1.0.1/SangoCard.exe &     # New version

# Archive old versions
tar -czf archives/1.0.0.tar.gz build/_artifacts/1.0.0/
```

## Benefits

- **Traceability**: Each build is tied to a specific version
- **Coexistence**: Multiple versions can exist simultaneously
- **Rollback**: Easy to test or deploy previous versions
- **CI/CD**: Automated versioning based on Git history
- **Testing**: Compare behavior across versions

## References

- **Rule**: R-BLD-070 in `.agent/base/20-rules.md`
- **Config**: `GitVersion.yml` in repository root
- **Docs**: https://gitversion.net/docs/
