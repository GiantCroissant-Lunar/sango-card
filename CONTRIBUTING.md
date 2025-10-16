# Contributing to Sango Card

Thank you for your interest in improving Sango Card! This document outlines the preferred workflow and project conventions so contributions remain smooth and consistent.

## Getting started

- **Review the repo**: See `README.md` for an overview of the repository layout, prerequisites, and build scripts.
- **Set up tooling**: Install the .NET SDK required by `_build.csproj`, the Unity Editor version referenced by the client project, and any other dependencies described in subproject documentation.
- **Run the build**: Validate your local setup by executing one of the build entry points in `build/` (`build.ps1`, `build.sh`, or `build.cmd`).

## Development workflow

- **Branching**: Create a feature branch from the default branch that clearly describes your change, e.g. `feature/nuke-ci-pipeline` or `fix/unity-import`.
- **Commits**: Keep commits focused and write descriptive messages. Squash or rebase as needed to present a clear commit history before submitting a pull request.
- **Tests & builds**: Ensure the Nuke build pipeline passes locally. For Unity-specific changes, open the editor to confirm assets/scripts compile without errors.
- **Documentation**: Update `README.md` or subproject documentation when you add new tooling, workflows, or configuration steps.

## Pull requests

1. Fork the repository (if you do not have direct push access).
2. Push your branch to your fork or to the main repository (if permitted).
3. Open a pull request describing:
   - The problem being solved
   - A summary of the solution
   - Any follow-up work or known limitations
4. Link related issues when applicable and request reviews from relevant maintainers.
5. Address review feedback promptly; use follow-up commits or amend existing ones as appropriate.

## Code style & quality

- **EditorConfig**: Respect the rules defined in the root `.editorconfig` and the additional settings under `build/`.
- **Automation**: Leverage the Nuke build scripts and Unity code generation tools (Jenny, Entitas, etc.) configured under `projects/client/`.
- **Static analysis**: Integrate code-quality checks under `projects/code-quality/` whenever they become available, and keep the folder updated with shared tooling.

## Communication

- Use GitHub issues for feature requests, bug reports, and questions.
- For major architectural changes, consider proposing a design doc or GitHub discussion before starting implementation.

We value respectful collaboration and inclusive communication. Please review `CODE_OF_CONDUCT.md` to understand expectations when participating in the community. Happy coding!
