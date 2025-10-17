---
doc_id: DOC-2025-00107
title: Terraform Sops Setup Complete
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [terraform-sops-setup-complete]
summary: >
  (Add summary here)
source:
  author: system
---
# SOPS Setup Complete ✅

SOPS (Secrets OPerationS) has been successfully configured for the sango-card project.

## What Was Created

### Configuration Files

- ✅ `.sops.yaml` - SOPS encryption configuration with age
- ✅ `secrets/.gitignore` - Protects plaintext secrets from being committed
- ✅ `secrets/terraform-secrets.json.example` - Example secrets template

### PowerShell Scripts (in `scripts/`)

- ✅ `New-AgeKeyPair.ps1` - Generate age encryption keys
- ✅ `Encrypt-Secrets.ps1` - Encrypt secrets with SOPS
- ✅ `Decrypt-Secrets.ps1` - Decrypt secrets locally
- ✅ `Apply-SecretsToTerraform.ps1` - Apply secrets to Terraform variables

### Documentation

- ✅ `README.md` - Complete infrastructure documentation
- ✅ `QUICKSTART.md` - 5-minute setup guide
- ✅ `scripts/README.md` - Detailed script documentation

### GitHub Workflow

- ✅ `.github/workflows/terraform-cd.yml` - Automated Terraform deployment

### Updated Files

- ✅ `github/.gitignore` - Enhanced to preserve example files

## Next Steps

### 1. Generate Your Encryption Key

```powershell
cd infra\terraform\scripts
.\New-AgeKeyPair.ps1
```

This creates a private key at `secrets/age.key` and displays your public key.

### 2. Update SOPS Configuration

Edit `.sops.yaml` and replace the placeholder with your actual public key:

```yaml
age:
  - "age1your_actual_public_key_here"
```

### 3. Create Your Secrets File

```powershell
cd ..\secrets
cp terraform-secrets.json.example terraform-secrets.json
notepad terraform-secrets.json
```

Add your actual secrets (GitHub token, Unity license, etc.)

### 4. Encrypt and Test

```powershell
cd ..\scripts
.\Encrypt-Secrets.ps1
.\Decrypt-Secrets.ps1
.\Apply-SecretsToTerraform.ps1

cd ..\github
terraform init
terraform plan
```

### 5. Setup GitHub Actions

Add `SOPS_AGE_KEY` secret to your GitHub repository:

- Settings → Secrets and variables → Actions
- New repository secret: `SOPS_AGE_KEY`
- Value: Contents of `secrets/age.key`

### 6. Commit Encrypted Secrets

```powershell
git add infra\terraform\secrets\terraform-secrets.json.encrypted
git add infra\terraform\.sops.yaml
git commit -m "feat(infra): add SOPS encrypted secrets"
git push
```

## How It Works

### Encryption Flow

```
Plaintext secrets → SOPS + age → Encrypted file → Commit to git
(terraform-secrets.json)           (*.encrypted)
     ↓ gitignored                      ↓ safe to commit
```

### CI/CD Flow

```
GitHub Actions
  ↓
Retrieve SOPS_AGE_KEY secret
  ↓
Decrypt terraform-secrets.json.encrypted
  ↓
Apply to Terraform variables
  ↓
Run Terraform plan/apply
  ↓
Cleanup sensitive files
```

## Key Benefits

✅ **Secrets in Git**: Encrypted secrets can be safely committed and versioned
✅ **No Manual Entry**: No need to manually enter secrets in GitHub UI
✅ **Selective Encryption**: Only sensitive keys are encrypted, non-sensitive stay readable
✅ **Team Collaboration**: Multiple team members can use different age keys
✅ **Audit Trail**: Git history shows when secrets were updated
✅ **CI/CD Ready**: Automated decryption in GitHub Actions

## Security Features

🔒 **Encrypted at Rest**: Secrets encrypted with age (modern, secure encryption)
🔒 **Gitignored Plaintext**: All plaintext files automatically excluded
🔒 **Regex-based Encryption**: Only keys matching patterns get encrypted
🔒 **Key Rotation Support**: Easy to rotate encryption keys
🔒 **No Secrets in Logs**: Scripts avoid exposing sensitive data

## Files That Are Safe to Commit

✅ `infra/terraform/.sops.yaml` - Contains only public keys
✅ `infra/terraform/secrets/*.encrypted` - Encrypted secrets
✅ `infra/terraform/secrets/*.sops.json` - Encrypted SOPS files
✅ All documentation and script files

## Files That Must NEVER Be Committed

❌ `infra/terraform/secrets/age.key` - Private encryption key
❌ `infra/terraform/secrets/terraform-secrets.json` - Plaintext secrets
❌ `infra/terraform/github/terraform.tfvars` - Generated variables
❌ `infra/terraform/github/*.tfstate` - Terraform state files

These are protected by `.gitignore` - double-check before committing!

## Documentation

- 📘 **Quick Start**: `QUICKSTART.md` - Get started in 5 minutes
- 📗 **Full Guide**: `README.md` - Complete infrastructure documentation
- 📙 **Scripts**: `scripts/README.md` - Detailed script reference
- 📕 **GitHub Module**: `github/README.md` - GitHub Terraform module docs
- ☁️ **Terraform Cloud**: `TERRAFORM_CLOUD_SETUP.md` - Remote state setup (optional)

## Support

If you encounter issues:

1. Check `QUICKSTART.md` for common setup problems
2. Review `README.md` troubleshooting section
3. Verify prerequisites are installed (`terraform`, `sops`, `age`)
4. Ensure `.gitignore` is protecting sensitive files

## Example Workflow

Daily development workflow:

```powershell
# Make changes to secrets
cd infra\terraform\secrets
notepad terraform-secrets.json

# Re-encrypt
cd ..\scripts
.\Encrypt-Secrets.ps1 -Force

# Test locally
.\Decrypt-Secrets.ps1 -Force
.\Apply-SecretsToTerraform.ps1 -Force
cd ..\github
terraform plan

# Commit and push
git add ..\secrets\terraform-secrets.json.encrypted
git commit -m "chore(infra): update secrets"
git push

# GitHub Actions automatically deploys changes
```

## Architecture

```
sango-card/
├── .github/
│   └── workflows/
│       └── terraform-cd.yml          # Automated deployment
├── infra/
│   └── terraform/
│       ├── .sops.yaml                # SOPS config
│       ├── README.md                 # Main docs
│       ├── QUICKSTART.md             # Setup guide
│       ├── github/                   # GitHub Terraform module
│       │   ├── main.tf
│       │   ├── variables.tf
│       │   └── terraform.tfvars      # Generated (gitignored)
│       ├── secrets/
│       │   ├── .gitignore            # Protects plaintext
│       │   ├── age.key               # Private key (gitignored)
│       │   ├── terraform-secrets.json           # Plaintext (gitignored)
│       │   ├── terraform-secrets.json.example   # Template (committed)
│       │   └── terraform-secrets.json.encrypted # Encrypted (committed)
│       └── scripts/
│           ├── README.md
│           ├── New-AgeKeyPair.ps1
│           ├── Encrypt-Secrets.ps1
│           ├── Decrypt-Secrets.ps1
│           └── Apply-SecretsToTerraform.ps1
```

---

**Status**: ✅ Setup Complete - Ready for first-time configuration
**Created**: 2025-10-17
**Version**: 1.0.0
