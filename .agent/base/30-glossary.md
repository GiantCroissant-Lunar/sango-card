# Domain Glossary

## Card Game Terms

- **Card**: Basic game unit with attributes (cost, effect, rarity, faction)
- **Deck**: Collection of cards selected by player for battle (30-60 cards typical)
- **Hand**: Cards currently available to play (typically 5-10 cards)
- **Mana/Energy**: Resource used to play cards
- **Board**: Play area where cards are active
- **Graveyard/Discard**: Area for used or destroyed cards
- **Draw**: Taking card(s) from deck into hand
- **Mill**: Discarding cards from deck without playing
- **Buff/Debuff**: Temporary stat modifications
- **AOE (Area of Effect)**: Effect that targets multiple cards/units
- **RNG (Random Number Generator)**: Randomness in card effects
- **Meta**: Current popular strategies and deck archetypes

## Unity Terms

- **GameObject**: Base object in Unity scene hierarchy
- **Component**: Behavior attached to GameObject (MonoBehaviour, Transform, etc.)
- **ScriptableObject**: Data container that exists as project asset, not scene object
- **Prefab**: Reusable GameObject template stored as asset
- **Scene**: Container for game objects representing a game state or level
- **Asset**: File in project (texture, model, audio, script, etc.)
- **Inspector**: Unity editor panel showing GameObject/asset properties
- **Hierarchy**: Tree view of GameObjects in current scene
- **Project**: Asset browser showing all project files
- **Play Mode**: Running game in Unity Editor for testing
- **Edit Mode**: Editing scenes and assets in Unity Editor
- **Build**: Compiled game executable for target platform
- **Addressable**: Asset that can be loaded at runtime by address/label
- **Assembly Definition**: Defines code compilation boundary for faster iteration

## Build System Terms

- **Nuke**: .NET-based build automation tool (similar to Make, but C#-based)
- **Task**: Go-based task runner (modern alternative to Make)
- **Taskfile**: Configuration file for Task runner (Taskfile.yml)
- **Build Target**: Specific build operation (e.g., "build", "test", "clean")
- **Artifact**: Output of build process (executable, package, logs)
- **CI/CD**: Continuous Integration / Continuous Deployment
- **Pipeline**: Automated sequence of build/test/deploy steps

## Spec-Kit Terms

- **Spec**: Feature specification document created with `/speckit.specify`
- **Plan**: Implementation plan created with `/speckit.plan`
- **Tasks**: Actionable task breakdown created with `/speckit.tasks`
- **Constitution**: Project principles document at `.specify/memory/constitution.md`
- **Clarification**: Structured Q&A process via `/speckit.clarify`
- **Analysis**: Cross-artifact validation via `/speckit.analyze`
- **Checklist**: Quality validation via `/speckit.checklist`
- **Feature Number**: Sequential identifier for specs (e.g., 001, 002, 003)

## Development Terms

- **Hot Path**: Code executed frequently (e.g., every frame)
- **GC (Garbage Collection)**: Automatic memory management in C#/.NET
- **Pooling**: Reusing objects instead of creating/destroying to reduce GC pressure
- **Profiling**: Measuring performance to identify bottlenecks
- **Frame Budget**: Target time per frame (16.67ms for 60 FPS)
- **LOD (Level of Detail)**: Different asset quality levels based on distance/importance
- **Batching**: Combining multiple draw calls into one for performance
- **Culling**: Not rendering objects outside camera view
- **TDD (Test-Driven Development)**: Writing tests before implementation
- **ECS (Entity Component System)**: Data-oriented architecture pattern
- **SOLID**: OOP design principles (Single Responsibility, Open/Closed, etc.)

## Platform Terms

- **Mobile**: iOS and Android target platforms
- **Standalone**: Desktop builds (Windows, Mac, Linux)
- **ARM64**: 64-bit ARM processor architecture (modern mobile devices)
- **IL2CPP**: Unity's C# to C++ compiler for better performance and security
- **Mono**: Unity's legacy C# runtime (replaced by IL2CPP on most platforms)
