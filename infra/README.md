# Sango Card Infrastructure as Code (IaC)

This directory contains Infrastructure as Code (IaC) for the Sango Card project:

- **Terraform** - GitHub repository management (secrets, variables, branch protection)
- **Ansible** - Development environment setup automation

## Quick Start

### For Developers (Setup Your Development Environment)

```bash
# Navigate to ansible directory
cd infra/ansible

# Windows (PowerShell as Administrator)
.\setup.ps1

# macOS/Linux
./setup.sh
```

This installs: .NET SDK 8.0, Node.js 20, Python 3.11, Git+LFS, Task, Nuke, pre-commit, and other tools.

**Time: ~10-15 minutes**

### For Repository Administrators (Configure GitHub)

```bash
cd infra/terraform/github

# Copy and edit configuration
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your settings

# Set GitHub token
export TF_VAR_github_token="ghp_your_token_here"

# Apply configuration
terraform init
terraform plan
terraform apply
```

**Time: ~5 minutes**

## What Gets Installed (Ansible)

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 8.0 | Nuke build system |
| Node.js | 20.x | npm tools |
| Python | 3.11 | pre-commit, spec-kit |
| Git + LFS | Latest | Version control |
| Task | 3.35.1 | Task runner |
| Nuke | Latest | Build automation |
| GitVersion | Latest | Semantic versioning |
| pre-commit | Latest | Git hooks |
| uv | Latest | Python package manager |

## What Gets Configured (Terraform)

- Repository settings and topics
- Branch protection (main + optional develop)
- GitHub Actions secrets (Unity license, build credentials)
- GitHub Actions variables (Unity version, build config)
- Issue labels (bug, feature, unity, gameplay, etc.)
- Optional CODEOWNERS file

## Directory Structure

```
infra/
├── README.md                   # This file
├── terraform/
│   └── github/                 # GitHub configuration
│       ├── main.tf             # Resources
│       ├── variables.tf        # Input variables
│       ├── outputs.tf          # Outputs
│       ├── backend.tf          # Backend config
│       └── terraform.tfvars.example
└── ansible/
    ├── playbook.yml            # Main playbook
    ├── group_vars/all.yml      # Global variables
    ├── inventory/hosts         # Host inventory
    ├── roles/                  # Ansible roles
    │   ├── common/             # Base packages
    │   ├── dotnet/             # .NET SDK
    │   ├── nodejs/             # Node.js
    │   ├── python/             # Python tools
    │   ├── git/                # Git config
    │   ├── unity/              # Unity setup
    │   └── build_tools/        # Build tools
    ├── setup.sh                # Quick setup (Unix)
    └── setup.ps1               # Quick setup (Windows)
```

## Platform Support

| Platform | Ansible | Terraform |
|----------|---------|-----------|
| Windows 10/11 | ✅ | ✅ |
| macOS (Intel/Apple Silicon) | ✅ | ✅ |
| Ubuntu/Debian Linux | ✅ | ✅ |

## Configuration

### Customize Tool Versions

Edit `ansible/group_vars/all.yml`:

```yaml
dotnet_version: "8.0"
nodejs_version: "20"
python_version: "3.11"
unity_version: "2022.3.x"
```

### Configure GitHub Settings

Edit `terraform/github/terraform.tfvars`:

```hcl
github_owner = "your-org"
repo_name = "sango-card"

repository_secrets = {
  UNITY_LICENSE = "..."
}

repository_variables = {
  UNITY_VERSION = "2022.3.x"
}
```

## Manual Unity Installation

Unity Hub and Editor must be installed manually:

1. Download Unity Hub: https://unity.com/download
2. Install Unity 2022.3.x (LTS)
3. Add modules:
   - Android Build Support
   - iOS Build Support (macOS only)
   - WebGL Build Support (optional)

## Common Commands

```bash
# Ansible - Install dev tools
cd infra/ansible
ansible-playbook playbook.yml

# Install only specific tools
ansible-playbook playbook.yml --tags "dotnet,nodejs"

# Terraform - Configure GitHub
cd infra/terraform/github
terraform init
terraform plan
terraform apply

# View GitHub configuration
terraform output
```

## Verification

After setup, verify installation:

```bash
dotnet --version       # Should show 8.x
node --version         # Should show v20.x
python3 --version      # Should show 3.11.x
git --version
task --version
nuke --version
pre-commit --version
```

Then build the project:

```bash
cd ../..  # Return to project root
task setup
task build
```

## Troubleshooting

### Ansible Issues

**Problem:** Package installation fails
**Solution:** Skip problematic packages
```bash
ansible-playbook playbook.yml --skip-tags unity
```

**Problem:** WinRM connection failed (Windows)
**Solution:** Enable WinRM as Administrator
```powershell
Enable-PSRemoting -Force
```

### Terraform Issues

**Problem:** State lock error
**Solution:** Force unlock (use with caution)
```bash
terraform force-unlock <lock-id>
```

**Problem:** Authentication error
**Solution:** Verify token
```bash
echo $TF_VAR_github_token
```

## Security Best Practices

1. **Never commit secrets** - `terraform.tfvars` is gitignored
2. **Use environment variables** for GitHub token
3. **Rotate tokens regularly** (quarterly)
4. **Use Terraform Cloud** for state encryption
5. **Review changes** with `terraform plan` before apply

## Maintenance

| Task | Frequency | Command |
|------|-----------|---------|
| Update tools | Monthly | `ansible-playbook playbook.yml` |
| Update versions | Per release | Edit `group_vars/all.yml` |
| Rotate tokens | Quarterly | Generate new GitHub PAT |
| Check drift | Weekly | `terraform plan` |

## Related Documentation

- [Ansible Details](ansible/README.md) - Ansible-specific documentation
- [Terraform Details](terraform/github/README.md) - Terraform-specific documentation
- [Project Build Docs](../docs/build/) - Build system documentation
- [Contributing Guide](../CONTRIBUTING.md) - Contribution guidelines

## Support

For issues:

1. Check README troubleshooting sections
2. Review error messages with verbose flags (`-vvv`)
3. Consult official documentation (Ansible, Terraform)
4. Open an issue in the repository

## License

Part of the Sango Card project. See project LICENSE for details.
