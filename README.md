# Sango Card

[![Unity](https://img.shields.io/badge/Unity-6000.2.x-black?logo=unity)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Proprietary-red)](LICENSE)
[![Build](https://img.shields.io/badge/Build-Nuke-blue?logo=nuget)](build/nuke/)
[![Task](https://img.shields.io/badge/Task-Taskfile-29BEB0?logo=task)](https://taskfile.dev/)
[![Pre-commit](https://img.shields.io/badge/Pre--commit-enabled-brightgreen?logo=pre-commit)](https://pre-commit.com/)
[![Spec-Kit](https://img.shields.io/badge/Spec--Kit-enabled-purple)](https://github.com/github/spec-kit)

Sango Card is organised as a multi-project workspace with scripted build automation.
Use this README as the quick reference for the repository layout and the primary workflows supported today.

## Repository structure

- **`build/`** Nuke build infrastructure (`build.ps1`, `build.sh`, `_build.csproj`, etc.).
- **`projects/client/`** Unity client scaffold and related tooling configuration.
- **`projects/code-quality/`** Placeholder for shared quality gates and automation (currently empty).

## Prerequisites

- **.NET SDK** compatible with the Nuke build (follow `_build.csproj` target framework).
- **Unity Editor** matching the version specified inside
  `projects/client/ProjectSettings/ProjectVersion.txt` once populated.
- **Git LFS** if large assets are introduced in `projects/client/Assets/`.
- **Task** (optional but recommended) - Modern task runner for simplified workflows.
  See [Task Documentation](docs/task/) for installation.
- **[uv](https://docs.astral.sh/uv/)** (optional for spec-driven development) -
  Package manager for Python-based tooling.
- **[specify-cli](https://github.com/github/spec-kit)** (optional) - Spec-Driven Development toolkit.
  See [Spec-Driven Development](#spec-driven-development) section below.
- **[pre-commit](https://pre-commit.com/)** (recommended) - Git hooks framework for code quality.
  Install with `pip install pre-commit` or `uv tool install pre-commit`.

## Getting started

1. Clone the repository and restore submodules if added in the future.
2. Install the prerequisites listed above.
3. **Quick start with Task (recommended)**:

   ```bash
   # Install Task (see docs/task/ for platform-specific instructions)
   task --version

   # Setup and build
   task setup
   task build
   ```

   **Or use Nuke directly**:
   - PowerShell: `./build.ps1`
   - Bash: `./build.sh`
   - Windows CMD: `build.cmd`
4. Open `projects/client/` in Unity for client-side development.

## Development workflow

- **Use Task for convenience**: `task dev` (clean + restore + build) or `task ci` (full CI pipeline).
  See [Task Documentation](docs/task/) for all available tasks.
- **Use the Nuke pipeline** (`build/`) directly for advanced scenarios to keep builds reproducible and
  CI friendly.
- Unity-generated code artifacts (e.g., Entitas, Jenny) are configured via the `.properties` files
  under `projects/client/`.
- Add documentation and onboarding notes for each subproject inside its own `README.md`
  (see `projects/client/README.md` as a starting point to replace).

### Common workflows with Task

```bash
task setup          # Initial setup and dependency restore
task build          # Build the project
task build:unity    # Build Unity project for Windows
task test           # Run all tests
task clean          # Clean build artifacts
task dev            # Complete dev workflow (clean + setup + build)
task ci             # Full CI pipeline (clean + setup + build + test)
```

For all available tasks, run `task --list` or see [Task Documentation](docs/task/).

## Spec-Driven Development

This project has adopted [GitHub's spec-kit](https://github.com/github/spec-kit) for structured,
specification-driven development. Spec-kit provides a methodology for building features through
well-defined phases: establishing principles, creating specifications, planning implementation,
breaking down tasks, and executing with precision.

### Quick Start with Spec-Kit

The spec-kit integration adds custom slash commands to GitHub Copilot and other supported
AI coding agents:

1. **`/speckit.constitution`** - Establish or update project governing principles
2. **`/speckit.specify`** - Create feature specifications (what you want to build)
3. **`/speckit.plan`** - Generate technical implementation plans with your tech stack
4. **`/speckit.tasks`** - Break down the plan into actionable tasks
5. **`/speckit.implement`** - Execute the tasks to build the feature

### Optional Quality Enhancement Commands

- **`/speckit.clarify`** - Ask structured questions about underspecified areas
  (recommended before `/speckit.plan`)
- **`/speckit.analyze`** - Verify cross-artifact consistency and coverage
  (run after `/speckit.tasks`, before `/speckit.implement`)
- **`/speckit.checklist`** - Generate quality checklists that validate completeness, clarity,
  and consistency

### Structure

All spec-kit artifacts are organized under `.specify/`:

- **`.specify/memory/`** - Project constitution and principles
- **`.specify/scripts/`** - Automation scripts for feature workflow
- **`.specify/templates/`** - Templates for specs, plans, and tasks
- **`.github/prompts/`** - Slash command definitions for GitHub Copilot

Specification files for individual features are stored in
`.specify/specs/{feature-number}-{feature-name}/`.

### Learn More

- **[Spec-Kit Documentation](https://github.com/github/spec-kit)** -
  Complete methodology and reference
- **[Spec-Driven Development Guide](https://github.com/github/spec-kit/blob/main/spec-driven.md)** -
  Deep dive into the full process
- **[Installing specify-cli](https://github.com/github/spec-kit#-get-started)** -
  CLI tool for bootstrapping projects

## Contributing

- Create feature branches from the default branch and open pull requests with clear descriptions.
- Keep code style consistent with `.editorconfig` entries at the repo root and within `build/`.
- Update this README when adding new projects, tools, or setup steps.

## License

Document licensing terms for the project here once decided.
