#Requires -Version 7.0
<#
.SYNOPSIS
    Generate a new age key pair for SOPS encryption.

.DESCRIPTION
    Creates a new age encryption key pair and saves it to the specified output file.
    The public key is displayed and should be added to .sops.yaml configuration.

.PARAMETER OutFile
    Path where the private key file will be saved. Default: infra/terraform/secrets/age.key

.EXAMPLE
    .\New-AgeKeyPair.ps1
    Generates key at default location

.EXAMPLE
    .\New-AgeKeyPair.ps1 -OutFile ./custom-location/age.key
    Generates key at custom location
#>
[CmdletBinding()]
param(
    [string]$OutFile = (Join-Path $PSScriptRoot '..' 'secrets' 'age.key')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Check if age-keygen is installed
if (-not (Get-Command age-keygen -ErrorAction SilentlyContinue)) {
    Write-Error @"
age-keygen not found. Please install age from:
- Windows: choco install age or scoop install age
- macOS: brew install age
- Linux: apt install age or download from https://github.com/FiloSottile/age/releases
"@
    exit 1
}

# Check if file already exists
if (Test-Path $OutFile) {
    Write-Error "Key file already exists: $OutFile`nIf you want to generate a new key, delete the existing file first."
    exit 1
}

# Create directory if it doesn't exist
$outDir = Split-Path $OutFile -Parent
if (-not (Test-Path $outDir)) {
    Write-Host "Creating directory: $outDir" -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

# Generate key
Write-Host "Generating age key pair..." -ForegroundColor Cyan
$keyContent = & age-keygen 2>&1

# Save to file
$keyContent | Out-File -Encoding utf8 -FilePath $OutFile -NoNewline

Write-Host "`n✓ Key file created: $OutFile" -ForegroundColor Green
Write-Host "`nIMPORTANT: Keep this file secure and never commit it to git!" -ForegroundColor Yellow

# Extract and display public key
$publicKeyLine = $keyContent | Select-String 'public key:' | Select-Object -First 1
if ($publicKeyLine) {
    $publicKey = ($publicKeyLine -split 'public key:\s*')[1].Trim()
    Write-Host "`nPublic key (add this to .sops.yaml):" -ForegroundColor Cyan
    Write-Host $publicKey -ForegroundColor White

    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Add this public key to infra/terraform/.sops.yaml" -ForegroundColor White
    Write-Host "2. Create infra/terraform/secrets/terraform-secrets.json with your secrets" -ForegroundColor White
    Write-Host "3. Run: .\Encrypt-Secrets.ps1 to encrypt the secrets" -ForegroundColor White
    Write-Host "4. Add AGE_KEY secret to GitHub repository with the content of $OutFile" -ForegroundColor White
}

exit 0
