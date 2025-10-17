---
name: Spec-Kit Task
about: Create a task from spec-kit for AI agent execution
title: '[TASK-X.X] Task Title'
labels: spec-kit-task, ai-agent-ready
assignees: ''
---

## Task Metadata

**Task ID:** TASK-X.X  
**Epic:** Epic Name  
**Story Points:** X  
**Priority:** Critical/High/Medium/Low  
**Dependencies:** TASK-X.X, TASK-X.X  
**Estimated Time:** X hours  
**Spec Reference:** `.specify/tasks/build-preparation-tool-tasks.md#task-xx`

## Task Description

[Brief description of what needs to be done]

## Subtasks

- [ ] Subtask 1
- [ ] Subtask 2
- [ ] Subtask 3

## Acceptance Criteria

- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

## Technical Context

### Files to Create/Modify

```
path/to/file1.cs
path/to/file2.cs
```

### Related Code

```csharp
// Example code or interfaces to implement
```

### Dependencies

- Package: `PackageName` version `X.X.X`
- Service: `ServiceName` (from DI)

## Test Requirements

### Unit Tests Required

```csharp
[Fact] void TestCase1()
[Fact] void TestCase2()
```

### Test Coverage Target

X% code coverage

## Implementation Guidance

### Approach

1. Step 1
2. Step 2
3. Step 3

### Patterns to Follow

- Use dependency injection
- Follow repository coding standards
- Use async/await for I/O operations

### Pitfalls to Avoid

- Don't hardcode paths
- Don't use blocking I/O
- Don't skip validation

## AI Agent Instructions

### For GitHub Copilot Workspace

This task is ready for AI agent execution. The agent should:

1. Read the spec-kit task details
2. Implement all subtasks
3. Write unit tests
4. Ensure acceptance criteria met
5. Follow coding standards in `.editorconfig`
6. Reference path resolution rules in `.agent/base/30-path-resolution.md`

### Context Files to Read

- `.specify/specs/build-preparation-tool.md` - Overall specification
- `.specify/tasks/build-preparation-tool-tasks.md` - Task breakdown
- `.agent/base/30-path-resolution.md` - Path resolution rules
- `docs/rfcs/RFC-001-build-preparation-tool.md` - Architecture design

### Success Criteria for AI Agent

- [ ] All subtasks completed
- [ ] All acceptance criteria met
- [ ] Unit tests written and passing
- [ ] Code follows project standards
- [ ] No hardcoded values
- [ ] Proper error handling
- [ ] XML documentation added

## Definition of Done

- [ ] Code implemented
- [ ] Unit tests written (X% coverage)
- [ ] Tests passing
- [ ] Code reviewed
- [ ] Documentation updated
- [ ] No compiler warnings
- [ ] Follows coding standards

## Related Issues

- Depends on: #XXX
- Blocks: #XXX
- Related to: #XXX

## Notes

[Any additional context, links, or information]
