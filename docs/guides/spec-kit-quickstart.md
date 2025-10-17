---
doc_id: DOC-2025-00065
title: Spec Kit Quick Start
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:
- spec-kit
- quickstart
summary: Quick start guide for Spec Kit workflow.
---

# Spec-Kit Quick Start Guide

A cheat sheet for using spec-kit in Sango Card development.

## Installation (One-Time Setup)

Already done for this project! But if you need to set up on another machine:

```powershell
# Install uv package manager
irm https://astral.sh/uv/install.ps1 | iex

# Install specify-cli
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git

# Check installation
specify check
```

## Core Workflow (5 Steps)

### 1️⃣ Establish Principles (Once Per Project)

```
/speckit.constitution Create principles for [your focus areas]
```

**Example:**

```
/speckit.constitution Focus on Unity performance, C# best practices,
card game balance, and player-centric design
```

**Output:** `.specify/memory/constitution.md`

---

### 2️⃣ Specify Requirements

```
/speckit.specify [Describe what you want to build]
```

**Example:**

```
/speckit.specify Create a deck builder where players can create custom
decks from their card collection. Include deck validation rules (min/max
cards, faction restrictions) and deck list export/import functionality
```

**Output:** `.specify/specs/{number}-{name}/spec.md`

---

### 3️⃣ Plan Implementation

```
/speckit.plan [Tech stack and architecture choices]
```

**Example:**

```
/speckit.plan Use Unity UI Toolkit for the deck builder interface.
Store deck data as ScriptableObjects with JSON serialization for
import/export. Implement validation with a rule-based system using
the Strategy pattern
```

**Output:** `.specify/specs/{number}-{name}/plan.md` + supporting docs

---

### 4️⃣ Break Down Tasks

```
/speckit.tasks
```

**Output:** `.specify/specs/{number}-{name}/tasks.md`

---

### 5️⃣ Execute Implementation

```
/speckit.implement
```

**Output:** Working code following the plan and tasks

---

## Optional Quality Commands

### Clarify Ambiguities (Before Planning)

```
/speckit.clarify
```

Runs structured Q&A to uncover underspecified areas. Use **before** `/speckit.plan`.

### Analyze Consistency (Before Implementation)

```
/speckit.analyze
```

Validates task coverage and cross-artifact consistency. Use **after** `/speckit.tasks`.

### Generate Quality Checklists (After Planning)

```
/speckit.checklist
```

Creates validation checklists for requirements quality. Use **after** `/speckit.plan`.

---

## Common Patterns

### Pattern 1: New Feature (Full Workflow)

```
# 1. Define what you want
/speckit.specify [feature description]

# 2. Clarify requirements
/speckit.clarify

# 3. Define how to build it
/speckit.plan [tech approach]

# 4. Validate with checklist (optional)
/speckit.checklist

# 5. Break into tasks
/speckit.tasks

# 6. Validate coverage (optional)
/speckit.analyze

# 7. Build it
/speckit.implement

# 8. Build and test
task build
task test
```

### Pattern 2: Quick Spec (Skip Optional Steps)

```
/speckit.specify [feature]
/speckit.plan [approach]
/speckit.tasks
/speckit.implement
```

### Pattern 3: Exploratory Spike (Skip Spec-Kit)

For quick experiments, you can skip spec-kit entirely and just code directly. Use spec-kit when:

- Feature is medium-to-large size
- Requirements need clarification
- Multiple implementation approaches exist
- Feature will be maintained long-term
- Team coordination is needed

---

## File Locations

| What | Where |
|------|-------|
| Project principles | `.specify/memory/constitution.md` |
| Feature specs | `.specify/specs/{number}-{name}/spec.md` |
| Implementation plans | `.specify/specs/{number}-{name}/plan.md` |
| Task breakdowns | `.specify/specs/{number}-{name}/tasks.md` |
| Data models | `.specify/specs/{number}-{name}/data-model.md` |
| API contracts | `.specify/specs/{number}-{name}/contracts/` |
| Slash commands | `.github/prompts/speckit.*.prompt.md` |
| Helper scripts | `.specify/scripts/powershell/` |

---

## VS Code Integration

Slash commands are automatically available in GitHub Copilot Chat when:

1. `.github/prompts/` directory exists (✅ already set up)
2. VS Code settings enable prompt files (✅ configured in `.vscode/settings.json`)
3. GitHub Copilot extension is installed

**Verify:** Type `/spec` in Copilot Chat and you should see autocomplete suggestions.

---

## Troubleshooting

### "Slash command not found"

- Ensure GitHub Copilot extension is up to date
- Check `.github/prompts/` directory exists
- Restart VS Code

### "Scripts fail to execute"

```powershell
# Allow script execution (one-time)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### "Feature branch not created"

- Ensure Git is initialized: `git status`
- Check script permissions
- Manually create branch: `git checkout -b feature/001-name`

### "Agent ignores constitution"

Explicitly reference it:

```
Before planning, review .specify/memory/constitution.md and ensure
all decisions align with our established principles
```

---

## Tips and Best Practices

### ✅ DO

- **Start with constitution** - Sets expectations for all features
- **Clarify before planning** - Saves rework later
- **Review before implementing** - Human validation is crucial
- **Commit specs with code** - Traceability for future maintenance
- **Update constitution** - As project practices evolve

### ❌ DON'T

- **Don't skip clarification** - Ambiguous specs lead to wrong implementations
- **Don't implement without plan** - Results in inconsistent architecture
- **Don't ignore constitution** - Defeats the purpose of having principles
- **Don't over-spec tiny changes** - Use spec-kit for meaningful features
- **Don't forget to build/test** - `/speckit.implement` doesn't run your tests

---

## Example Session

**Goal:** Add a card favoriting system

```
User: I want players to be able to favorite cards so they can quickly
      filter to their favorites in the collection view

Agent: Let me help you create a spec for this feature.

User: /speckit.specify Create a card favoriting system where players
      can mark cards as favorites by tapping a star icon. Favorites
      persist across sessions. Add a filter toggle in the collection
      view to show only favorited cards. Allow unfavoriting by tapping
      the star again.

Agent: [Creates .specify/specs/001-card-favorites/spec.md]

User: /speckit.clarify

Agent: [Asks clarifying questions about edge cases, data storage, etc.]
       Q1: Should favorites sync across devices if cloud save is added later?
       Q2: Is there a limit to how many cards can be favorited?
       ...

User: [Answers questions to refine spec]

User: /speckit.plan Use Unity PlayerPrefs for local storage with JSON
      serialization. Add a bool isFavorite field to CardData. Update
      CardCollectionView to support filtering. Use UI Toolkit toggle
      button with star icon.

Agent: [Creates plan.md, data-model.md, etc.]

User: /speckit.tasks

Agent: [Creates tasks.md with ordered task list]

User: /speckit.implement

Agent: [Implements all tasks, creating scripts and updating UI]

User: task build

Agent: [Builds successfully]

User: task test

Agent: [Tests pass]

User: git add -A
      git commit -m "feat: Add card favoriting system (#001)"
      git push
```

---

## More Help

- **Full Guide:** `docs/SPEC-KIT.md`
- **Official Docs:** <https://github.com/github/spec-kit>
- **Methodology:** <https://github.com/github/spec-kit/blob/main/spec-driven.md>
- **Constitution:** `.specify/memory/constitution.md`

---

**Last Updated:** 2025-10-16
