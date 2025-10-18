# Build Preparation Script
# Usage: .\build-prepare.ps1 [ConfigName] [Target]
# Example: .\build-prepare.ps1 preparation PrepareCache
# Example: .\build-prepare.ps1 injection PrepareClient

param(
    # Config name (without .json extension)
    [Parameter(Position=0, HelpMessage="Config name (e.g., preparation, injection)")]
    [ValidateNotNullOrEmpty()]
    [string]$ConfigName = "preparation",

    # Build target
    [Parameter(Position=1, HelpMessage="Build target (PrepareCache, PrepareClient, BuildClient)")]
    [ValidateNotNullOrEmpty()]
    [string]$Target = "PrepareCache"
)

# Show help if requested
if ($args -contains '-?' -or $args -contains '-Help') {
    Write-Host "Build Preparation Script"
    Write-Host ""
    Write-Host "Usage: .\build-prepare.ps1 [ConfigName] [Target]"
    Write-Host ""
    Write-Host "Parameters:"
    Write-Host "  ConfigName: Config file name without .json (default: preparation)"
    Write-Host "              Available: preparation, injection"
    Write-Host "  Target:     Build target (default: PrepareCache)"
    Write-Host "              Available: PrepareCache, PrepareClient, BuildClient"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\build-prepare.ps1 preparation PrepareCache"
    Write-Host "  .\build-prepare.ps1 injection PrepareClient"
    Write-Host ""
    Write-Host "Targets:"
    Write-Host "  PrepareCache   - Populate cache from code-quality (safe, no client changes)"
    Write-Host "  PrepareClient  - Inject to client (performs git reset, build-time only)"
    Write-Host "  BuildClient    - Full build with preparation"
    exit 0
}

# Construct config path
$ConfigPath = "build/preparation/configs/$ConfigName.json"

Write-Host "=== Build Preparation ===" -ForegroundColor Cyan
Write-Host "Config: $ConfigPath" -ForegroundColor Yellow
Write-Host "Target: $Target" -ForegroundColor Yellow
Write-Host ""

# Run NUKE build with parameters
& "$PSScriptRoot\..\build.ps1" $Target --PreparationConfig $ConfigPath
