# Sango Card - GitHub Terraform Module

Manages GitHub repository configuration as code for the Sango Card project.

## Features

- ✅ Repository settings and topics
- ✅ Branch protection (main + develop)
- ✅ GitHub Actions secrets
- ✅ GitHub Actions variables  
- ✅ Issue labels
- ✅ CODEOWNERS file (optional)
- ✅ Terraform Cloud backend support

## Quick Start

```bash
# 1. Copy example configuration
cp terraform.tfvars.example terraform.tfvars

# 2. Edit terraform.tfvars with your settings
#    - github_owner
#    - repository_secrets
#    - repository_variables

# 3. Set GitHub token (don't commit!)
export TF_VAR_github_token="ghp_your_token_here"

# 4. Initialize and apply
terraform init
terraform plan
terraform apply
```

## Prerequisites

- Terraform >= 1.5.0
- GitHub Personal Access Token with:
  - `repo` scope (full repository access)
  - `admin:org` scope (for organization repositories)

## Configuration

### Required Variables

```hcl
github_owner = "your-org-or-username"
github_token = "ghp_..."  # Or use environment variable
```

### Secrets Configuration

```hcl
repository_secrets = {
  # Unity
  UNITY_LICENSE = "your-license-content"
  UNITY_EMAIL = "your-email"
  UNITY_PASSWORD = "your-password"
  
  # Android
  ANDROID_KEYSTORE_BASE64 = "base64-encoded-keystore"
  ANDROID_KEYSTORE_PASS = "keystore-password"
  
  # iOS (if building for iOS)
  IOS_CERTIFICATE_BASE64 = "base64-certificate"
  IOS_PROVISION_BASE64 = "base64-provisioning-profile"
}
```

### Variables Configuration

```hcl
repository_variables = {
  UNITY_VERSION = "2022.3.x"
  DOTNET_VERSION = "8.0.x"
  BUILD_CONFIGURATION = "Release"
  BUILD_ANDROID = "true"
  BUILD_IOS = "true"
}
```

## Terraform Cloud Setup

1. Create account at [app.terraform.io](https://app.terraform.io)
2. Create organization
3. Edit `backend.tf`:
   ```hcl
   terraform {
     cloud {
       organization = "your-org"
       workspaces {
         name = "sango-card-github"
       }
     }
   }
   ```
4. Login: `terraform login`
5. Initialize: `terraform init`

## Common Tasks

### View Current Configuration

```bash
terraform output
```

### Update GitHub Settings

```bash
# 1. Edit terraform.tfvars
# 2. Preview changes
terraform plan

# 3. Apply changes
terraform apply
```

### Add New Secret

```bash
# Edit terraform.tfvars
repository_secrets = {
  # ... existing secrets ...
  NEW_SECRET = "value"
}

# Apply
terraform plan
terraform apply
```

## Outputs

After applying, view outputs:

```bash
terraform output repository_html_url
terraform output protected_branches
terraform output issue_labels
```

## Troubleshooting

### Authentication Error

```bash
# Verify token is set
echo $TF_VAR_github_token

# Test token manually
curl -H "Authorization: token $TF_VAR_github_token" https://api.github.com/user
```

### State Lock

```bash
# Force unlock (use with caution)
terraform force-unlock <lock-id>
```

### Provider Version

```bash
# Update providers
terraform init -upgrade
```

## Security

- **Never commit** `terraform.tfvars` (gitignored)
- **Use environment variables** for sensitive data
- **Rotate tokens** regularly (quarterly)
- **Use Terraform Cloud** for encrypted state storage

## Resources Managed

| Resource | Description |
|----------|-------------|
| `github_repository` | Repository settings |
| `github_branch_protection` | Branch protection rules |
| `github_actions_secret` | GitHub Actions secrets |
| `github_actions_variable` | GitHub Actions variables |
| `github_issue_label` | Issue labels |
| `github_repository_file` | CODEOWNERS file (optional) |

## Documentation

- [Terraform GitHub Provider](https://registry.terraform.io/providers/integrations/github/latest/docs)
- [Terraform Cloud Docs](https://developer.hashicorp.com/terraform/cloud-docs)
- [GitHub API](https://docs.github.com/en/rest)

## Support

For issues, check the main [infra README](../../README.md) or open an issue.
