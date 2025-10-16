#!/usr/bin/env pwsh
# Install Task (go-task/task) on Windows

$ErrorActionPreference = "Stop"

Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Task Runner Installation Script            ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Check if Task is already installed
$taskInstalled = $null -ne (Get-Command task -ErrorAction SilentlyContinue)

if ($taskInstalled) {
    $version = task --version
    Write-Host "✓ Task is already installed: $version" -ForegroundColor Green
    Write-Host ""
    Write-Host "Run 'task --list' to see available tasks" -ForegroundColor Yellow
    exit 0
}

Write-Host "Task is not installed. Attempting to install..." -ForegroundColor Yellow
Write-Host ""

# Try winget first (Windows 10 1709+ / Windows 11)
if (Get-Command winget -ErrorAction SilentlyContinue) {
    Write-Host "Installing Task via winget..." -ForegroundColor Cyan
    try {
        winget install Task.Task --silent --accept-package-agreements --accept-source-agreements
        Write-Host "✓ Task installed successfully via winget!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Please restart your terminal and run 'task --version' to verify" -ForegroundColor Yellow
        exit 0
    }
    catch {
        Write-Host "✗ winget installation failed, trying alternative methods..." -ForegroundColor Yellow
    }
}

# Try Scoop
if (Get-Command scoop -ErrorAction SilentlyContinue) {
    Write-Host "Installing Task via Scoop..." -ForegroundColor Cyan
    try {
        scoop install task
        Write-Host "✓ Task installed successfully via Scoop!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Run 'task --version' to verify" -ForegroundColor Yellow
        exit 0
    }
    catch {
        Write-Host "✗ Scoop installation failed, trying alternative methods..." -ForegroundColor Yellow
    }
}

# Try Chocolatey
if (Get-Command choco -ErrorAction SilentlyContinue) {
    Write-Host "Installing Task via Chocolatey..." -ForegroundColor Cyan
    try {
        choco install go-task -y
        Write-Host "✓ Task installed successfully via Chocolatey!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Please restart your terminal and run 'task --version' to verify" -ForegroundColor Yellow
        exit 0
    }
    catch {
        Write-Host "✗ Chocolatey installation failed, trying manual installation..." -ForegroundColor Yellow
    }
}

# Manual installation as last resort
Write-Host "No package manager found. Installing manually..." -ForegroundColor Cyan
Write-Host ""

$installDir = "$env:LOCALAPPDATA\Programs\task"
$binPath = "$installDir\task.exe"

# Create directory
New-Item -ItemType Directory -Force -Path $installDir | Out-Null

# Download latest release
Write-Host "Downloading latest Task release..." -ForegroundColor Cyan
$arch = if ([Environment]::Is64BitOperatingSystem) { "amd64" } else { "386" }
$downloadUrl = "https://github.com/go-task/task/releases/latest/download/task_windows_$arch.zip"

$tempZip = "$env:TEMP\task.zip"
$tempDir = "$env:TEMP\task_extract"

try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempZip -UseBasicParsing
    
    # Extract
    Expand-Archive -Path $tempZip -DestinationPath $tempDir -Force
    
    # Move executable
    Copy-Item "$tempDir\task.exe" -Destination $binPath -Force
    
    # Clean up
    Remove-Item $tempZip -Force
    Remove-Item $tempDir -Recurse -Force
    
    # Add to PATH if not already there
    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($currentPath -notlike "*$installDir*") {
        Write-Host "Adding Task to PATH..." -ForegroundColor Cyan
        [Environment]::SetEnvironmentVariable(
            "Path",
            "$currentPath;$installDir",
            "User"
        )
        $env:Path = "$env:Path;$installDir"
    }
    
    Write-Host ""
    Write-Host "✓ Task installed successfully to: $installDir" -ForegroundColor Green
    Write-Host ""
    Write-Host "Please restart your terminal and run 'task --version' to verify" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Quick start:" -ForegroundColor Cyan
    Write-Host "  task --list      # List all available tasks" -ForegroundColor White
    Write-Host "  task setup       # Setup development environment" -ForegroundColor White
    Write-Host "  task build       # Build the project" -ForegroundColor White
    Write-Host "  task dev         # Run complete development workflow" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "✗ Manual installation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Task manually:" -ForegroundColor Yellow
    Write-Host "1. Visit: https://taskfile.dev/installation/" -ForegroundColor White
    Write-Host "2. Download the appropriate binary for your system" -ForegroundColor White
    Write-Host "3. Add it to your PATH" -ForegroundColor White
    exit 1
}
