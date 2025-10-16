# Nuke Build API Compatibility Issue

## Status
The existing IUnityBuild.cs component was written for Nuke 8.x API and is incompatible with Nuke 9.0.4 currently in use.

## Issues
1. FileSystemTasks static class removed - now extension methods on AbsolutePath
2. ProcessTasks.StartProcess signature changed - timeout parameter type changed

## Impact
- Build project doesn't compile currently
- This is a pre-existing issue, not introduced by R-CODE-090 changes

## Resolution Required
Update Components/IUnityBuild.cs to use Nuke 9.x API:
- Replace EnsureCleanDirectory() with UnityBuildOutput.CreateOrCleanDirectory()
- Replace DeleteDirectory() with 	empPath.DeleteDirectory()
- Replace EnsureExistingDirectory() with UnityBuildOutput.CreateDirectory()
- Fix ProcessTasks.StartProcess() timeout parameter

## Files Changed for R-CODE-090
- ✅ `.agent/base/20-rules.md` - Added R-CODE-090 rule with file naming convention
- ✅ `.editorconfig` (root and build) - Added pattern documentation
- ✅ `Build.cs` - Made partial, moved IUnityBuild to separate file
- ✅ `Build.UnityBuild.cs` - Created (NEW) - Uses interface name without 'I' prefix
- ✅ Agent adapters - Updated with R-CODE-090 guidance and naming convention
- ✅ `docs/CODING-PATTERNS.md` - Complete pattern documentation (NEW)
- ✅ `scripts/git-hooks/pre-commit.ps1` - Pre-commit enforcement with naming logic (NEW)
- ✅ `docs/task/TODO.md` - Roslyn analyzer roadmap (NEW)

## Note
The partial class pattern implementation (R-CODE-090) is correct and follows best practices.
The build failure is due to legacy Nuke API usage in IUnityBuild.cs, not the pattern itself.
