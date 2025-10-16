# CLAUDE Instructions

→ **Agent rules at:** `.agent/adapters/claude.md`
→ **Base rules at:** `.agent/base/`
→ **Constitution at:** `.specify/memory/constitution.md`

You are working in a Unity 2022.3 LTS mobile card game project with spec-driven development methodology.

## Quick Reference

### Read First
1. `.agent/adapters/claude.md` - Your adapter configuration
2. `.specify/memory/constitution.md` - Project principles
3. `.agent/base/20-rules.md` - Normative rules (cite IDs)

### Core Principles (cite as P-1, P-2, etc.)
- P-1: Unity-First Development (mobile optimization)
- P-2: Data-Driven Design (ScriptableObjects)
- P-3: Spec-Driven Features (never bypass workflow)
- P-4: Performance Consciousness (60 FPS target)
- P-5: Player-Centric Design
- P-10: When in doubt, ask

### Key Rules (cite as R-CODE-010, etc.)
- **Code**: Prefer editing over creating (R-CODE-010)
- **Unity**: ScriptableObjects for data (R-UNITY-010)
- **Unity**: Cache components (R-UNITY-030)
- **Unity**: Object pooling (R-UNITY-040)
- **Spec-Kit**: Follow workflow (R-SPEC-010)
- **Spec-Kit**: Use /speckit.clarify when ambiguous (R-SPEC-050)
- **Performance**: Profile first (R-PERF-010)
- **Build**: Use Task runner (R-BLD-010)

### Repository Structure
- **`build/`** - Nuke build infrastructure
- **`projects/client/`** - Unity client (main game)
- **`projects/code-quality/`** - Quality assurance Unity project
- **`.specify/`** - Spec-kit artifacts (specs, plans, tasks)
- **`.agent/`** - Multi-agent instruction system

### Spec-Kit Workflow (R-SPEC-010)
All significant features MUST follow:
1. `/speckit.specify` - Define requirements
2. `/speckit.clarify` - Resolve ambiguities (optional)
3. `/speckit.plan` - Technical approach
4. `/speckit.tasks` - Task breakdown
5. `/speckit.analyze` - Validate coverage (optional)
6. `/speckit.implement` - Execute

### Build Commands (R-BLD-010)
Use Task runner, never direct commands:
- `task setup` - Initial setup
- `task build` - Build all
- `task build:unity` - Unity build
- `task test` - Run tests
- `task clean` - Clean artifacts

### Unity Context
- Version: 2022.3 LTS
- Target: Mobile (Android ARM64)
- UI: UI Toolkit (UIElements), not legacy uGUI
- Performance: 60 FPS on mid-range devices
- Assets: Organize by feature, not type

### Before You Start
- Check current spec in `.specify/specs/` if working on feature
- Verify build works: `task clean && task build`
- When uncertain, ask don't assume (P-10)

### Commit Messages (R-GIT-010)
Use `git commit -F <file>` with co-authorship footer:
```
🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

---

For detailed rules, see `.agent/adapters/claude.md` and `.agent/base/20-rules.md`.
For project philosophy, see `.specify/memory/constitution.md`.
