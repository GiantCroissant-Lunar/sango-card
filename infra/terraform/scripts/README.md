# Terraform Scripts

PowerShell scripts for managing encrypted secrets and Terraform workflow.

## Prerequisites

- PowerShell 7.0+
- [SOPS](https://github.com/mozilla/sops) - Secret encryption tool
- [age](https://github.com/FiloSottile/age) - Modern encryption tool

### Installation

**Windows:**

```powershell
choco install sops age
# or
scoop install sops age
```

**macOS:**

```bash
brew install sops age
```

**Linux:**

```bash
# Ubuntu/Debian
sudo apt install age
# SOPS from releases
wget https://github.com/mozilla/sops/releases/download/v3.8.1/sops-v3.8.1.linux.amd64
sudo mv sops-v3.8.1.linux.amd64 /usr/local/bin/sops
sudo chmod +x /usr/local/bin/sops
```

## Quick Start

### 1. Generate Age Key Pair

```powershell
.\New-AgeKeyPair.ps1
```

This creates `infra/terraform/secrets/age.key` and displays your public key.

### 2. Update SOPS Configuration

Copy the public key and update `infra/terraform/.sops.yaml`:

```yaml
creation_rules:
  - age:
      - "age1your_public_key_here"  # Replace this line
```

### 3. Create Secrets File

```powershell
# Copy example
cp ..\secrets\terraform-secrets.json.example ..\secrets\terraform-secrets.json

# Edit with your actual secrets
notepad ..\secrets\terraform-secrets.json
```

### 4. Encrypt Secrets

```powershell
.\Encrypt-Secrets.ps1
```

This creates `terraform-secrets.json.encrypted` which can be safely committed to git.

### 5. Use in CI/CD

Add your age private key as GitHub secret:

1. Go to repository Settings → Secrets and variables → Actions
2. Create new secret: `SOPS_AGE_KEY`
3. Paste entire content of `infra/terraform/secrets/age.key`

## Scripts Reference

### New-AgeKeyPair.ps1

Generate a new age encryption key pair.

```powershell
.\New-AgeKeyPair.ps1 [-OutFile <path>]
```

**Parameters:**

- `-OutFile`: Output path for private key (default: `../secrets/age.key`)

**Output:**

- Creates age private key file
- Displays public key for `.sops.yaml` configuration

### Encrypt-Secrets.ps1

Encrypt secrets file using SOPS and age.

```powershell
.\Encrypt-Secrets.ps1 [-Source <path>] [-Target <path>] [-Force]
```

**Parameters:**

- `-Source`: Plaintext secrets file (default: `../secrets/terraform-secrets.json`)
- `-Target`: Encrypted output file (default: `../secrets/terraform-secrets.json.encrypted`)
- `-Force`: Overwrite existing target file

**Requirements:**

- `.sops.yaml` must be configured with your age public key
- Source file must exist and be valid JSON

### Decrypt-Secrets.ps1

Decrypt SOPS-encrypted secrets file.

```powershell
.\Decrypt-Secrets.ps1 [-Source <path>] [-Target <path>] [-AgeKeyFile <path>] [-Force]
```

**Parameters:**

- `-Source`: Encrypted secrets file (default: `../secrets/terraform-secrets.json.encrypted`)
- `-Target`: Decrypted output file (default: `../secrets/terraform-secrets.json`)
- `-AgeKeyFile`: Age private key file (default: `../secrets/age.key`)
- `-Force`: Overwrite existing target file

**Environment Variables:**

- `SOPS_AGE_KEY`: Can be used instead of `-AgeKeyFile` (useful for CI/CD)

### Apply-SecretsToTerraform.ps1

Apply decrypted secrets to Terraform.

```powershell
.\Apply-SecretsToTerraform.ps1 [-SecretsFile <path>] [-OutputTfvars <path>] [-ExportEnvVars] [-Force]
```

**Parameters:**

- `-SecretsFile`: Decrypted secrets JSON (default: `../secrets/terraform-secrets.json`)
- `-OutputTfvars`: Output tfvars file (default: `../github/terraform.tfvars`)
- `-ExportEnvVars`: Export as environment variables instead of file
- `-Force`: Overwrite existing output file

**Modes:**

1. **File mode** (default): Creates `terraform.tfvars` file

   ```powershell
   .\Apply-SecretsToTerraform.ps1
   cd ..\github
   terraform plan
   ```

2. **Environment variable mode**: Exports `TF_VAR_*` variables

   ```powershell
   .\Apply-SecretsToTerraform.ps1 -ExportEnvVars
   cd ..\github
   terraform plan  # Uses environment variables
   ```

## Workflows

### Local Development

```powershell
# One-time setup
.\New-AgeKeyPair.ps1

# Update .sops.yaml with public key
# Create and edit secrets file

# Encrypt and apply
.\Encrypt-Secrets.ps1
.\Decrypt-Secrets.ps1
.\Apply-SecretsToTerraform.ps1

# Run Terraform
cd ..\github
terraform init
terraform plan
terraform apply
```

### Updating Secrets

```powershell
# 1. Edit plaintext secrets
notepad ..\secrets\terraform-secrets.json

# 2. Re-encrypt
.\Encrypt-Secrets.ps1 -Force

# 3. Commit encrypted file
git add ..\secrets\terraform-secrets.json.encrypted
git commit -m "Update encrypted secrets"
```

### CI/CD Pipeline

The GitHub Actions workflow automatically:

1. Checks out repository
2. Installs Terraform and SOPS
3. Writes `SOPS_AGE_KEY` secret to key file
4. Decrypts `terraform-secrets.json.encrypted`
5. Applies secrets to Terraform variables
6. Runs Terraform init/plan/apply

See `.github/workflows/terraform-cd.yml` for implementation.

## Security Best Practices

### ✅ DO

- **Keep age private key secure** - Never commit to git
- **Use strong GitHub secrets** - Store `SOPS_AGE_KEY` securely
- **Rotate keys regularly** - Generate new keys periodically
- **Audit encrypted files** - Review changes before committing
- **Use .gitignore properly** - Ensure plaintext files are excluded

### ❌ DON'T

- **Don't commit plaintext secrets** - Always encrypt first
- **Don't share private keys** - Use separate keys per team member if needed
- **Don't skip encryption** - Even for non-production environments
- **Don't store keys in code** - Use environment variables or secure storage

## Key Rotation

When rotating encryption keys:

1. Generate new key pair:

   ```powershell
   .\New-AgeKeyPair.ps1 -OutFile ..\secrets\age-new.key
   ```

2. Add new public key to `.sops.yaml` (keep old one):

   ```yaml
   age:
     - "age1old_key"
     - "age1new_key"
   ```

3. Re-encrypt with both keys:

   ```powershell
   .\Encrypt-Secrets.ps1 -Force
   ```

4. Update GitHub secret `SOPS_AGE_KEY` with new key

5. Remove old public key from `.sops.yaml` and re-encrypt:

   ```powershell
   .\Encrypt-Secrets.ps1 -Force
   ```

## Troubleshooting

### Error: "sops CLI not found"

Install SOPS:

```powershell
choco install sops  # Windows
brew install sops   # macOS
```

### Error: "age-keygen not found"

Install age:

```powershell
choco install age  # Windows
brew install age   # macOS
```

### Error: "Failed to decrypt"

Check:

1. `SOPS_AGE_KEY` environment variable is set correctly
2. Age key file exists and is readable
3. Encrypted file was encrypted with your public key
4. SOPS configuration (`.sops.yaml`) is correct

### Error: "No age key found"

Ensure:

- Age private key exists at expected location
- Or `SOPS_AGE_KEY` environment variable is set with key content

## Additional Resources

- [SOPS Documentation](https://github.com/mozilla/sops)
- [age Documentation](https://github.com/FiloSottile/age)
- [Terraform Variables](https://developer.hashicorp.com/terraform/language/values/variables)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
