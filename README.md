
# Sango Card

Sango Card is organised as a multi-project workspace with scripted build automation. Use this README as the quick reference for the repository layout and the primary workflows supported today.

## Repository structure
- **`build/`** Nuke build infrastructure (`build.ps1`, `build.sh`, `_build.csproj`, etc.).
- **`projects/client/`** Unity client scaffold and related tooling configuration.
- **`projects/code-quality/`** Placeholder for shared quality gates and automation (currently empty).

## Prerequisites
- **.NET SDK** compatible with the Nuke build (follow `_build.csproj` target framework).
- **Unity Editor** matching the version specified inside `projects/client/ProjectSettings/ProjectVersion.txt` once populated.
- **Git LFS** if large assets are introduced in `projects/client/Assets/`.

## Getting started
1. Clone the repository and restore submodules if added in the future.
2. Install the prerequisites listed above.
3. Run the platform-specific build script from `build/`:
   - PowerShell: `./build.ps1`
   - Bash: `./build.sh`
   - Windows CMD: `build.cmd`
4. Open `projects/client/` in Unity for client-side development.

## Development workflow
- Use the Nuke pipeline (`build/`) to keep builds reproducible and CI friendly.
- Unity-generated code artifacts (e.g., Entitas, Jenny) are configured via the `.properties` files under `projects/client/`.
- Add documentation and onboarding notes for each subproject inside its own `README.md` (see `projects/client/README.md` as a starting point to replace).

## Contributing
- Create feature branches from the default branch and open pull requests with clear descriptions.
- Keep code style consistent with `.editorconfig` entries at the repo root and within `build/`.
- Update this README when adding new projects, tools, or setup steps.

## License
Document licensing terms for the project here once decided.
