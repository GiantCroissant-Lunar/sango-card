# Windsurf Adapter

Base-Version-Expected: 1.0.0

References base rule set in `.agent/base/`. The base canon prevails on conflict.

## Windsurf Context

Windsurf (powered by Codeium) is an AI-native IDE with agentic coding capabilities, providing both inline suggestions and flow-based assistance for larger tasks.

## Interaction Style

- Be direct and actionable. Windsurf users expect rapid iteration.
- For multi-step refactoring, use Cascade mode to maintain context across edits.
- Cite rule IDs when relevant (e.g., "Following R-UNITY-010...").
- When uncertain about architecture, propose options and ask (R-PRC-010).

## Unity & C# Context

- Unity 6000.2.x mobile card game project
- C# with .NET Standard 2.1 compatibility
- Mobile-first: Android ARM64 target, 60 FPS requirement (R-PERF-020)
- Spec-driven development with spec-kit integration

## Code Generation

- Prefer editing existing files over creating new ones (R-CODE-010)
- Never fabricate implementation details - ask for clarification (R-CODE-020)
- Follow C# conventions: PascalCase public, _camelCase private (R-CODE-030)
- Use `[SerializeField]` for Unity inspector fields (R-CODE-040)
- Respect project structure; avoid orphaned files (R-CODE-050)
- **Scoped configuration files** (R-CODE-100):
  - When creating config files (.gitignore, .editorconfig, etc.), prefer scoped/local configs
  - Place configs in relevant subdirectories near files they affect
  - Root configs only for truly global settings
- **Partial classes for multiple interfaces** (R-CODE-090):
  - Split classes with multiple interfaces into partial files
  - `ClassName.cs`: base class inheritance only
  - `ClassName.InterfaceName.cs`: each interface separately (without 'I' prefix)
  - Interface members in interface-specific file
  - Suggest this pattern when creating build components or multi-interface classes

## Unity-Specific Behavior

- **Data**: ScriptableObjects for all game data (cards, abilities, configs) (R-UNITY-010)
- **Lifecycle**: Awake for setup, Start for dependencies, OnEnable/OnDisable for events (R-UNITY-020)
- **Performance**: Cache component references in Awake, never GetComponent in Update (R-UNITY-030)
- **Pooling**: Use object pooling for instantiated objects (cards, VFX, projectiles) (R-UNITY-040)
- **UI**: UI Toolkit (UIElements) for all UI, not legacy uGUI (R-UNITY-050)
- **Organization**: Assets by feature/system, not by type (R-UNITY-080)

## Security Enforcement

- Never log or suggest secrets, tokens, or credentials (R-SEC-010, R-SEC-020)
- Redact PII in log suggestions (R-SEC-030)
- Include timeout/retry for external API calls (R-SEC-040)
- Encrypt save files with player-specific keys (R-SEC-050)

## Performance Awareness

- Profile before optimizing; suggest Unity Profiler usage (R-PERF-010)
- Target 60 FPS on mid-range mobile (Snapdragon 700 series) (R-PERF-020)
- Minimize per-frame allocations; suggest pooling and caching (R-PERF-030)
- Be mindful of mobile texture memory limits (R-PERF-040)

## Spec-Kit Integration

- **Workflow**: All significant features follow spec-kit (R-SPEC-010)
- **Commands**: Never create specs manually; use `/speckit.specify` (R-SPEC-020)
- **Planning**: Use `/speckit.plan` for non-trivial features (R-SPEC-030)
- **Constitution**: Read and respect `.specify/memory/constitution.md` (R-SPEC-040)
- **Clarity**: Use `/speckit.clarify` when requirements are ambiguous (R-SPEC-050)
- **Validation**: Suggest `/speckit.analyze` for medium-to-large features (R-SPEC-060)

## Testing

- Suggest Unity Test Framework for game logic (R-TST-010)
- Edit Mode for logic, Play Mode for integration (R-TST-020)
- Target >70% coverage for game logic (R-TST-040)
- Mock Unity APIs for deterministic tests (R-TST-050)

## Git Workflow

- Use `git commit -F <file>` for commit bodies (R-GIT-010)
- **Always use temporary commit message files** (R-GIT-060):
  - Create temporary file (e.g., `.git/COMMIT_EDITMSG_TEMP`)
  - Use `git commit -F <file>` not multiple `-m` flags
  - Clean up temp file after committing
  - Ensures proper multi-line formatting
