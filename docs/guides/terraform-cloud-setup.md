---
doc_id: DOC-2025-00082
title: Terraform Cloud Setup Guide
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:
- terraform
- terraform-cloud
- setup
summary: Complete guide for setting up Terraform Cloud backend.
---

# Terraform Cloud Setup Guide

Complete guide for setting up Terraform Cloud with the sango-card project.

## Why Terraform Cloud?

Terraform Cloud provides:

- ✅ **Remote state storage** - Encrypted, versioned, and shared across team
- ✅ **State locking** - Prevents concurrent modifications
- ✅ **Version history** - Rollback to previous states
- ✅ **Team collaboration** - Role-based access control
- ✅ **Remote runs** - Execute Terraform in the cloud (optional)
- ✅ **Free tier** - Up to 500 resources, 5 users

## Prerequisites

- Terraform Cloud account (free at [app.terraform.io](https://app.terraform.io))
- Terraform CLI installed locally
- SOPS setup completed (see `QUICKSTART.md`)

## Step-by-Step Setup

### 1. Create Terraform Cloud Account

1. Go to <https://app.terraform.io>
2. Sign up with GitHub, GitLab, or email
3. Verify your email address

### 2. Create Organization

If you don't have an organization:

1. Click "Create an organization"
2. Enter organization name (e.g., `your-company`)
3. Choose plan (Free is fine)
4. Click "Create organization"

### 3. Create Workspace

1. In your organization, click "New workspace"
2. Choose workflow:
   - **CLI-driven workflow** (Recommended) - Run Terraform locally, store state remotely
   - **VCS-driven workflow** - Automatic runs on git push (optional)
3. Enter workspace name: `sango-card-github`
4. Click "Create workspace"

### 4. Configure Workspace Settings

#### Execution Mode (Recommended: Local)

1. In workspace, go to Settings → General
2. Set "Execution Mode" to **Local**
3. This runs Terraform on your machine but stores state remotely
4. Save settings

#### Terraform Version

1. Settings → General
2. Set "Terraform Version" to `1.9.x` or `Latest`
3. Enable "Auto-apply" if desired (applies changes automatically after plan)

### 5. Configure Variables in Terraform Cloud

You have two options for managing secrets:

#### Option A: Use SOPS (Recommended)

Continue using SOPS for local development and CI/CD. Terraform Cloud only stores state.

**No additional configuration needed** - your existing SOPS setup works!

#### Option B: Store Variables in Terraform Cloud

Store variables directly in Terraform Cloud UI:

1. Go to workspace → Variables
2. Add Terraform variables:
   - `github_owner` (Terraform variable)
   - `github_token` (Terraform variable, mark as Sensitive)
   - `repository_secrets` (HCL, Sensitive)
   - `repository_variables` (HCL)

Example HCL format for `repository_secrets`:

```hcl
{
  UNITY_LICENSE = "your-license-content"
  UNITY_EMAIL = "email@example.com"
}
```

### 6. Update Backend Configuration

Edit `infra/terraform/github/backend.tf`:

```hcl
terraform {
  cloud {
    organization = "your-org-name"  # Replace with your org name

    workspaces {
      name = "sango-card-github"
    }
  }
}
```

Comment out the local backend:

```hcl
# terraform {
#   backend "local" {
#     path = "terraform.tfstate"
#   }
# }
```

### 7. Authenticate Terraform CLI

Run the login command:

```powershell
terraform login
```

This will:

1. Open browser for authentication
2. Generate API token
3. Save token locally (~/.terraform.d/credentials.tfrc.json)

Or manually create token:

1. Terraform Cloud → User Settings → Tokens
2. Create new token
3. Save to `~/.terraform.d/credentials.tfrc.json`:

   ```json
   {
     "credentials": {
       "app.terraform.io": {
         "token": "your-token-here"
       }
     }
   }
   ```

### 8. Initialize with Terraform Cloud

```powershell
cd infra\terraform\github

# Initialize and migrate state
terraform init -migrate-state

# Terraform will ask: Do you want to copy existing state to the new backend?
# Answer: yes
```

### 9. Verify Setup

```powershell
# Plan should work normally
terraform plan

# State is now stored in Terraform Cloud
# Check workspace UI to see state
```

### 10. Configure GitHub Actions (Optional)

If using GitHub Actions with Terraform Cloud:

1. Create Terraform Cloud **Team API Token**:
   - Organization Settings → API Tokens
   - Create team token
   - Copy token

2. Add to GitHub secrets:
   - Repository Settings → Secrets → New secret
   - Name: `TF_API_TOKEN`
   - Value: (paste token)

3. Update `.github/workflows/terraform-cd.yml`:

   ```yaml
   - name: Setup Terraform
     uses: hashicorp/setup-terraform@v3
     with:
       terraform_version: 1.9.x
       cli_config_credentials_token: ${{ secrets.TF_API_TOKEN }}
   ```

## Workflow Comparison

### Local Backend (Default)

```powershell
cd infra\terraform\scripts
.\Decrypt-Secrets.ps1
.\Apply-SecretsToTerraform.ps1

cd ..\github
terraform init
terraform plan
terraform apply
```

State stored in `terraform.tfstate` (local file, gitignored).

### Terraform Cloud Backend

```powershell
cd infra\terraform\scripts
.\Decrypt-Secrets.ps1
.\Apply-SecretsToTerraform.ps1

cd ..\github
terraform init      # Connects to TF Cloud
terraform plan      # Plan stored in cloud
terraform apply     # State stored in cloud
```

State stored in Terraform Cloud (encrypted, versioned, shared).

## Team Collaboration

### Adding Team Members

1. Terraform Cloud → Organization Settings → Teams
2. Create team (e.g., "developers", "admins")
3. Set team permissions:
   - **Read**: View workspace and state
   - **Plan**: Run plans
   - **Write**: Apply changes
   - **Admin**: Full workspace access

4. Invite team members:
   - Organization Settings → Users
   - Send invites

### Workspace Access

Each team member needs:

1. Terraform Cloud account
2. Added to organization
3. Added to team with workspace access
4. Run `terraform login` locally

## Remote Execution (Optional)

To run Terraform in Terraform Cloud instead of locally:

1. Workspace Settings → General
2. Set "Execution Mode" to **Remote**
3. Configure Terraform Cloud to run plans/applies

Benefits:

- Consistent execution environment
- No need to install Terraform locally
- Automatic runs on VCS push (if VCS-connected)

Drawbacks:

- Slower than local execution
- Requires uploading code to cloud
- More complex SOPS integration

**Recommendation**: Use **Local** execution mode with SOPS.

## Cost Considerations

### Free Tier (Sufficient for Most Use Cases)

- Up to 500 resources
- 5 users
- Remote state storage
- State locking
- Version history
- Community support

### Paid Plans (Not needed for sango-card)

- More resources
- More users
- Advanced features (SSO, audit logging, etc.)

## Switching Between Backends

### From Local to Terraform Cloud

```powershell
# 1. Update backend.tf (as shown above)
# 2. Initialize with migration
terraform init -migrate-state
# Answer 'yes' when prompted
```

### From Terraform Cloud back to Local

```powershell
# 1. Comment out cloud block in backend.tf
# 2. Uncomment local backend
# 3. Initialize with migration
terraform init -migrate-state
# Answer 'yes' when prompted
```

## Troubleshooting

### Error: "No valid credential sources found"

```powershell
# Re-authenticate
terraform login
```

### Error: "Backend initialization required"

```powershell
terraform init -reconfigure
```

### Error: "Workspace not found"

Verify:

1. Organization name is correct in `backend.tf`
2. Workspace exists in Terraform Cloud
3. You have access to the workspace

### State Lock Issues

If state is locked from a failed run:

1. Go to workspace in Terraform Cloud UI
2. Settings → State → Force unlock
3. Or wait for lock to timeout (default: 15 minutes)

## Best Practices

### ✅ DO

- Use **CLI-driven workflow** with local execution
- Keep SOPS for secret management
- Use Terraform Cloud only for state storage
- Enable workspace locking for critical changes
- Review state versions before rollback
- Use separate workspaces per environment (dev, staging, prod)

### ❌ DON'T

- Don't commit `.terraform/` directory
- Don't store secrets in Terraform Cloud variables (use SOPS)
- Don't use remote execution if you need SOPS decryption
- Don't share API tokens (each user should have their own)

## Alternative: S3 Backend

If you prefer AWS S3 instead of Terraform Cloud:

See `backend.tf` for S3 configuration example.

Setup:

```bash
# Create S3 bucket
aws s3 mb s3://your-terraform-state-bucket

# Create DynamoDB table for locking
aws dynamodb create-table \
  --table-name terraform-state-lock \
  --attribute-definitions AttributeName=LockID,AttributeType=S \
  --key-schema AttributeName=LockID,KeyType=HASH \
  --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

# Initialize Terraform
terraform init -migrate-state
```

## Summary

**For sango-card project, recommended setup:**

1. ✅ **Terraform Cloud** for state storage (free tier)
2. ✅ **Local execution mode** (run Terraform on your machine)
3. ✅ **SOPS** for secret management (existing setup)
4. ✅ **CLI-driven workflow** (manual runs)

This gives you:

- Secure, shared state storage
- No additional complexity
- Existing SOPS workflow continues to work
- Easy team collaboration

**Not recommended:**

- ❌ Remote execution (complicates SOPS integration)
- ❌ VCS-driven workflow (automatic runs on push - too aggressive for infra)
- ❌ Storing secrets in TF Cloud (SOPS is better)

## Next Steps

After setup:

1. ✅ Run `terraform plan` to verify connection
2. ✅ Check Terraform Cloud UI to see state
3. ✅ Share organization invite with team members
4. ✅ Document your organization and workspace names in team wiki

## Resources

- [Terraform Cloud Documentation](https://developer.hashicorp.com/terraform/cloud-docs)
- [CLI-Driven Workflow](https://developer.hashicorp.com/terraform/cloud-docs/run/cli)
- [Terraform Cloud Free Tier](https://www.hashicorp.com/products/terraform/pricing)
- [Team Management](https://developer.hashicorp.com/terraform/cloud-docs/users-teams-organizations/teams)
