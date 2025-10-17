---
doc_id: DOC-2025-00154
title: Infra QUICKSTART
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [infra-quickstart]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00105
title: Infra QUICKSTART
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [infra-quickstart]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00100
title: Infra QUICKSTART
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [infra-quickstart]
summary: >
  (Add summary here)
source:
  author: system
---
# Sango Card Infrastructure - Quick Start Guide

## For Developers: Setup Your Machine

### Windows

```powershell
cd infra\ansible
.\setup.ps1
```

### macOS / Linux

```bash
cd infra/ansible
./setup.sh
```

**Installs:** .NET 8.0, Node.js 20, Python 3.11, Git+LFS, Task, Nuke, pre-commit

**Time:** ~10-15 minutes

---

## For Admins: Setup GitHub Repository

```bash
cd infra/terraform/github
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars

export TF_VAR_github_token="ghp_your_token"
terraform init
terraform plan
terraform apply
```

**Configures:** Branch protection, secrets, variables, labels

**Time:** ~5 minutes

---

## Verification

```bash
dotnet --version    # 8.x
node --version      # v20.x
python3 --version   # 3.11.x
git --version
task --version

# Build project
cd ../..
task setup
task build
```

---

## Unity Installation (Manual)

1. Download Unity Hub: <https://unity.com/download>
2. Install Unity 2022.3.x
3. Add Android + iOS build support

---

## Troubleshooting

### "Ansible not found"

```bash
pip install ansible        # macOS/Linux
pip install ansible pywinrm # Windows
```

### "WinRM failed" (Windows)

```powershell
Enable-PSRemoting -Force
```

### Package failed

```bash
ansible-playbook playbook.yml --skip-tags problematic_tag
```

---

## Key Commands

```bash
# Ansible
cd infra/ansible
ansible-playbook playbook.yml
ansible-playbook playbook.yml --tags "dotnet,nodejs"

# Terraform
cd infra/terraform/github
terraform init
terraform plan
terraform apply
terraform output
```

---

## Documentation

- [Main README](README.md) - Full guide
- [Ansible README](ansible/README.md) - Ansible details
- [Terraform README](terraform/github/README.md) - Terraform details
