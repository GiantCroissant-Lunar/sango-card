# Sango Card - Terraform Infrastructure

Infrastructure as Code for the Sango Card project using Terraform.

## Overview

This directory contains Terraform configurations for managing:

- **GitHub**: Repository settings, branch protection, secrets, variables, and labels
- **GCP**: Google Cloud Platform resources (future)
- **Scripts**: Helper scripts for secret management with SOPS encryption

## Structure

```
infra/terraform/
├── .sops.yaml                      # SOPS encryption configuration
├── README.md                       # This file
├── main.tf                         # Root module configuration
├── github/                         # GitHub repository management
│   ├── main.tf                     # GitHub resources
│   ├── variables.tf                # Input variables
│   ├── outputs.tf                  # Output values
│   ├── versions.tf                 # Provider versions
│   ├── backend.tf                  # Terraform Cloud backend
│   ├── providers.tf                # Provider configuration
│   ├── terraform.tfvars.example    # Example variables
│   └── README.md                   # GitHub module docs
├── gcp/                            # Google Cloud Platform (future)
├── secrets/                        # Encrypted secrets directory
│   ├── .gitignore                  # Ignore plaintext files
│   ├── terraform-secrets.json.example    # Example secrets file
│   └── terraform-secrets.json.encrypted  # Encrypted secrets (committed)
└── scripts/                        # Helper scripts
    ├── README.md                   # Scripts documentation
    ├── New-AgeKeyPair.ps1          # Generate encryption keys
    ├── Encrypt-Secrets.ps1         # Encrypt secrets with SOPS
    ├── Decrypt-Secrets.ps1         # Decrypt secrets
    └── Apply-SecretsToTerraform.ps1 # Apply to Terraform vars
```

## Documentation

📚 **Complete Guides:**

- **[Terraform SOPS Quick Start](../../docs/guides/terraform-quickstart.md)** - 5-minute setup guide for SOPS encrypted secrets
- **[Terraform Security Guide](../../docs/guides/terraform-security.md)** - Security best practices for encrypted secrets
- **[Terraform Cloud Setup](../../docs/guides/terraform-cloud-setup.md)** - Complete Terraform Cloud backend setup

## Quick Start

### Prerequisites

