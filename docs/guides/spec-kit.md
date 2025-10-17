---
doc_id: DOC-2025-00122
title: Spec Kit
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [spec-kit]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00064
title: Spec Kit Guide
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:

- spec-kit
- workflow
summary: Complete guide to using Spec Kit for feature development.

---

# Spec-Kit Integration Guide

This document explains how to use [GitHub's spec-kit](https://github.com/github/spec-kit) for spec-driven development in the Sango Card project.

## What is Spec-Driven Development?

Spec-Driven Development flips traditional software development by making specifications executable and actionable rather than just documentation. Instead of writing code first and specs as an afterthought, you define **what** you want to build (requirements), **why** it matters (user stories), and then let AI agents help you plan and implement with precision.

## Benefits

- **Reduced rework** - Clarify requirements before implementation
- **Better alignment** - Ensure technical plans match product requirements
- **Consistent quality** - Follow established project principles
- **Traceable decisions** - Document rationale for technical choices
- **Iterative refinement** - Improve specs through structured feedback
- **AI-native workflow** - Optimized for working with AI coding agents

## The Workflow

### Phase 1: Establish Principles

Use `/speckit.constitution` to create or update your project's governing principles:

```
/speckit.constitution Create principles focused on Unity best practices,
C# coding standards, performance requirements for card game mechanics,
and consistent UI/UX patterns
```

This creates `.specify/memory/constitution.md` which guides all subsequent development decisions.

### Phase 2: Specify Requirements

Use `/speckit.specify` to describe what you want to build:

```
/speckit.specify Create a card collection system where players can view,
organize, and manage their card inventory. Include filtering by rarity,
type, and faction. Support drag-and-drop card organization into custom decks.
```

Focus on **what** and **why**, not **how**. This creates a spec in `.specify/specs/{feature-number}-{feature-name}/spec.md`.

### Phase 3: Plan Implementation

Use `/speckit.plan` to define the technical approach:

```
/speckit.plan Use Unity's UI Toolkit for the card collection interface.
Store card data in ScriptableObjects. Implement filtering with LINQ queries.
Use Unity's new Input System for drag-and-drop interactions.
```

This creates implementation plans, data models, and API contracts in the feature spec directory.

### Phase 4: Break Down Tasks

Use `/speckit.tasks` to generate an actionable task list:

```
/speckit.tasks
```

This creates `tasks.md` with ordered, dependency-aware tasks that can be executed systematically.

### Phase 5: Execute Implementation

Use `/speckit.implement` to build the feature:

```
/speckit.implement
```

The AI agent will execute tasks in order, respecting dependencies and following the established plan.

## Optional Quality Gates

### Clarification (Recommended Before Planning)

Use `/speckit.clarify` to identify and resolve ambiguities:

```
/speckit.clarify
```

This runs a structured questioning workflow to uncover underspecified areas before you commit to a technical plan.

### Analysis (Before Implementation)

Use `/speckit.analyze` after tasks are defined:

```
/speckit.analyze
```

This validates that your tasks fully cover the specification and plan, highlighting any gaps or inconsistencies.

### Custom Checklists

Use `/speckit.checklist` to generate quality validation checklists:

```
/speckit.checklist
```

Think of these as "unit tests for English" - they validate requirements are complete, clear, and consistent.

## File Structure

```
.specify/
├── memory/
│   └── constitution.md          # Project principles and guidelines
├── scripts/
│   └── powershell/              # Automation scripts for feature workflows
│       ├── check-prerequisites.ps1
│       ├── common.ps1
│       ├── create-new-feature.ps1
│       ├── setup-plan.ps1
│       └── update-agent-context.ps1
├── specs/
│   └── {feature-number}-{feature-name}/
│       ├── spec.md              # Feature specification
│       ├── plan.md              # Implementation plan
│       ├── tasks.md             # Task breakdown
│       ├── data-model.md        # Data structures
│       ├── contracts/           # API contracts, schemas
│       └── research.md          # Technical research notes
└── templates/
    ├── spec-template.md         # Template for new specs
    ├── plan-template.md         # Template for implementation plans
    ├── tasks-template.md        # Template for task breakdowns
    └── checklist-template.md    # Template for quality checklists

.github/
└── prompts/
    ├── speckit.constitution.prompt.md
    ├── speckit.specify.prompt.md
    ├── speckit.plan.prompt.md
    ├── speckit.tasks.prompt.md
    ├── speckit.implement.prompt.md
    ├── speckit.clarify.prompt.md
    ├── speckit.analyze.prompt.md
    └── speckit.checklist.prompt.md
```

## Best Practices

### Start with Constitution

Before creating your first feature spec, establish project principles with `/speckit.constitution`. This ensures consistency across all features.

### Iterate on Specs

Don't treat the first draft as final. Use natural language with your AI agent to refine specs after `/speckit.specify`:

```
The card rarity filtering should also include a "favorites" category
that persists across sessions
```

### Clarify Before Planning

Run `/speckit.clarify` before `/speckit.plan` to reduce rework. It's easier to fix requirements than to refactor code.

### Validate with Checklists

After `/speckit.plan`, use `/speckit.checklist` to create quality gates that ensure your spec meets standards.

### Review Before Implementation

Have a human review the spec, plan, and tasks before running `/speckit.implement`. The AI agent will execute what's written, so ensure it's correct.

### Branch Per Feature

The spec-kit scripts automatically create feature branches. Keep each feature on its own branch for clean history and easy review.

## Integration with Existing Workflows

### With Task Runner

Spec-kit complements but doesn't replace the existing Task-based build workflow:

- Use **spec-kit** for feature design and implementation
- Use **Task** for building, testing, and CI/CD operations

Example workflow:

1. `/speckit.specify` - Define feature
2. `/speckit.plan` - Plan implementation
3. `/speckit.tasks` - Break down work
4. `/speckit.implement` - Build feature
5. `task build` - Build and verify
6. `task test` - Run tests
7. `task ci` - Full CI pipeline

### With Unity Development

Spec-kit works naturally with Unity:

- Specs can describe Unity-specific requirements (ScriptableObjects, Components, etc.)
- Plans can reference Unity APIs and best practices
- Implementation executes Unity-specific commands (`dotnet`, Unity CLI)

### With Git Workflow

Spec-kit enhances Git workflow:

- Feature branches are created automatically
- Each feature has a dedicated spec directory
- Commit history reflects spec → plan → tasks → implementation flow

## Example: Complete Feature Workflow

Let's walk through adding a "Daily Rewards" feature:

### 1. Establish principles (once per project)

```
/speckit.constitution Focus on Unity performance optimization,
C# async/await patterns, maintainable MonoBehaviour architecture,
and player-centric reward systems that feel generous but balanced
```

### 2. Create specification

```
/speckit.specify Create a daily rewards system where players receive
increasing rewards for consecutive daily logins. Show a calendar UI
with 7 days, highlighting claimed days. Rewards include cards, gold,
and premium currency. Track streak across sessions with local persistence.
```

### 3. Clarify ambiguities

```
/speckit.clarify
```

The agent asks questions like:

- "What happens if a player misses a day - does the streak reset?"
- "Should time zones affect the daily reset?"
- "What's the maximum reward tier?"

Answer each question to refine the spec.

### 4. Create implementation plan

```
/speckit.plan Use Unity's PlayerPrefs for persistence, with JSON
serialization for the reward calendar state. Implement the calendar UI
with UI Toolkit. Use System.DateTime for time calculations with UTC
normalization. Create a RewardDefinition ScriptableObject for configuring
reward tiers.
```

### 5. Generate task breakdown

```
/speckit.tasks
```

Review the generated tasks in `.specify/specs/001-daily-rewards/tasks.md`.

### 6. Optional: Analyze coverage

```
/speckit.analyze
```

Validates that tasks cover all requirements and plan items.

### 7. Implement

```
/speckit.implement
```

The agent executes tasks in order, creating scripts, assets, and tests.

### 8. Build and test

```powershell
task build
task test
```

### 9. Review and iterate

If issues are found, update the spec or plan and regenerate tasks, or fix directly in code for minor changes.

## Troubleshooting

### Slash commands not appearing

Ensure you're using a supported AI agent (GitHub Copilot, Claude Code, etc.) and that `.github/prompts/` directory exists with prompt files.

### Scripts fail to execute

Check that PowerShell execution policy allows scripts:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Feature branch not created automatically

Verify Git is installed and the repository is initialized. Check `.specify/scripts/powershell/create-new-feature.ps1` for errors.

### Agent ignores constitution

Explicitly reference it in prompts:

```
Review the constitution at .specify/memory/constitution.md and ensure
this plan follows all established principles
```

## Resources

- **[Spec-Kit Repository](https://github.com/github/spec-kit)** - Official toolkit
- **[Spec-Driven Methodology](https://github.com/github/spec-kit/blob/main/spec-driven.md)** - Full process guide
- **[Supported AI Agents](https://github.com/github/spec-kit#-supported-ai-agents)** - Compatibility matrix
- **[CLI Reference](https://github.com/github/spec-kit#-specify-cli-reference)** - Command documentation

## Contributing

When contributing features to Sango Card:

1. Use spec-kit workflow for new features
2. Include specs and plans in pull requests
3. Reference the spec number in commit messages (e.g., "Implement #001: Daily Rewards")
4. Keep constitution updated as project practices evolve

## Questions?

For questions about spec-kit integration, open a discussion or issue in the repository.
