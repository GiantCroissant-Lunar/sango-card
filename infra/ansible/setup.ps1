# Quick setup script for Windows
# Sango Card development environment setup

Write-Host "=== Sango Card Development Environment Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "WARNING: Not running as Administrator. Some installations may fail." -ForegroundColor Yellow
    Write-Host "Consider running PowerShell as Administrator for best results." -ForegroundColor Yellow
    Write-Host ""
}

# Check if Ansible is installed
$ansibleInstalled = Get-Command ansible-playbook -ErrorAction SilentlyContinue

if (-not $ansibleInstalled) {
    Write-Host "Ansible not found. Installing..." -ForegroundColor Yellow

    # Check if pip is available
    $pipInstalled = Get-Command pip -ErrorAction SilentlyContinue

    if ($pipInstalled) {
        pip install ansible pywinrm
    } else {
        Write-Host "ERROR: pip not found. Please install Python first." -ForegroundColor Red
        Write-Host "Download from: https://www.python.org/downloads/" -ForegroundColor Yellow
        exit 1
    }
}

# Display Ansible version
$ansibleVersion = ansible --version 2>&1 | Select-Object -First 1
Write-Host "Ansible version: $ansibleVersion" -ForegroundColor Green
Write-Host ""

# Ensure WinRM is configured
Write-Host "Checking WinRM configuration..." -ForegroundColor Cyan
try {
    Test-WSMan -ErrorAction Stop | Out-Null
    Write-Host "WinRM is configured" -ForegroundColor Green
} catch {
    Write-Host "Configuring WinRM for Ansible..." -ForegroundColor Yellow
    if ($isAdmin) {
        Enable-PSRemoting -Force
        winrm set winrm/config/service/auth '@{Basic="true"}'
        winrm set winrm/config/service '@{AllowUnencrypted="true"}'
        Write-Host "WinRM configured successfully" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Administrator privileges required to configure WinRM" -ForegroundColor Red
        Write-Host "Please run this script as Administrator" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
Write-Host "Running Ansible playbook..." -ForegroundColor Cyan

# Run the playbook
ansible-playbook -i inventory/hosts playbook.yml $args

Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host "Please restart your terminal to use the newly installed tools." -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Install Unity Hub from https://unity.com/download" -ForegroundColor White
Write-Host "  2. Install Unity 2022.3.x with Android and iOS build support" -ForegroundColor White
Write-Host "  3. Run: task setup" -ForegroundColor White
Write-Host "  4. Run: task build" -ForegroundColor White
