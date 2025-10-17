---
doc_id: DOC-2025-00188
title: Terraform Quickstart
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [terraform-quickstart]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00138
title: Terraform Quickstart
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [terraform-quickstart]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00080
title: Terraform SOPS Quick Start
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:

- terraform
- sops
- quickstart
- infrastructure
summary: 5-minute setup guide for SOPS encrypted secrets with Terraform.

---

# Quick Start Guide - SOPS Encrypted Secrets

This guide will help you set up SOPS encryption for Terraform secrets in 5 minutes.

## Prerequisites Check

Before starting, ensure you have these installed:

```powershell
# Check installations
terraform --version    # Should be >= 1.5.0
sops --version        # Should be >= 3.8.0
age --version         # Should be >= 1.1.0
pwsh --version        # Should be >= 7.0.0
```

If missing, install them:

```powershell
# Windows
choco install terraform sops age powershell-core

# macOS
brew install terraform sops age powershell

# Linux
# See README.md for detailed installation steps
```

## Step-by-Step Setup

### 1. Generate Encryption Keys (1 minute)

```powershell
# Navigate to scripts directory
cd infra\terraform\scripts

# Generate new age key pair
.\New-AgeKeyPair.ps1
```

**Expected output:**

```
Generating age key pair...
✓ Key file created: D:\...\infra\terraform\secrets\age.key

Public key (add this to .sops.yaml):
age1abc123...xyz789

Next steps:
1. Add this public key to infra/terraform/.sops.yaml
...
```

**⚠️ Important:** Copy the displayed public key (starting with `age1...`)

### 2. Configure SOPS (30 seconds)

```powershell
# Edit .sops.yaml
cd ..
notepad .sops.yaml
```

Find this line:

```yaml
- "age1REPLACEME_YOUR_PUBLIC_KEY_HERE"  # pragma: allowlist secret
```

Replace with your actual public key:

```yaml
- "age1abc123...xyz789"  # Your actual public key
```

Save and close the file.

### 3. Create Secrets File (2 minutes)

```powershell
# Copy example file
cd secrets
cp terraform-secrets.json.example terraform-secrets.json

# Edit with your actual secrets
notepad terraform-secrets.json
```

**Minimum required fields:**

```json
{
  "github_owner": "your-github-username-or-org",
  "github_token": "ghp_your_github_personal_access_token",
  "repository_secrets": {
    "UNITY_LICENSE": "your-unity-license-content"
  },
  "repository_variables": {
    "UNITY_VERSION": "2022.3.x"
  }
}
```

**To get GitHub token:**

1. Go to <https://github.com/settings/tokens>
2. Generate new token (classic)
3. Select scopes: `repo`, `admin:org`
4. Copy token and paste in `github_token`

Save and close the file.

### 4. Encrypt Secrets (30 seconds)

```powershell
# Go to scripts directory
cd ..\scripts

# Encrypt the secrets
.\Encrypt-Secrets.ps1
```

**Expected output:**

```
Encrypting secrets...
  Source: D:\...\infra\terraform\secrets\terraform-secrets.json
  Target: D:\...\infra\terraform\secrets\terraform-secrets.json.encrypted

✓ Successfully encrypted secrets to: D:\...\terraform-secrets.json.encrypted

The encrypted file can be safely committed to git.
```

### 5. Test Locally (1 minute)

```powershell
# Decrypt secrets
.\Decrypt-Secrets.ps1

# Apply to Terraform variables
.\Apply-SecretsToTerraform.ps1

# Navigate to GitHub module
cd ..\github

# Initialize Terraform
terraform init

# Preview changes
terraform plan
```

**Success!** You should see a Terraform plan without errors.

### 6. Setup GitHub Actions (1 minute)

For automated CI/CD, add the age key to GitHub:

1. **Copy the age private key:**

   ```powershell
   # Display key content
   Get-Content ..\secrets\age.key | Set-Clipboard
   # Key is now in your clipboard
   ```

2. **Add to GitHub:**
   - Go to your repository on GitHub
   - Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `SOPS_AGE_KEY`
   - Value: Paste the age key from clipboard
   - Click "Add secret"

3. **Commit encrypted secrets:**

   ```powershell
   git add ..\secrets\terraform-secrets.json.encrypted
   git add ..\.sops.yaml
   git commit -m "feat(infra): add encrypted Terraform secrets"
   git push
   ```

4. **Verify workflow:**
   - Go to Actions tab on GitHub
   - Wait for "Terraform CD" workflow to run
   - Check that it completes successfully

## What You've Accomplished ✅

- ✅ Generated age encryption keys
- ✅ Configured SOPS for secret encryption
- ✅ Created and encrypted Terraform secrets
- ✅ Tested Terraform locally
- ✅ Setup GitHub Actions for automated deployments
- ✅ Committed encrypted secrets safely to git

## Common Use Cases

### Updating Secrets

```powershell
# 1. Edit plaintext secrets
cd infra\terraform\secrets
notepad terraform-secrets.json

# 2. Re-encrypt
cd ..\scripts
.\Encrypt-Secrets.ps1 -Force

# 3. Commit
git add ..\secrets\terraform-secrets.json.encrypted
git commit -m "chore(infra): update secrets"
git push
```

### Running Terraform Locally

```powershell
# Decrypt and apply secrets
cd infra\terraform\scripts
.\Decrypt-Secrets.ps1
.\Apply-SecretsToTerraform.ps1

# Run Terraform
cd ..\github
terraform plan
terraform apply
```

### Adding New Secrets

Edit `secrets/terraform-secrets.json`:

```json
{
  "repository_secrets": {
    "EXISTING_SECRET": "value",
    "NEW_SECRET": "new-value"  // Add here
  }
}
```

Then re-encrypt and commit:

```powershell
cd scripts
.\Encrypt-Secrets.ps1 -Force
git add ..\secrets\terraform-secrets.json.encrypted
git commit -m "feat(infra): add NEW_SECRET"
git push
```

## Security Reminders 🔒

### ✅ Safe to Commit

- `secrets/terraform-secrets.json.encrypted` - Encrypted secrets
- `.sops.yaml` - SOPS configuration (contains public key only)

### ❌ NEVER Commit

- `secrets/age.key` - Private encryption key
- `secrets/terraform-secrets.json` - Plaintext secrets
- `github/terraform.tfvars` - Generated variables

These are already in `.gitignore` - just be careful!

## Troubleshooting

### "sops CLI not found"

```powershell
# Windows
choco install sops

# macOS
brew install sops
```

### "age-keygen not found"

```powershell
# Windows
choco install age

# macOS
brew install age
```

### "Failed to decrypt"

Check environment:

```powershell
# Verify key exists
Test-Path infra\terraform\secrets\age.key

# Verify SOPS config
Get-Content infra\terraform\.sops.yaml
```

### Terraform plan fails with "variable not set"

Make sure you've run:

```powershell
cd infra\terraform\scripts
.\Decrypt-Secrets.ps1
.\Apply-SecretsToTerraform.ps1
```

## Next Steps

- 📖 Read full [README.md](README.md) for advanced configuration
- 🔧 Check [scripts/README.md](scripts/README.md) for all script options
- 🏗️ Review [github/README.md](github/README.md) for GitHub module details
- ☁️ Setup [Terraform Cloud](TERRAFORM_CLOUD_SETUP.md) for team collaboration (optional)

## Need Help?

- Review [Troubleshooting](README.md#troubleshooting) in main README
- Check [GitHub Issues](../../issues)
- Read [SOPS documentation](https://github.com/mozilla/sops)

---

**Congratulations! 🎉** Your Terraform secrets are now encrypted and ready for production use.
