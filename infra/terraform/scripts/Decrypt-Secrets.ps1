#Requires -Version 7.0
<#
.SYNOPSIS
    Decrypt secrets file using SOPS and age.

.DESCRIPTION
    Decrypts a SOPS-encrypted secrets file using age encryption.
    Requires SOPS_AGE_KEY environment variable or age key file.

.PARAMETER Source
    Path to the encrypted secrets file. Default: infra/terraform/secrets/terraform-secrets.json.encrypted

.PARAMETER Target
    Path for the decrypted output file. Default: infra/terraform/secrets/terraform-secrets.json

.PARAMETER AgeKeyFile
    Path to age private key file. Default: infra/terraform/secrets/age.key
    Can be skipped if SOPS_AGE_KEY environment variable is set.

.PARAMETER Force
    Overwrite target file if it already exists.

.EXAMPLE
    .\Decrypt-Secrets.ps1
    Decrypts using default paths

.EXAMPLE
    $env:SOPS_AGE_KEY = (Get-Content ./age.key -Raw)
    .\Decrypt-Secrets.ps1
    Decrypts using environment variable
#>
[CmdletBinding()]
param(
    [string]$Source = (Join-Path $PSScriptRoot '..' 'secrets' 'terraform-secrets.json.encrypted'),
    [string]$Target = (Join-Path $PSScriptRoot '..' 'secrets' 'terraform-secrets.json'),
    [string]$AgeKeyFile = (Join-Path $PSScriptRoot '..' 'secrets' 'age.key'),
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Check if sops is installed
if (-not (Get-Command sops -ErrorAction SilentlyContinue)) {
    Write-Error @"
sops CLI not found. Please install from:
- Windows: choco install sops or scoop install sops
- macOS: brew install sops
- Linux: Download from https://github.com/mozilla/sops/releases
"@
    exit 1
}

# Verify source file exists
if (-not (Test-Path $Source)) {
    Write-Error "Encrypted source file not found: $Source"
    exit 1
}

# Check if target already exists
if ((Test-Path $Target) -and -not $Force) {
    Write-Error "Target file already exists: $Target`nUse -Force to overwrite"
    exit 1
}

# Set up age key for decryption
if (-not $env:SOPS_AGE_KEY) {
    if (Test-Path $AgeKeyFile) {
        Write-Host "Loading age key from: $AgeKeyFile" -ForegroundColor Cyan
        $env:SOPS_AGE_KEY = Get-Content $AgeKeyFile -Raw
    } else {
        Write-Error @"
Age key not found. Either:
1. Set SOPS_AGE_KEY environment variable with your private key
2. Ensure age key file exists at: $AgeKeyFile
"@
        exit 1
    }
}

Write-Host "Decrypting secrets..." -ForegroundColor Cyan
Write-Host "  Source: $Source" -ForegroundColor Gray
Write-Host "  Target: $Target" -ForegroundColor Gray

# Decrypt with sops
try {
    $decryptedContent = & sops --decrypt $Source 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "SOPS decryption failed: $decryptedContent"
        exit 1
    }

    $decryptedContent | Out-File -Encoding utf8 -FilePath $Target -NoNewline

    Write-Host "`n✓ Successfully decrypted secrets to: $Target" -ForegroundColor Green
    Write-Host "`nWARNING: Keep this plaintext file secure and never commit it to git!" -ForegroundColor Yellow

} catch {
    Write-Error "Decryption failed: $_"
    exit 1
}

exit 0
