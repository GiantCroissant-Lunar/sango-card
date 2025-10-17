#Requires -Version 7.0
<#
.SYNOPSIS
    Apply decrypted secrets to Terraform variables.

.DESCRIPTION
    Reads decrypted secrets JSON and generates terraform.tfvars file for local use,
    or exports as environment variables for CI/CD use.

.PARAMETER SecretsFile
    Path to decrypted secrets JSON. Default: infra/terraform/secrets/terraform-secrets.json

.PARAMETER OutputTfvars
    Path to output terraform.tfvars file. Default: infra/terraform/github/terraform.tfvars

.PARAMETER ExportEnvVars
    Export secrets as TF_VAR_* environment variables instead of creating tfvars file.

.PARAMETER Force
    Overwrite output file if it already exists.

.EXAMPLE
    .\Apply-SecretsToTerraform.ps1
    Creates terraform.tfvars file from secrets

.EXAMPLE
    .\Apply-SecretsToTerraform.ps1 -ExportEnvVars
    Exports secrets as environment variables for current session
#>
[CmdletBinding()]
param(
    [string]$SecretsFile = (Join-Path $PSScriptRoot '..' 'secrets' 'terraform-secrets.json'),
    [string]$OutputTfvars = (Join-Path $PSScriptRoot '..' 'github' 'terraform.tfvars'),
    [switch]$ExportEnvVars,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Verify secrets file exists
if (-not (Test-Path $SecretsFile)) {
    Write-Error @"
Secrets file not found: $SecretsFile

Please decrypt first:
  .\Decrypt-Secrets.ps1
"@
    exit 1
}

# Read and parse secrets
Write-Host "Reading secrets from: $SecretsFile" -ForegroundColor Cyan
try {
    $secrets = Get-Content $SecretsFile -Raw | ConvertFrom-Json
} catch {
    Write-Error "Failed to parse secrets JSON: $_"
    exit 1
}

if ($ExportEnvVars) {
    # Export as environment variables
    Write-Host "`nExporting Terraform variables as environment variables..." -ForegroundColor Cyan

    # Simple string variables
    if ($secrets.github_owner) {
        $env:TF_VAR_github_owner = $secrets.github_owner
        Write-Host "  Exported TF_VAR_github_owner" -ForegroundColor Gray
    }

    if ($secrets.github_token) {
        $env:TF_VAR_github_token = $secrets.github_token
        Write-Host "  Exported TF_VAR_github_token (sensitive)" -ForegroundColor Gray
    }

    # Map variables - convert to HCL format
    if ($secrets.repository_secrets) {
        $secretsHcl = ($secrets.repository_secrets.PSObject.Properties | ForEach-Object {
            "  `"$($_.Name)`" = `"$($_.Value -replace '"', '\"')`""
        }) -join "`n"
        $env:TF_VAR_repository_secrets = "{`n$secretsHcl`n}"
        Write-Host "  Exported TF_VAR_repository_secrets (sensitive)" -ForegroundColor Gray
    }

    if ($secrets.repository_variables) {
        $varsHcl = ($secrets.repository_variables.PSObject.Properties | ForEach-Object {
            "  `"$($_.Name)`" = `"$($_.Value -replace '"', '\"')`""
        }) -join "`n"
        $env:TF_VAR_repository_variables = "{`n$varsHcl`n}"
        Write-Host "  Exported TF_VAR_repository_variables" -ForegroundColor Gray
    }

    Write-Host "`n✓ Environment variables exported for current session" -ForegroundColor Green

} else {
    # Generate terraform.tfvars file
    if ((Test-Path $OutputTfvars) -and -not $Force) {
        Write-Error "Output file already exists: $OutputTfvars`nUse -Force to overwrite"
        exit 1
    }

    Write-Host "`nGenerating terraform.tfvars..." -ForegroundColor Cyan

    $tfvarsContent = @"
# Auto-generated from encrypted secrets
# DO NOT COMMIT THIS FILE

github_owner = "$($secrets.github_owner)"
github_token = "$($secrets.github_token)"

repository_secrets = {
"@

    foreach ($prop in $secrets.repository_secrets.PSObject.Properties) {
        $value = $prop.Value -replace '"', '\"' -replace '\n', '\n' -replace '\r', ''
        $tfvarsContent += "`n  $($prop.Name) = `"$value`""
    }

    $tfvarsContent += "`n}`n`nrepository_variables = {"

    foreach ($prop in $secrets.repository_variables.PSObject.Properties) {
        $value = $prop.Value -replace '"', '\"'
        $tfvarsContent += "`n  $($prop.Name) = `"$value`""
    }

    $tfvarsContent += "`n}"

    # Write to file
    $tfvarsContent | Out-File -Encoding utf8 -FilePath $OutputTfvars -NoNewline

    Write-Host "`n✓ Created: $OutputTfvars" -ForegroundColor Green
    Write-Host "`nWARNING: This file contains sensitive data. Never commit it to git!" -ForegroundColor Yellow
    Write-Host "It should be listed in .gitignore" -ForegroundColor Yellow
}

exit 0
