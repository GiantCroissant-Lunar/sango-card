# Agent Custom Instructions

Multi-agent project configuration for Sango Card.

## Supported Agents

### Claude Code

**Pointer:** `CLAUDE.md` → `.agent/adapters/claude.md`

Claude is the primary development agent for this project with full context awareness.

### GitHub Copilot

**Pointer:** `.github/copilot-instructions.md` → `.agent/adapters/copilot.md`

Copilot provides inline code suggestions and chat assistance following project patterns.

### Windsurf

**Pointer:** `.windsurf/rules.md` → `.agent/adapters/windsurf.md`

Windsurf/Codeium integration for alternative AI coding assistance.

## Agent Rules System

All agents follow the shared rule base at `.agent/base/` with agent-specific adapters.

**Structure:**

- `.agent/base/` - Canonical rules (10 principles, 60+ rules, glossary)
- `.agent/adapters/` - Agent-specific implementations
- `.agent/meta/` - Versioning and governance

**Version:** 1.0.0 (unity-csharp stack)

## Tool Calling Efficiency

When using tools, maximize efficiency by calling multiple independent tools in parallel:

```typescript
// ✅ Good - Parallel independent operations
await Promise.all([
  readFile(''spec.md''),
  readFile(''plan.md''),
  readFile(''tasks.md'')
]);

// ❌ Bad - Sequential when parallel is possible
await readFile(''spec.md'');
await readFile(''plan.md'');
await readFile(''tasks.md'');
```

**Guidelines:**

- Read multiple files in parallel when gathering information
- Execute independent tasks simultaneously
- Report progress in parallel with subsequent actions
- Only sequential when operations have dependencies

## Key Constraints

All agents must:

- Follow spec-kit workflow for features (R-SPEC-010)
- Use Task runner for builds (R-BLD-010)
- Never commit secrets (R-SEC-020, R-GIT-020)
- Respect Unity 6000.x patterns (R-UNITY-xxx)
- Cite rule IDs when explaining constraints
- **NEVER modify `projects/client` outside build operations (R-BLD-060)**
  - The Unity project at `projects/client` is a standalone Git repository
  - It is read-only except during build execution
  - Build process must perform `git reset --hard` before building

## Adding New Agents

1. Copy `.agent/meta/adapter-template.md`
2. Fill in agent-specific details
3. Save to `.agent/adapters/{agent}.md`
4. Create pointer file in appropriate location
5. Update this file with new agent entry

## Documentation

- **Base Rules:** `.agent/base/20-rules.md`
- **Constitution:** `.specify/memory/constitution.md`
- **Spec-Kit Guide:** `docs/SPEC-KIT.md`
- **Quick Start:** `docs/SPEC-KIT-QUICKSTART.md`
