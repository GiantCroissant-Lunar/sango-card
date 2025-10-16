# GitHub Copilot Adapter

Base-Version-Expected: 1.0.0

References base rule set in `.agent/base/`. The base canon prevails on conflict.

## Copilot Context

GitHub Copilot provides code suggestions in editor and chat-based assistance. This adapter configures how Copilot should behave in the Sango Card Unity 6000.2.x project.

## Code Suggestions

- Prefer editing existing code over generating new files (R-CODE-010).
- Follow C# conventions: PascalCase public, camelCase private with underscore (R-CODE-030).
- Use `[SerializeField]` for Unity inspector fields (R-CODE-040).
- Suggest object pooling for frequently instantiated objects (R-UNITY-040).
- Cache component references, avoid GetComponent in loops (R-UNITY-030).
- **Scoped configuration files** (R-CODE-100):
  - When creating config files (.gitignore, .editorconfig, etc.), prefer scoped/local configs
  - Place configs near the files they affect
  - Root configs only for truly global settings
- **Partial classes for multiple interfaces** (R-CODE-090):
  - Suggest separate files for each interface implementation
  - Base file: `ClassName.cs` with parent class only
  - Interface files: `ClassName.InterfaceName.cs` (without 'I' prefix) for each interface
  - Example: `Build.cs` → `partial class Build : NukeBuild`, `Build.UnityBuild.cs` → `partial class Build : IUnityBuild`
  - Interface members should be implemented in the interface-specific file

## Unity Patterns

When suggesting Unity code:

- ScriptableObjects for game data (cards, configs) (R-UNITY-010).
- Proper lifecycle usage: Awake for setup, Start for dependencies (R-UNITY-020).
- UI Toolkit (UIElements) for UI, not legacy uGUI (R-UNITY-050).
- Addressables for runtime loading (R-UNITY-090).

## Security

- Never suggest hardcoded credentials or API keys (R-SEC-020).
- Redact sensitive data in logs (R-SEC-030).
- Include timeout/retry logic for external calls (R-SEC-040).
- Suggest encrypted save files, not plain PlayerPrefs (R-SEC-050).

## Performance

- Avoid per-frame allocations in suggestions (R-PERF-030).
- Suggest pooling for common patterns (particle effects, UI elements).
- Use TextMeshPro over legacy Text (mobile performance).
- Avoid LINQ in hot paths (Update, FixedUpdate).

## Testing

- Suggest test methods when creating game logic (R-TST-010).

## Git Workflow

- **Commit via file** (R-GIT-060):
  - Always create temporary file for commit messages
  - Use `git commit -F <file>` not multiple `-m` flags
  - Clean up temp file after committing
  - Ensures proper formatting and multi-line support
- Use Unity Test Framework patterns (R-TST-020).
- Mock Unity APIs in test suggestions (R-TST-050).

## Spec-Kit Awareness

When user mentions feature work:

- Remind about spec-kit workflow if not already using it (R-SPEC-010).
- Suggest `/speckit.specify` for new features (R-SPEC-020).
- Suggest `/speckit.clarify` when requirements seem ambiguous (R-SPEC-050).

## Documentation

- Suggest XML comments for public APIs (R-DOC-020).
- Do not auto-generate documentation files (R-DOC-010).
- Keep comments concise and focused on "why" not "what".

## Git Practices

- Include meaningful commit message suggestions.
- Remind about co-authorship footer for AI assistance (R-GIT-010).
- Flag potential secrets in diff preview (R-GIT-020).

## Build Commands

When user asks about building:

- Suggest `task build` not direct `dotnet` or Unity commands (R-BLD-010).
- Remind about local verification: `task clean && task build` (R-BLD-030).
- **Versioned Artifacts** (R-BLD-070):
  - Build outputs: `build/_artifacts/{version}/unity-output/{Platform}/`
  - Unity builds in unity-output/, logs in logs/, intermediate in intermediate/
  - Suggest querying GitVersion for current version
  - Executable paths: `build/_artifacts/{version}/unity-output/StandaloneWindows64/app.exe`
  - Log paths: `build/_artifacts/{version}/logs/build.log`
  - Example: `build/_artifacts/1.0.0/unity-output/Android/SangoCard.apk`
- **CRITICAL**: Never modify `projects/client` Unity project outside build operations (R-BLD-060).
  - This directory is a standalone Git repo managed only during builds.
  - Do not view, edit, suggest changes, or commit anything in this directory.
  - Build tasks must `git reset --hard` this directory before building.

## Inline Chat Guidance

When providing explanations in chat:

- Cite rule IDs for clarity (e.g., "This follows R-UNITY-010...").
- Reference constitution for strategic questions.
- Propose multiple options for architectural decisions (R-PRC-010).
- Be concise, avoid verbose explanations unless asked.

## Platform Considerations

- Assume mobile (Android ARM64) as primary target.
- Consider touch input in UI suggestions.
- Be mindful of mobile performance constraints.
- Suggest responsive UI that scales across aspect ratios.

## Common Patterns to Suggest

1. **Card Data**: ScriptableObject with serialized fields
2. **Card Controller**: MonoBehaviour with cached references
3. **Object Pool**: Generic pool manager for reusable objects
4. **Save System**: Encrypted JSON serialization
5. **Event System**: ScriptableObject-based events for decoupling
6. **State Machine**: Enum-based with switch for card/game states
7. **Dependency Injection**: Constructor injection for testable components
8. **Factory Pattern**: For creating card instances from definitions

## Anti-Patterns to Avoid

1. Public fields on MonoBehaviours (use SerializeField + private)
2. GetComponent in Update loop (cache in Awake)
3. String-based lookups (use enum or ScriptableObject references)
4. FindObjectOfType in performance-critical code
5. Singleton pattern abuse (prefer DI or ScriptableObject events)
6. GameObject.Find by name (use references or tags)
7. Resources.Load at runtime (use Addressables)
8. Legacy uGUI for new features (use UI Toolkit)
