# Claude Code Adapter

Base-Version-Expected: 1.0.0

References base rule set in `.agent/base/`. The base canon prevails on conflict.

## Interaction Style

- Be concise and direct. Minimize preamble and postamble (per AGENTS.md).
- For multi-step tasks, track progress internally. Only report milestones.
- Cite rule IDs when explaining constraints or decisions (e.g., "Following R-UNITY-010...").
- When uncertain about architecture, propose options rather than implementing (R-PRC-010).

## Unity & C# Context

- This is a Unity 6000.2.x mobile card game project.
- Respect Unity and C# naming conventions (R-CODE-030, R-CODE-040).
- Understand Unity lifecycle methods and mobile optimization patterns.
- Target .NET Standard 2.1 compatibility.
- Mobile-first: assume ARM64 Android as primary target, 60 FPS requirement.

## Code Generation

- Always prefer editing existing files over creating new ones (R-CODE-010).
- Do not fabricate implementation details (R-CODE-020).
- Follow existing project structure; avoid orphaned files (R-CODE-050).
- Separate game logic from MonoBehaviours for testability (R-CODE-060).
- **Scoped configuration files** (R-CODE-100):
  - When creating config files (.gitignore, .editorconfig, etc.), prefer scoped/local configs
  - Place configs in relevant subdirectories near files they affect
  - Root configs only for truly global settings
- **Partial classes for multiple interfaces** (R-CODE-090):
  - When creating/modifying classes with multiple interfaces, use separate partial files
  - Base file contains only direct parent class inheritance
  - Each interface in its own `ClassName.InterfaceName.cs` file (without 'I' prefix)
  - Interface members implemented in interface-specific file
  - Build classes follow this pattern strictly (see Build.cs + Build.UnityBuild.cs)

## Unity-Specific Behavior

- Use ScriptableObjects for all game data: cards, abilities, configs (R-UNITY-010).
- Cache component references, never GetComponent in Update (R-UNITY-030).
- Object pooling for frequently instantiated objects (R-UNITY-040).
- UI Toolkit (UIElements) for all UI, not legacy uGUI (R-UNITY-050).
- Organize assets by feature/system, not by type (R-UNITY-080).

## Security Enforcement

- Never log or display secrets, tokens, or credentials (R-SEC-010, R-SEC-020).
- Do not log PII without explicit redaction (R-SEC-030).
- Always define timeouts for external calls (R-SEC-040).
- Encrypt save files with player-specific keys (R-SEC-050).

## Performance Awareness

- Profile before optimizing using Unity Profiler (R-PERF-010).
- Target 60 FPS on mid-range mobile (R-PERF-020).
- Minimize per-frame allocations, use pooling (R-PERF-030).
- Respect mobile texture memory limits (R-PERF-040).

## Spec-Kit Integration

- ALL significant features follow spec-kit workflow (R-SPEC-010).
- Never create specs manually, use `/speckit.specify` (R-SPEC-020).
- When ambiguous, use `/speckit.clarify` before planning (R-SPEC-050).
- Read constitution at `.specify/memory/constitution.md` for context (R-SPEC-040).
- Reference spec numbers in commits (R-SPEC-070).

## Testing

- Write unit tests for game logic using Unity Test Framework (R-TST-010).
- Edit Mode for logic tests, Play Mode for integration tests (R-TST-020).
- Target >70% coverage for game logic (R-TST-040).
- Mock Unity APIs for deterministic tests (R-TST-050).

## Git Workflow

- Use `git commit -F <file>` for commit bodies with proper formatting (R-GIT-010).
- **Always use temporary commit message files** (R-GIT-060):
  - Create temporary file (e.g., `.git/COMMIT_EDITMSG_TEMP`)
  - Use `git commit -F <file>` not multiple `-m` flags
  - Clean up temp file after committing
  - Ensures proper multi-line formatting
- Include Claude co-authorship footer in commits.
- Never commit secrets or Unity Library/ folder (R-GIT-020, R-GIT-030).
- Commit meta files alongside assets (R-GIT-040).

## Build System

- Use Task runner for all operations (R-BLD-010):
  - `task setup` - Initial setup
  - `task build` - Build all
  - `task build:unity` - Unity build
  - `task test` - Run tests
  - `task clean` - Clean artifacts
- Verify build locally before committing: `task clean && task build` (R-BLD-030).
- **CRITICAL**: Unity project at `projects/client` is isolated (R-BLD-060):
  - Never modify, view, edit, or suggest changes to files in `projects/client` outside build operations.
  - This directory is a standalone Git repo managed only during builds.
  - Build tasks must `git reset --hard` this directory before building.
- **Versioned Artifacts** (R-BLD-070):
  - All build outputs go to `build/_artifacts/{version}/`
  - Version determined by GitVersion
  - Query version before building, create versioned directory
  - Run executables from versioned path: `build/_artifacts/{version}/app.exe`
  - Never place artifacts in root `build/_artifacts/` directory

## Documentation

- Only create docs when explicitly requested (R-DOC-010).
- XML comments for public APIs (R-DOC-020).
- Update READMEs when adding structure (R-DOC-030).
- Document non-obvious Unity patterns (R-DOC-040).

## Extended Context Strategy

- Claude can handle longer reasoning; still cite rule IDs for traceability.
- Use context efficiently: read spec/constitution first, then act.
- When working with Unity, understand scene structure before making changes.
- For large features, break into smaller, testable units.

## Mobile Game Specifics

- Always consider touch input (no mouse hover states).
- Test UI scaling on various aspect ratios (16:9, 18:9, 19.5:9).
- Be mindful of battery drain (avoid excessive particle effects, CPU-intensive tasks).
- Plan for interrupted gameplay (phone calls, app switching).
- Respect mobile memory constraints (texture sizes, object counts).
