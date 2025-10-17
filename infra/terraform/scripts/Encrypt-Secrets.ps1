#Requires -Version 7.0
<#
.SYNOPSIS
    Encrypt secrets file using SOPS and age.

.DESCRIPTION
    Encrypts a plaintext JSON secrets file using SOPS with age encryption.
    The .sops.yaml configuration determines which keys get encrypted.

.PARAMETER Source
    Path to the plaintext secrets file. Default: infra/terraform/secrets/terraform-secrets.json

.PARAMETER Target
    Path for the encrypted output file. Default: infra/terraform/secrets/terraform-secrets.json.encrypted

.PARAMETER Force
    Overwrite target file if it already exists.

.EXAMPLE
    .\Encrypt-Secrets.ps1
    Encrypts using default paths

.EXAMPLE
    .\Encrypt-Secrets.ps1 -Source ./secrets/my-secrets.json -Target ./secrets/my-secrets.json.encrypted -Force
    Encrypts with custom paths and overwrites existing file
#>
[CmdletBinding()]
param(
    [string]$Source = (Join-Path $PSScriptRoot '..' 'secrets' 'terraform-secrets.json'),
    [string]$Target = (Join-Path $PSScriptRoot '..' 'secrets' 'terraform-secrets.json.encrypted'),
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
    Write-Error "Source file not found: $Source`n`nCreate it first by copying and editing:`n  cp $(Join-Path (Split-Path $Source) 'terraform-secrets.json.example') $Source"
    exit 1
}

# Check if target already exists
if ((Test-Path $Target) -and -not $Force) {
    Write-Error "Target file already exists: $Target`nUse -Force to overwrite"
    exit 1
}

# Verify .sops.yaml exists
$sopsConfig = Join-Path $PSScriptRoot '..' '.sops.yaml'
if (-not (Test-Path $sopsConfig)) {
    Write-Error ".sops.yaml not found at: $sopsConfig`nPlease create it first with your age public key"
    exit 1
}

# Check if age key is configured in .sops.yaml
$sopsContent = Get-Content $sopsConfig -Raw
if ($sopsContent -match 'age1REPLACEME' -or $sopsContent -notmatch 'age1[a-z0-9]{58}') {
    Write-Error @"
.sops.yaml appears to have placeholder age key.
Please generate an age key with New-AgeKeyPair.ps1 and update .sops.yaml with your public key.
"@
    exit 1
}

# Validate source is valid JSON
try {
    $null = Get-Content $Source -Raw | ConvertFrom-Json
} catch {
    Write-Error "Source file is not valid JSON: $Source`nError: $_"
    exit 1
}

Write-Host "Encrypting secrets..." -ForegroundColor Cyan
Write-Host "  Source: $Source" -ForegroundColor Gray
Write-Host "  Target: $Target" -ForegroundColor Gray

# Encrypt with sops
try {
    $encryptedContent = & sops --encrypt $Source 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "SOPS encryption failed: $encryptedContent"
        exit 1
    }

    $encryptedContent | Out-File -Encoding utf8 -FilePath $Target -NoNewline

    Write-Host "`n✓ Successfully encrypted secrets to: $Target" -ForegroundColor Green
    Write-Host "`nThe encrypted file can be safely committed to git." -ForegroundColor Cyan
    Write-Host "Keep the plaintext file ($Source) secure and never commit it!" -ForegroundColor Yellow

} catch {
    Write-Error "Encryption failed: $_"
    exit 1
}

exit 0
