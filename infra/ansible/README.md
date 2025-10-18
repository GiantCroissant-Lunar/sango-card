# Sango Card - Ansible Development Environment Setup

Automates development environment setup for Sango Card across Windows, macOS, and Linux.

## What Gets Installed

- **.NET SDK 8.0** - Nuke build system
- **Node.js 20** - npm-based tools
- **Python 3.11** - pre-commit, spec-kit
- **Git + LFS** - Version control
- **Task** - Task runner
- **Nuke** - Build automation
- **GitVersion** - Semantic versioning
- **pre-commit** - Git hooks
- **uv** - Python package manager
- **jq** - JSON processing

## Quick Start

### Windows

```powershell
# Open PowerShell as Administrator
cd infra\ansible
.\setup.ps1
```

### macOS / Linux

```bash
cd infra/ansible
./setup.sh
```

## Prerequisites

### All Platforms

- Python 3.x
- pip or uv
- Ansible >= 2.12

Install Ansible:

```bash
pip install ansible        # macOS/Linux
pip install ansible pywinrm # Windows
```

### Platform-Specific

**Windows:**

- PowerShell 5.1+
- WinRM enabled: `Enable-PSRemoting -Force`

**macOS:**

- Xcode Command Line Tools: `xcode-select --install`

**Linux:**

- sudo privileges

## Configuration

### Global Settings

Edit `group_vars/all.yml`:

```yaml
# Tool versions
dotnet_version: "8.0"
nodejs_version: "20"
python_version: "3.11"
unity_version: "2022.3.x"

# Flags
install_unity: false
install_vscode: false
```

### Per-User Settings

Create `host_vars/localhost.yml`:

```yaml
# Git configuration
git_user_name: "Your Name"
git_user_email: "your.email@example.com"

# Installation preferences
install_vscode: true
install_rider: true
```

## Usage

### Full Setup

```bash
ansible-playbook playbook.yml
```

### Install Specific Tools

```bash
# Only .NET and Node.js
ansible-playbook playbook.yml --tags "dotnet,nodejs"

# Only Git and build tools
ansible-playbook playbook.yml --tags "git,build"
```

### Skip Specific Tools

```bash
# Skip Unity
ansible-playbook playbook.yml --skip-tags unity

# Skip Python
ansible-playbook playbook.yml --skip-tags python
```

### Dry Run

```bash
# Check what would change
ansible-playbook playbook.yml --check
```

## Available Tags

| Tag | Description |
|-----|-------------|
| `common`, `base` | Base system packages |
| `dotnet` | .NET SDK |
| `nodejs` | Node.js and npm |
| `python` | Python and tools |
| `git` | Git configuration |
| `build` | Build tools (Task, Nuke) |
| `unity`, `gamedev` | Unity setup |
| `development` | All development tools |

## Verification

After setup:

```bash
# Check installations
dotnet --version    # Should show 8.x
node --version      # Should show v20.x
python3 --version   # Should show 3.11.x
git --version
task --version
nuke --version

# Build project
cd ../..  # Return to project root
task setup
task build
```

## Unity Setup

Unity must be installed manually:

1. Download Unity Hub: <https://unity.com/download>
2. Install Unity 2022.3.x (LTS)
3. Add modules:
   - Android Build Support
   - iOS Build Support (macOS)
   - WebGL Build Support (optional)

## Troubleshooting

### Ansible Connection Failed (Windows)

```powershell
Enable-PSRemoting -Force
winrm set winrm/config/service/auth '@{Basic="true"}'
```

### Package Installation Failed

```bash
# Skip problematic package
ansible-playbook playbook.yml --skip-tags problematic_tag

# Or install manually
```

### Python/pip Issues

```bash
# Update pip
python3 -m pip install --upgrade pip

# Use uv instead
curl -LsSf https://astral.sh/uv/install.sh | sh
```

## Roles

### common

Installs base packages (git, curl, wget, etc.)

### dotnet

Installs .NET SDK 8.0 and disables telemetry

### nodejs

Installs Node.js 20 and configures npm

### python

Installs Python 3.11, uv, pre-commit

### git

Configures Git, installs LFS

### build_tools

Installs Task, Nuke, GitVersion, jq

### unity

Provides Unity installation instructions

## Platform-Specific Notes

### Windows

- Uses Chocolatey package manager
- Requires Administrator privileges
- May need terminal restart

### macOS

- Uses Homebrew package manager
- Requires Xcode CLI tools
- May prompt for password

### Linux

- Uses apt/yum package manager
- Requires sudo privileges
- Installs to `~/.local/bin`

## Advanced Usage

### Update All Tools

```bash
# Re-run playbook
ansible-playbook playbook.yml
```

### Verbose Output

```bash
# Debug mode
ansible-playbook playbook.yml -vvv
```

### Custom Inventory

```bash
# Use custom inventory
ansible-playbook -i custom_hosts playbook.yml
```

## CI/CD Integration

Setup self-hosted GitHub Actions runner:

```yaml
# In group_vars/all.yml or host_vars/localhost.yml
setup_github_runner: true
github_org: your-org
github_pat: "{{ lookup('env', 'GH_TOKEN') }}"
```

Run:

```bash
export GH_TOKEN="your_github_token"
ansible-playbook playbook.yml --tags runner
```

## Documentation

- [Ansible Docs](https://docs.ansible.com)
- [Main Infra README](../README.md)
- [Project Setup Guide](../../docs/setup/)

## Support

For issues, check the main [infra README](../README.md) or open an issue in the repository.
