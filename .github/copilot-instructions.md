# GitHub Copilot Instructions

→ **Agent rules at:** `.agent/adapters/copilot.md`
→ **Base rules at:** `.agent/base/`
→ **Constitution at:** `.specify/memory/constitution.md`

Unity 2022.3 LTS mobile card game with spec-driven development.

## Core Guidelines

### Code Suggestions
- Prefer editing existing code (R-CODE-010)
- Use `[SerializeField]` for Unity fields (R-CODE-040)
- ScriptableObjects for game data (R-UNITY-010)
- Cache components, avoid GetComponent in loops (R-UNITY-030)
- UI Toolkit for UI, not legacy uGUI (R-UNITY-050)

### Security
- Never suggest hardcoded credentials (R-SEC-020)
- Redact sensitive data in logs (R-SEC-030)
- Encrypt save files (R-SEC-050)

### Performance
- Avoid per-frame allocations (R-PERF-030)
- Suggest pooling for common patterns (R-UNITY-040)
- No LINQ in hot paths (Update, FixedUpdate)

### Spec-Kit
- Remind about `/speckit.specify` for new features (R-SPEC-010)
- Suggest `/speckit.clarify` when ambiguous (R-SPEC-050)

### Build
- Suggest `task build` not direct commands (R-BLD-010)

## Common Patterns
1. Card Data: ScriptableObject
2. Card Controller: MonoBehaviour with cached refs
3. Object Pool: Generic manager
4. Save System: Encrypted JSON
5. Events: ScriptableObject-based

## Anti-Patterns to Avoid
1. Public fields on MonoBehaviours
2. GetComponent in Update
3. String-based lookups
4. FindObjectOfType in hot paths
5. Resources.Load (use Addressables)

---

See `.agent/adapters/copilot.md` for complete rules.