- [Terraform](https://www.terraform.io/downloads) >= 1.5.0
- [SOPS](https://github.com/mozilla/sops) >= 3.8.0
- [age](https://github.com/FiloSottile/age) >= 1.1.0
- PowerShell 7.0+ (for helper scripts)
- GitHub Personal Access Token with `repo` and `admin:org` permissions

### Installation

**Windows:**

```powershell
choco install terraform sops age
# or
scoop install terraform sops age
```

**macOS:**

```bash
brew install terraform sops age
```

**Linux:**

```bash
# Terraform
wget -O- https://apt.releases.hashicorp.com/gpg | sudo gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/hashicorp.list
sudo apt update && sudo apt install terraform

# SOPS
wget https://github.com/mozilla/sops/releases/download/v3.8.1/sops-v3.8.1.linux.amd64
sudo mv sops-v3.8.1.linux.amd64 /usr/local/bin/sops
sudo chmod +x /usr/local/bin/sops

# age
sudo apt install age
```

### Setup

1. **Generate encryption keys:**

   ```powershell
   cd scripts
   .\New-AgeKeyPair.ps1
   ```

2. **Update SOPS configuration:**

   Copy the displayed public key and update `.sops.yaml`:

   ```yaml
   age:
     - "age1your_public_key_here"  # Replace with your actual public key
   ```

3. **Create secrets file:**

   ```powershell
   cp secrets\terraform-secrets.json.example secrets\terraform-secrets.json
   # Edit secrets\terraform-secrets.json with your actual values
   ```

4. **Encrypt secrets:**

   ```powershell
   cd scripts
   .\Encrypt-Secrets.ps1
   ```

5. **Configure GitHub:**

   Add `SOPS_AGE_KEY` secret to your GitHub repository:
   - Go to Settings → Secrets and variables → Actions
   - New repository secret: `SOPS_AGE_KEY`
   - Paste contents of `secrets/age.key`

6. **Apply with Terraform:**

   ```powershell
   cd scripts
   .\Decrypt-Secrets.ps1
   .\Apply-SecretsToTerraform.ps1

   cd ..\github
   terraform init
   terraform plan
   terraform apply
   ```

## Workflows

### Local Development

```powershell
# Decrypt and apply secrets
cd infra\terraform\scripts
.\Decrypt-Secrets.ps1
.\Apply-SecretsToTerraform.ps1

# Run Terraform
cd ..\github
terraform init
terraform plan
terraform apply
```

### CI/CD (GitHub Actions)

The `.github/workflows/terraform-cd.yml` workflow automatically:

1. Installs Terraform and SOPS
2. Decrypts secrets using `SOPS_AGE_KEY` GitHub secret
3. Applies secrets to Terraform variables
4. Runs `terraform plan` on PRs (with plan comment)
5. Runs `terraform apply` on main branch pushes

### Updating Secrets

```powershell
# 1. Edit plaintext secrets
notepad secrets\terraform-secrets.json

# 2. Re-encrypt
cd scripts
.\Encrypt-Secrets.ps1 -Force

# 3. Commit encrypted file
git add ..\secrets\terraform-secrets.json.encrypted
git commit -m "chore(infra): update encrypted secrets"
git push
```

## Modules

### GitHub Module

Manages GitHub repository configuration:

- Repository settings and topics
- Branch protection rules (main, develop)
- GitHub Actions secrets and variables
- Issue labels
- CODEOWNERS file (optional)

See [github/README.md](github/README.md) for detailed documentation.

### GCP Module (Future)

Will manage Google Cloud Platform resources for:

- Cloud Run services
- Cloud Storage buckets
- Cloud SQL databases
- IAM permissions

## Security

### Best Practices

✅ **DO:**

- Keep age private key (`secrets/age.key`) secure
- Always encrypt secrets before committing
- Use `.gitignore` to exclude plaintext files
- Rotate encryption keys periodically
- Use Terraform Cloud for remote state
- Enable branch protection on main

❌ **DON'T:**

- Never commit plaintext secrets
- Don't share age private keys
- Don't store keys in code or git history
- Don't skip encryption for any environment

### Encrypted Files

These files are safe to commit (encrypted with SOPS):

- `secrets/terraform-secrets.json.encrypted`
- Any file matching `*.encrypted` or `*.sops.json`

These files must NEVER be committed (gitignored):

- `secrets/age.key` (private encryption key)
- `secrets/terraform-secrets.json` (plaintext secrets)
- `secrets/*.json` (any plaintext JSON)
- `github/terraform.tfvars` (generated variables file)
- `*.tfstate*` (Terraform state files)

### Key Rotation

See [scripts/README.md](scripts/README.md#key-rotation) for key rotation procedures.

## Terraform Cloud / State Backend

By default, state is stored locally. For team collaboration, use Terraform Cloud.

**Quick Setup:**

1. Create account at [app.terraform.io](https://app.terraform.io)
2. Create organization and workspace named `sango-card-github`
3. Update `github/backend.tf` with your organization name
4. Run: `terraform login && terraform init -migrate-state`

**📘 See [Terraform Cloud Setup Guide](../../docs/guides/terraform-cloud-setup.md) for complete setup**

This guide covers:

- Detailed setup steps with screenshots
- Local vs Remote execution modes
- Variable management options
- Team collaboration setup
- S3 backend alternative
- Switching between backends
- Troubleshooting tips

## Troubleshooting

### Common Issues

**Error: "sops CLI not found"**

```powershell
# Install SOPS
choco install sops  # Windows
brew install sops   # macOS
```

**Error: "age-keygen not found"**

```powershell
# Install age
choco install age  # Windows
brew install age   # macOS
```

**Error: "Failed to decrypt"**

Check:

1. `SOPS_AGE_KEY` environment variable is set
2. Age key file exists at `secrets/age.key`
3. `.sops.yaml` has correct public key
4. Encrypted file was created with your key

**Terraform Init fails**

Ensure you're in the correct directory:

```bash
cd infra/terraform/github  # Not infra/terraform
terraform init
```

### Debug Mode

Enable Terraform debug logging:

```powershell
$env:TF_LOG = "DEBUG"
terraform plan
```

Enable SOPS debug output:

```powershell
$env:SOPS_DEBUG = "1"
sops -d secrets/terraform-secrets.json.encrypted
```

## Scripts Documentation

See [scripts/README.md](scripts/README.md) for detailed documentation on:

- `New-AgeKeyPair.ps1` - Generate encryption keys
- `Encrypt-Secrets.ps1` - Encrypt secrets with SOPS
- `Decrypt-Secrets.ps1` - Decrypt secrets
- `Apply-SecretsToTerraform.ps1` - Apply to Terraform variables

## Resources

- [Terraform Documentation](https://www.terraform.io/docs)
- [SOPS Documentation](https://github.com/mozilla/sops)
- [age Documentation](https://github.com/FiloSottile/age)
- [Terraform GitHub Provider](https://registry.terraform.io/providers/integrations/github/latest/docs)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

## Support

For issues or questions:

1. Check [Troubleshooting](#troubleshooting) section
2. Review [scripts/README.md](scripts/README.md)
3. Check GitHub module [README](github/README.md)
4. Open an issue in the repository