- Include Windsurf co-authorship footer:

  ```
  🤖 Generated with Windsurf

  Co-Authored-By: Windsurf <noreply@codeium.com>
  ```

- Never commit secrets or Unity Library/ (R-GIT-020, R-GIT-030)
- Commit .meta files alongside assets (R-GIT-040)

## Build System

- Use Task runner for all operations (R-BLD-010):
  - `task setup` - Initial setup
  - `task build` - Build all
  - `task build:unity` - Unity build
  - `task test` - Run tests
  - `task clean` - Clean artifacts
- Verify build locally: `task clean && task build` (R-BLD-030)
- **CRITICAL**: Unity project at `projects/client` is isolated (R-BLD-060):
  - Never modify, view, edit, or suggest changes to files in `projects/client` outside build operations.
  - This directory is a standalone Git repo managed only during builds.
  - Build tasks must `git reset --hard` this directory before building.
- **Versioned Artifacts** (R-BLD-070):
  - All build outputs to `build/_artifacts/{version}/unity-output/{Platform}/`
  - Unity builds in unity-output/ container folder
  - Platform folders: Android/, iOS/, StandaloneWindows64/, StandaloneLinux64/, WebGL/
  - Additional folders: logs/, intermediate/ (at version root level)
  - Version from GitVersion
  - Create versioned directory with unity-output and subfolders before each build
  - Run executables: `build/_artifacts/{version}/unity-output/StandaloneWindows64/app.exe`
  - Build logs: `build/_artifacts/{version}/logs/unity-build.log`
  - Intermediate files: `build/_artifacts/{version}/intermediate/gradle/`

## Documentation

- Only create docs when explicitly requested (R-DOC-010)
- Suggest XML comments for public APIs (R-DOC-020)
- Update READMEs when adding structure (R-DOC-030)
- Document non-obvious Unity patterns (R-DOC-040)

## Windsurf-Specific Features

### Cascade Mode

When using Cascade for multi-file refactoring:

- Read constitution and relevant specs first
- Plan changes across files before executing
- Validate dependencies between edits
- Run tests after each logical grouping

### Inline Suggestions

- Suggest ScriptableObject when seeing hardcoded data
- Suggest caching when seeing repeated GetComponent calls
- Suggest pooling when seeing Instantiate/Destroy patterns
- Suggest async/await for I/O operations

### Chat Assistant

- Reference rule IDs in explanations
- Propose multiple architectural options
- Ask clarifying questions before large changes
- Link to relevant docs (constitution, specs, base rules)

## Common Unity Patterns

### Card Data Definition

```csharp
[CreateAssetMenu(fileName = "New Card", menuName = "Game/Card")]
public class CardDefinition : ScriptableObject
{
    [SerializeField] private string _cardName;
    [SerializeField] private int _manaCost;
    [SerializeField] private Sprite _artwork;
    // ... more fields
}
```

### Card Controller

```csharp
public class CardController : MonoBehaviour
{
    [SerializeField] private CardDefinition _definition;

    private Image _artworkImage;
    private TextMeshProUGUI _nameText;

    private void Awake()
    {
        // Cache components once
        _artworkImage = GetComponent<Image>();
        _nameText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        // Initialize with definition
        if (_definition != null)
        {
            _artworkImage.sprite = _definition.Artwork;
            _nameText.text = _definition.CardName;
        }
    }
}
```

### Object Pool

```csharp
public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Queue<T> _pool = new();

    public T Get()
    {
        if (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return Object.Instantiate(_prefab);
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }
}
```

## Mobile Considerations

- Always consider touch input (no hover states)
- Test UI on various aspect ratios (16:9, 18:9, 19.5:9, 20:9)
- Be mindful of battery drain (limit particles, CPU-intensive tasks)
- Plan for interrupted gameplay (calls, app switching)
- Respect memory constraints (texture sizes, object counts)
- Use TextMeshPro with SDF shaders for crisp text

## Anti-Patterns to Avoid

1. Public fields on MonoBehaviours (use [SerializeField] + private)
2. GetComponent in Update/FixedUpdate (cache in Awake)
3. String-based lookups (use enum or ScriptableObject references)
4. FindObjectOfType in performance-critical paths
5. Singleton abuse (prefer DI or ScriptableObject events)
6. GameObject.Find by name (use direct references or tags)
7. Resources.Load at runtime (use Addressables)
8. Legacy uGUI for new UI (use UI Toolkit)
9. Hardcoded game data in scripts (use ScriptableObjects)
10. Ignoring mobile performance (profile early and often)
