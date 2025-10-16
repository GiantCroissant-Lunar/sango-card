#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-commit hook to enforce partial class interface separation pattern (R-CODE-090).

.DESCRIPTION
    Validates that classes implementing multiple interfaces follow the pattern:
    - Base file (ClassName.cs) contains only parent class inheritance
    - Each interface in separate file (ClassName.IInterfaceName.cs)
    
    This hook is part of the enforcement strategy for R-CODE-090.

.NOTES
    Rule: R-CODE-090 - Partial Class Interface Separation
    Documentation: docs/CODING-PATTERNS.md
    Enforcement: Pre-commit hook + Roslyn analyzer (future)
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# ANSI color codes for better output
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = $Reset
    )
    Write-Host "$Color$Message$Reset"
}

function Test-PartialClassPattern {
    <#
    .SYNOPSIS
        Checks if staged C# files follow the partial class interface separation pattern.
    #>
    
    Write-ColorOutput "🔍 Checking partial class interface separation pattern (R-CODE-090)..." $Blue
    
    # Get staged .cs files
    $stagedFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { $_ -match '\.cs$' }
    
    if (-not $stagedFiles) {
        Write-ColorOutput "✅ No C# files staged for commit." $Green
        return $true
    }
    
    $violations = @()
    
    foreach ($file in $stagedFiles) {
        if (-not (Test-Path $file)) {
            continue
        }
        
        $content = Get-Content $file -Raw
        
        # Regex to find class declarations with multiple interfaces
        # Matches: class ClassName : BaseClass, IInterface1, IInterface2
        # or: class ClassName : IInterface1, IInterface2
        $pattern = '(?m)^\s*(partial\s+)?class\s+(\w+)\s*:\s*([^{]+?)(?={)'
        
        if ($content -match $pattern) {
            $classDeclaration = $Matches[3].Trim()
            $className = $Matches[2]
            
            # Split by comma to count inheritance items
            $inheritanceItems = $classDeclaration -split ',' | ForEach-Object { $_.Trim() }
            
            if ($inheritanceItems.Count -gt 1) {
                # Check if this is a base file (ClassName.cs) or interface file (ClassName.IInterfaceName.cs)
                $fileName = [System.IO.Path]::GetFileNameWithoutExtension($file)
                $baseName = $fileName -split '\.' | Select-Object -First 1
                
                # If file is ClassName.cs and has multiple items, it's likely a violation
                # unless all items after first are also in separate files
                if ($fileName -eq $className) {
                    # This is the base file - should only have one inheritance
                    $interfaces = $inheritanceItems | Select-Object -Skip 1
                    
                    if ($interfaces.Count -gt 0) {
                        $violations += @{
                            File = $file
                            Class = $className
                            Message = "Class '$className' implements multiple interfaces in base file. Each interface should be in a separate partial class file."
                            Interfaces = $interfaces
                            ExpectedFiles = $interfaces | ForEach-Object { 
                                # Remove 'I' prefix from interface name for file naming
                                $interfaceName = $_ -replace '^I', ''
                                "$className.$interfaceName.cs"
                            }
                        }
                    }
                }
            }
        }
    }
    
    if ($violations.Count -gt 0) {
        Write-ColorOutput "`n❌ Partial class pattern violations detected (R-CODE-090):`n" $Red
        
        foreach ($violation in $violations) {
            Write-ColorOutput "  File: $($violation.File)" $Yellow
            Write-ColorOutput "  Class: $($violation.Class)" $Yellow
            Write-ColorOutput "  Issue: $($violation.Message)" $Red
            Write-ColorOutput "  Interfaces: $($violation.Interfaces -join ', ')" $Yellow
            Write-ColorOutput ""
            Write-ColorOutput "  Expected structure:" $Blue
            Write-ColorOutput "    - $($violation.Class).cs → partial class $($violation.Class) : BaseClass" $Blue
            if ($violation.ExpectedFiles) {
                foreach ($expectedFile in $violation.ExpectedFiles) {
                    Write-ColorOutput "    - $expectedFile → partial class $($violation.Class) : [Interface]" $Blue
                }
            }
            else {
                foreach ($interface in $violation.Interfaces) {
                    $interfaceName = $interface -replace '^I', ''
                    Write-ColorOutput "    - $($violation.Class).$interfaceName.cs → partial class $($violation.Class) : $interface" $Blue
                }
            }
            Write-ColorOutput ""
        }
        
        Write-ColorOutput "See docs/CODING-PATTERNS.md for details on R-CODE-090 pattern." $Yellow
        Write-ColorOutput "To bypass this check (not recommended): git commit --no-verify`n" $Yellow
        
        return $false
    }
    
    Write-ColorOutput "✅ All staged C# files follow partial class pattern." $Green
    return $true
}

# Main execution
try {
    $result = Test-PartialClassPattern
    
    if (-not $result) {
        exit 1
    }
    
    exit 0
}
catch {
    Write-ColorOutput "❌ Pre-commit hook error: $_" $Red
    Write-ColorOutput "Stack trace: $($_.ScriptStackTrace)" $Red
    exit 1
}
