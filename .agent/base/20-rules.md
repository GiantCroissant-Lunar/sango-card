# Normative Rules (Canon)

(Use: Adapters cite "R-CODE-010" etc.)

## Code & Architecture
R-CODE-010: Prefer editing existing files over creating new ones unless explicitly required.
R-CODE-020: Do not fabricate file contents. If unsure about implementation details, request clarification.
R-CODE-030: Follow C# naming conventions: PascalCase for public members, camelCase with underscore prefix for private fields (e.g., `_myField`).
R-CODE-040: Unity-specific: Use `[SerializeField]` for inspector-visible private fields; avoid public fields except for constants.
R-CODE-050: Respect existing project structure. Do not create orphaned files outside the workspace hierarchy.
R-CODE-060: Separate business logic from MonoBehaviours when practical for testability.
R-CODE-070: Use dependency injection patterns for complex component interactions to enable testing.
R-CODE-080: Prefer composition over inheritance in Unity Component design.

## Unity-Specific Rules
R-UNITY-010: ScriptableObjects for static game data (cards, abilities, configurations). Never hardcode game data in scripts.
R-UNITY-020: Use Unity's lifecycle correctly: Awake for internal setup, Start for external dependencies, OnEnable/OnDisable for event subscriptions.
R-UNITY-030: Cache component references in Awake or Start. Never use `GetComponent<>()` in Update loop.
R-UNITY-040: Use object pooling for frequently instantiated objects (card visuals, particle effects, projectiles).
R-UNITY-050: UI must use Unity UI Toolkit (UIElements) for performance. Legacy uGUI only for compatibility.
R-UNITY-060: Scene organization: one main scene per game state, additive scenes for UI layers.
R-UNITY-070: Prefabs for all reusable GameObjects. Never duplicate objects in scene hierarchy.
R-UNITY-080: Asset organization: group by feature/system, not by type. Example: `Assets/Features/DeckBuilder/` not `Assets/Scripts/`.
R-UNITY-090: Use Addressables for runtime asset loading. Direct Resources.Load only for development/prototyping.
R-UNITY-100: Build targets: Standalone (Windows x64) for development, Android (ARM64) for production.

## Security
R-SEC-010: Never log, echo, or invent secrets. Use `<REDACTED>` placeholder in examples.
R-SEC-020: Do not embed credentials/tokens in code or documentation examples.
R-SEC-030: Do not log PII (Personally Identifiable Information). Redact sensitive fields by default.
R-SEC-040: External API calls must define timeout and retry/backoff policy in production code.
R-SEC-050: Encrypt save files using player-specific keys. Never store sensitive data in plain text PlayerPrefs.
R-SEC-060: Validate and sanitize all card definition data before loading into game systems.

## Testing
R-TST-010: Write unit tests for game logic (card rules, deck validation, combat systems) using Unity Test Framework.
R-TST-020: Unity tests use Edit Mode for logic, Play Mode for integration. Place in `Tests~` folders or separate assemblies.
R-TST-030: Flaky tests are quarantined (marked and tracked in issues) not deleted.
R-TST-040: Target >70% code coverage for game logic. UI code may have lower coverage.
R-TST-050: Mock Unity APIs (Time, Random, etc.) in unit tests for deterministic results.

## Performance
R-PERF-010: Profile before optimizing. Use Unity Profiler to identify actual bottlenecks.
R-PERF-020: Target 60 FPS on mid-range mobile devices (Snapdragon 700 series equivalent).
R-PERF-030: Minimize per-frame allocations. Use object pools, cache collections, avoid LINQ in hot paths.
R-PERF-040: Texture sizes must respect mobile memory limits. Use texture compression (ASTC for Android).
R-PERF-050: Shader variants must be controlled. Strip unused variants in build.
R-PERF-060: Lazy-load assets to reduce startup time. Target <5 second initial load on mid-range devices.

## Documentation
R-DOC-010: Only create documentation when explicitly requested. Do not create proactive documentation.
R-DOC-020: XML comments for public APIs. Include parameter descriptions, return values, and usage examples.
R-DOC-030: Update README files when adding new projects, tools, or setup steps.
R-DOC-040: Document non-obvious Unity-specific patterns (custom editors, build processors, etc.).

## Spec-Kit Integration
R-SPEC-010: All significant features MUST follow spec-kit workflow: specify → plan → tasks → implement.
R-SPEC-020: Never create specification files manually. Always use `/speckit.specify` command.
R-SPEC-030: Never bypass `/speckit.plan` for non-trivial features. Ad-hoc implementation only for bug fixes or tiny changes.
R-SPEC-040: Read and respect `.specify/memory/constitution.md` when making architectural decisions.
R-SPEC-050: When spec is ambiguous, use `/speckit.clarify` before planning. Do not assume requirements.
R-SPEC-060: Validate task coverage with `/speckit.analyze` before implementation for medium-to-large features.
R-SPEC-070: Reference spec number in commits (e.g., "#001: Implement daily rewards system").

## Git Workflow
R-GIT-010: Commit bodies MUST be authored from a file and passed via `git commit -F <file>`.
  - Do not include literal backslash-escaped newlines (e.g., `\n`) in `-m` arguments.
  - Subject line ≤ 72 chars; then a blank line; then Markdown body.
  - Include co-authorship footer:
    ```
    🤖 Generated with [Claude Code](https://claude.com/claude-code)

    Co-Authored-By: Claude <noreply@anthropic.com>
    ```
R-GIT-020: Do not commit files that likely contain secrets (.env, credentials.json, API keys).
R-GIT-030: Do not commit Unity Library/ folder. Use .gitignore correctly.
R-GIT-040: Commit Unity meta files alongside their assets. Never commit asset without corresponding .meta file.
R-GIT-050: Use meaningful branch names: `feature/{spec-number}-{name}`, `hotfix/{issue}`, `experimental/{name}`.

## Build System
R-BLD-010: Use Task runner for all build operations. Never run `dotnet`, `unity`, or `npm` directly.
R-BLD-020: Build commands:
  - `task setup` - Initial setup and dependency restore
  - `task build` - Build all projects
  - `task build:unity` - Build Unity project for target platform
  - `task test` - Run all tests
  - `task clean` - Clean build artifacts
R-BLD-030: Verify build succeeds locally before committing: `task clean && task build`.
R-BLD-040: Unity builds MUST be reproducible. No manual Build Settings changes - configure via build scripts.
R-BLD-050: Nuke build targets for CI/CD. Task targets for developer convenience.

## Process
R-PRC-010: When uncertain about architectural decisions, propose options rather than implementing immediately.
R-PRC-020: Do not duplicate full rule text in adapters; adapters cite rule IDs only.
R-PRC-030: Never renumber or reuse a retired rule ID; create a new ID for semantic changes.
R-PRC-040: For multi-step tasks, provide clear progress updates.
R-PRC-050: Escalate to human if constitution conflicts with technical requirements.

## Deprecated Rules
(None yet)
