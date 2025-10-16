# {Agent Name} Adapter

Base-Version-Expected: 1.0.0

References base rule set in `.agent/base/`. The base canon prevails on conflict.

## Agent Context

{Describe the agent: capabilities, limitations, interaction model}

Example:

- {Agent Name} is a {type} coding assistant that {capabilities}
- Strengths: {list strengths}
- Limitations: {list limitations}
- Interaction style: {chat/inline/both}

## Interaction Style

{How should this agent communicate?}

Example:

- Be concise and direct. Minimize preamble and postamble.
- For multi-step tasks, {how to track progress}.
- Cite rule IDs when explaining constraints (e.g., "Following R-CODE-010...").
- When uncertain, {how to handle uncertainty}.

## Unity & C# Context

{Agent-specific guidance for Unity development}

Example:

- This is a Unity 2022.3 LTS mobile card game project.
- Respect Unity and C# naming conventions (R-CODE-030, R-CODE-040).
- Understand Unity lifecycle and mobile optimization.
- Target .NET Standard 2.1 compatibility.

## Code Generation

{How should agent generate code?}

Must enforce:

- R-CODE-010: Prefer editing over creating
- R-CODE-020: Don't fabricate details
- R-CODE-030: Follow C# conventions
- R-CODE-040: Unity [SerializeField] pattern
- R-CODE-050: Respect structure
- R-CODE-060: Separate logic from MonoBehaviours

## Unity-Specific Behavior

{How should agent work with Unity?}

Must enforce:

- R-UNITY-010: ScriptableObjects for data
- R-UNITY-020: Proper lifecycle usage
- R-UNITY-030: Cache component references
- R-UNITY-040: Object pooling
- R-UNITY-050: UI Toolkit preference
- R-UNITY-080: Feature-based organization

## Security Enforcement

{How should agent handle security?}

Must enforce:

- R-SEC-010: Never log secrets
- R-SEC-020: No embedded credentials
- R-SEC-030: Redact PII
- R-SEC-040: Timeouts for external calls
- R-SEC-050: Encrypt save files

## Performance Awareness

{How should agent consider performance?}

Must enforce:

- R-PERF-010: Profile before optimizing
- R-PERF-020: 60 FPS target
- R-PERF-030: Minimize allocations
- R-PERF-040: Mobile texture limits

## Spec-Kit Integration

{How should agent work with spec-kit?}

Must enforce:

- R-SPEC-010: Follow spec-kit workflow
- R-SPEC-020: Use /speckit.specify
- R-SPEC-040: Respect constitution
- R-SPEC-050: Use /speckit.clarify when ambiguous

## Testing

{How should agent approach testing?}

Must enforce:

- R-TST-010: Unity Test Framework
- R-TST-020: Edit Mode vs Play Mode
- R-TST-040: 70% coverage target
- R-TST-050: Mock Unity APIs

## Git Workflow

{How should agent handle Git?}

Must enforce:

- R-GIT-010: Commit with -F file
- R-GIT-020: Never commit secrets
- R-GIT-030: No Library/ folder
- R-GIT-040: Commit .meta files

## Build System

{How should agent use build tools?}

Must enforce:

- R-BLD-010: Use Task runner
- R-BLD-020: Build commands
- R-BLD-030: Verify locally
- R-BLD-040: Reproducible Unity builds

## Documentation

{How should agent handle documentation?}

Must enforce:

- R-DOC-010: Only when requested
- R-DOC-020: XML comments for public APIs
- R-DOC-030: Update READMEs
- R-DOC-040: Document Unity patterns

## Agent-Specific Patterns

{Patterns specific to this agent's capabilities}

Example for code completion agent:

- Suggest object pooling when seeing Instantiate/Destroy
- Suggest caching when seeing multiple GetComponent calls
- Suggest ScriptableObject when seeing hardcoded data

Example for chat agent:

- Provide multiple architectural options
- Ask clarifying questions before implementing
- Reference constitution for strategic decisions

## Common Use Cases

{Examples of how to use this agent effectively}

1. **Creating new card**: {workflow}
2. **Adding game mechanic**: {workflow}
3. **Optimizing performance**: {workflow}
4. **Writing tests**: {workflow}

## Anti-Patterns for This Agent

{What this agent should NOT do}

1. {Anti-pattern} - Use {correct pattern} instead (cite rule)
2. {Anti-pattern} - Use {correct pattern} instead (cite rule)

---

## Customization Checklist

When creating new adapter from this template:

- [ ] Replace all `{placeholders}` with actual content
- [ ] Set correct `Base-Version-Expected` version
- [ ] Add agent-specific context and capabilities
- [ ] Define interaction style for this agent
- [ ] Add agent-specific code patterns
- [ ] Include relevant examples
- [ ] Create pointer file in appropriate location
- [ ] Test adapter with actual agent
- [ ] Update `.agent/README.md` with new agent entry
