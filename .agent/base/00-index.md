# Agent Instruction Base
Version: 1.0.0
Source of Truth for all automated assistant behavior in Sango Card project.

## Project Context
- **Type**: Unity card game project (Unity 2022.3 LTS, C#, .NET Standard 2.1)
- **Phase**: Active development with rapid iteration
- **Focus**: Mobile-first card game with performant UI, data-driven design, and spec-driven development methodology
- **Build System**: Nuke (.NET build automation) + Task (Go-based task runner)

## Composition
- 10-principles.md: Core philosophy for Unity game development
- 20-rules.md: Normative, enforceable rules (ID-based)
- 30-glossary.md: Domain terms (card game, Unity, build system)

Adapters (in ../adapters) must reference **rule IDs** instead of copying rule text.

## Adapter Sync & Versioning
- Adapters MUST declare `Base-Version-Expected:`. If it doesn't match this file's `Version`, adapters should **fail closed** (ask for upgrade).
- Pointer files (e.g., CLAUDE.md) should redirect agents to this canon and the agent-specific adapter.

All adapters must enforce documentation conventions and spec-kit integration.

## Naming Conventions (Documents)

### Spec-Kit Artifacts
- **Specifications**
  - Location: `.specify/specs/` (managed by spec-kit)
  - Filename: `{number}-{feature-name}/spec.md` (e.g., `001-daily-rewards/spec.md`)
  - Created via `/speckit.specify` command
  - Never create manually - always use spec-kit workflow
  
- **Implementation Plans**
  - Location: `.specify/specs/{number}-{feature-name}/plan.md`
  - Created via `/speckit.plan` command
  - Includes data models, API contracts, architecture decisions
  
- **Task Breakdowns**
  - Location: `.specify/specs/{number}-{feature-name}/tasks.md`
  - Created via `/speckit.tasks` command
  - Ordered, dependency-aware task list

### General Documentation
- **README Files**
  - Location: Project root and major directories
  - Purpose: Onboarding, quick reference, structure overview
  
- **Technical Guides**
  - Location: `docs/` directory
  - Lowercase filenames with hyphens (e.g., `unity-setup.md`)
  - Organized by topic or component

### Unity Naming
- **Scenes**: PascalCase (e.g., `MainMenu`, `BattleScene`)
- **Prefabs**: PascalCase with descriptive names (e.g., `CardUI`, `DeckBuilder`)
- **ScriptableObjects**: PascalCase with suffix (e.g., `CardDefinition`, `AbilityConfig`)
- **Scripts**: PascalCase matching class name (e.g., `CardController.cs`)

Rules:
- Use spec-kit workflow for all feature development (never bypass)
- Lowercase on disk for portability in docs; use PascalCase for Unity assets
- Once published, spec numbers are immutable

## Change Policy
- **Add rule**: append with a new unique ID; never repurpose IDs.
- **Deprecate rule**: mark "DEPRECATED" but keep the ID (do not delete).
- **Major version bump** if any backward-incompatible change (removal or semantics shift). Minor bump for additive rules or clarifications.

## Constitution Integration
This rule system complements but does not replace `.specify/memory/constitution.md`. The constitution defines high-level principles and project philosophy, while this rule system provides specific, enforceable technical guidelines.

When conflicts arise:
1. Constitution principles take precedence for strategic decisions
2. Agent rules take precedence for tactical implementation details
3. Escalate to human if genuine conflict exists
