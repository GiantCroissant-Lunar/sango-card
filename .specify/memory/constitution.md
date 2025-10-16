# Sango Card Project Constitution

## Core Principles

### I. Unity-First Development

All game logic and systems are built with Unity best practices in mind:

- Favor ScriptableObjects for data-driven design
- Use Unity's Component-based architecture appropriately
- Leverage Unity's lifecycle (Awake, Start, Update) patterns correctly
- Profile and optimize for target platforms (mobile-first approach)
- Maintain scene organization and prefab reusability

### II. Code Quality and Maintainability

Code must be clean, maintainable, and follow C# conventions:

- Follow C# naming conventions (PascalCase for public members, camelCase for private)
- Keep classes focused with single responsibility principle
- Document public APIs with XML comments
- Use async/await for I/O operations
- Implement proper error handling and logging
- Avoid magic numbers - use named constants or configuration

### III. Performance Consciousness

Card games require responsive, smooth gameplay:

- Target 60 FPS on mid-range mobile devices
- Minimize garbage collection through object pooling where appropriate
- Use Unity's UI Toolkit for efficient UI rendering
- Profile before optimizing - measure, don't guess
- Lazy-load assets to reduce startup time
- Cache frequently accessed components and references

### IV. Testable Architecture

Systems should be designed for testability:

- Separate business logic from MonoBehaviour when practical
- Use dependency injection for testing complex interactions
- Write unit tests for game rules and logic
- Integration tests for critical user flows
- Validate data consistency with automated checks

### V. Build Automation First

All build and deployment operations must be automated:

- Use Nuke build system for reproducible builds
- Task runner for developer convenience workflows
- CI/CD pipeline for automated testing and deployment
- Version management follows semantic versioning
- Build artifacts are reproducible and traceable

## Technical Standards

### Architecture Guidelines

- **Data Layer**: ScriptableObjects for static game data (cards, abilities, rules)
- **Game Logic**: Plain C# classes and structs for game state and rules
- **Presentation Layer**: MonoBehaviours for Unity integration and rendering
- **Persistence**: JSON-based save system with versioning support
- **Networking**: Plan for future multiplayer but build single-player first

### Technology Stack

- **Engine**: Unity (version specified in ProjectSettings/ProjectVersion.txt)
- **Language**: C# (.NET Standard 2.1 compatible)
- **Build System**: Nuke (.NET-based build automation)
- **Task Runner**: Task (Go-based task automation)
- **UI Framework**: Unity UI Toolkit (UIElements)
- **Input System**: Unity's new Input System
- **Asset Pipeline**: Unity Addressables for future-proofing

### Security and Data Protection

- Never commit credentials or API keys to version control
- Use Unity's PlayerPrefs only for non-sensitive settings
- Encrypt save files with player-specific keys
- Validate all input data from external sources
- Sanitize and validate card definitions before loading

## Development Workflow

### Spec-Driven Feature Development

All significant features follow the spec-kit workflow:

1. Create specification with `/speckit.specify` (requirements, user stories)
2. Clarify ambiguities with `/speckit.clarify` (optional but recommended)
3. Generate implementation plan with `/speckit.plan` (technical approach)
4. Break down into tasks with `/speckit.tasks` (actionable steps)
5. Validate with `/speckit.analyze` (consistency check)
6. Implement with `/speckit.implement` (execution)

### Quality Gates

Before merging to main branch:

- All specs must have completed "Review & Acceptance Checklist"
- Unit tests pass with >70% code coverage for game logic
- Build succeeds without warnings on all target platforms
- Performance profiling shows no regressions
- Code review approved by at least one maintainer
- Documentation updated to reflect changes

### Branch Strategy

- **main**: Production-ready code, always deployable
- **feature/{number}-{name}**: Feature development (created by spec-kit)
- **hotfix/{issue}**: Critical production fixes
- **experimental/{name}**: Exploratory work not following spec-kit

### Code Review Principles

- Focus on architecture, maintainability, and performance
- Validate against constitution principles
- Ensure tests adequately cover changes
- Check for Unity best practices adherence
- Verify documentation completeness

## Player-Centric Design

### User Experience Standards

- Responsive interactions: feedback within 100ms
- Clear visual hierarchy and intuitive navigation
- Accessibility: support for colorblind modes and adjustable text sizes
- Forgiving UX: confirm destructive actions, allow undo where practical
- Progressive disclosure: don't overwhelm new players

### Gameplay Balance

- Playtesting required for all card changes affecting balance
- Document balance rationale in card definitions
- Track game metrics to identify balance issues
- Regular balance patches based on player data

### Monetization Ethics

- No pay-to-win mechanics
- All content achievable through gameplay
- Transparent drop rates for randomized rewards
- Respect player time and investment

## Governance

This constitution supersedes all other development practices and serves as the final authority for architectural and technical decisions.

### Amendment Process

Constitution amendments require:

1. Documented rationale with examples
2. Team consensus or maintainer approval
3. Migration plan for existing code if applicable
4. Update to version and amendment date

### Compliance

- All pull requests must demonstrate constitution compliance
- Deviations require explicit justification and approval
- Spec-kit `/speckit.constitution` command updates this document
- Regular constitutional review (quarterly) to ensure relevance

### Conflict Resolution

When constitution principles conflict:

1. Player experience takes precedence over developer convenience
2. Performance and quality over feature velocity
3. Maintainability over cleverness
4. Simplicity over flexibility (until flexibility is proven necessary)

**Version**: 1.0.0 | **Ratified**: 2025-10-16 | **Last Amended**: 2025-10-16
