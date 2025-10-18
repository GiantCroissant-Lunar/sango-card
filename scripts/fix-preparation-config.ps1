# Fix preparation.json to have correct source paths
# This script updates all package entries to point to their original locations

$configPath = "build/preparation/configs/preparation.json"
$codeQualityPackageCache = "projects/code-quality/Library/PackageCache"

Write-Host "Loading configuration..." -ForegroundColor Cyan
$config = Get-Content $configPath -Raw | ConvertFrom-Json

Write-Host "Scanning PackageCache for hash suffixes..." -ForegroundColor Cyan
$packageDirs = Get-ChildItem $codeQualityPackageCache -Directory

# Create a mapping of package names to their full directory names (with hash)
$packageMap = @{}
foreach ($dir in $packageDirs) {
    $name = $dir.Name
    # Extract package name (everything before @)
    if ($name -match '^(.+)@[a-f0-9]+$') {
        $packageName = $matches[1]
        $packageMap[$packageName] = $name
    }
}

Write-Host "Found $($packageMap.Count) packages in PackageCache" -ForegroundColor Yellow

# Fix package entries
$fixedCount = 0
foreach ($package in $config.packages) {
    # Skip if already pointing to correct location
    if ($package.source -notlike "build/preparation/cache/*") {
        continue
    }

    # Extract package name from cache path
    $cacheName = $package.source -replace '^build/preparation/cache/', ''

    # Find matching package in PackageCache
    if ($packageMap.ContainsKey($cacheName)) {
        $fullDirName = $packageMap[$cacheName]
        $package.source = "$codeQualityPackageCache/$fullDirName"
        $package.target = "build/preparation/cache/$cacheName"
        $fixedCount++
        Write-Host "  Fixed: $cacheName -> $fullDirName" -ForegroundColor Green
    } else {
        Write-Warning "  Not found in PackageCache: $cacheName"
    }
}

Write-Host "`nFixed $fixedCount package entries" -ForegroundColor Cyan

# Fix assembly entries
Write-Host "`nFixing assembly entries..." -ForegroundColor Cyan
$assemblyFixedCount = 0
$assetsPackagesPath = "projects/code-quality/Assets/Packages"

foreach ($assembly in $config.assemblies) {
    # Skip if already pointing to correct location
    if ($assembly.source -notlike "build/preparation/cache/*") {
        continue
    }

    # Extract assembly name from cache path
    $cacheName = $assembly.source -replace '^build/preparation/cache/', ''

    # For assemblies, they're in Assets/Packages as directories
    # The directory name is usually {Name}.{Version} or just the DLL name without extension
    $assemblyNameWithoutExt = [System.IO.Path]::GetFileNameWithoutExtension($cacheName)

    # Check if it's a directory in Assets/Packages
    $possibleDirs = Get-ChildItem $assetsPackagesPath -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "$assemblyNameWithoutExt*" }

    if ($possibleDirs) {
        # Use the first matching directory
        $dirName = $possibleDirs[0].Name
        $assembly.source = "$assetsPackagesPath/$dirName"
        $assembly.target = "build/preparation/cache/$dirName"
        $assemblyFixedCount++
        Write-Host "  Fixed: $cacheName -> $dirName" -ForegroundColor Green
    } else {
        Write-Warning "  Not found in Assets/Packages: $assemblyNameWithoutExt"
    }
}

Write-Host "`nFixed $assemblyFixedCount assembly entries" -ForegroundColor Cyan
Write-Host "Saving configuration..." -ForegroundColor Cyan

# Save with proper formatting
$config | ConvertTo-Json -Depth 100 | Set-Content $configPath -Encoding UTF8

Write-Host "Done!" -ForegroundColor Green
