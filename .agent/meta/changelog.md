# Agent Rules Changelog

All notable changes to agent instruction base will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-16

### Added
- Initial agent instruction system for Sango Card project
- Base rules adapted from Winged Bean project structure
- 10 core principles for Unity card game development
- 60+ normative rules across categories:
  - R-CODE-xxx: Code quality & architecture (8 rules)
  - R-UNITY-xxx: Unity-specific rules (10 rules)
  - R-SEC-xxx: Security guidelines (6 rules)
  - R-TST-xxx: Testing standards (5 rules)
  - R-PERF-xxx: Performance requirements (6 rules)
  - R-DOC-xxx: Documentation conventions (4 rules)
  - R-SPEC-xxx: Spec-kit integration (7 rules)
  - R-GIT-xxx: Git workflow (5 rules)
  - R-BLD-xxx: Build system (5 rules)
  - R-PRC-xxx: Process guidelines (5 rules)
- Domain glossary covering card game, Unity, build system, and spec-kit terms
- Claude Code adapter with Unity-specific context
- GitHub Copilot adapter with inline suggestion guidance
- Metadata structure (changelog, versioning, adapter template)

### Philosophy
- Unity-first development with mobile optimization
- Spec-driven feature development methodology
- Performance-conscious with 60 FPS target
- Testable architecture with separation of concerns
- Build automation through Task/Nuke pipeline

### Context
- Unity 2022.3 LTS
- C# with .NET Standard 2.1
- Mobile-first (Android ARM64)
- Spec-kit integration for structured development

## Unreleased

(No changes pending)

---

### Version Guidelines

**MAJOR version** (X.0.0): Backward-incompatible changes to rule semantics or structure
- Removing a rule category
- Changing rule ID numbering scheme
- Fundamental philosophy changes

**MINOR version** (1.X.0): Backward-compatible additions
- New rules added
- New rule categories
- Additional context or examples

**PATCH version** (1.0.X): Clarifications and fixes
- Typo fixes
- Wording improvements
- Example corrections
- Documentation enhancements

### Change Categories

- **Added**: New rules, categories, or sections
- **Changed**: Modifications to existing rule semantics
- **Deprecated**: Rules marked for removal (but still present)
- **Removed**: Deleted rules (MAJOR version only)
- **Fixed**: Corrections to errors or ambiguities
- **Security**: Security-related rule updates
