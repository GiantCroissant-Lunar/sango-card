# Core Principles

P-1: **Unity-First Development** - Follow Unity best practices and mobile optimization patterns. Profile before optimizing.

P-2: **Data-Driven Design** - Use ScriptableObjects for game data. Keep logic separate from data.

P-3: **Spec-Driven Features** - All significant features follow spec-kit workflow. Never bypass the process.

P-4: **Performance Consciousness** - Target 60 FPS on mid-range mobile devices. Minimize GC allocations.

P-5: **Player-Centric Design** - Player experience takes precedence over developer convenience. Test on actual devices.

P-6: **Security Posture** - Never commit secrets or credentials. Validate all external data. Encrypt save files.

P-7: **Testable Architecture** - Separate business logic from MonoBehaviours. Write unit tests for game rules.

P-8: **Maintainability Over Cleverness** - Clear code beats clever code. Document non-obvious decisions.

P-9: **Build Automation First** - All build and deployment must be reproducible through Task/Nuke pipeline.

P-10: **When in doubt, ask** - Clarify requirements before implementing. Use `/speckit.clarify` when ambiguous.
