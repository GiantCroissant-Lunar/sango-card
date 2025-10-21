# Test NODE_OPTIONS Environment Variable

Write-Host "=== NODE_OPTIONS Validation ===" -ForegroundColor Cyan
Write-Host ""

# Check current environment
Write-Host "Current NODE_OPTIONS value:" -ForegroundColor Yellow
if ($env:NODE_OPTIONS) {
    Write-Host "  $env:NODE_OPTIONS" -ForegroundColor Green
} else {
    Write-Host "  NOT SET" -ForegroundColor Red
}
Write-Host ""

# Expected value
$expectedValue = "--max-old-space-size=8192"
Write-Host "Expected value: $expectedValue" -ForegroundColor Yellow
Write-Host ""

# Validation
if ($env:NODE_OPTIONS -eq $expectedValue) {
    Write-Host "✓ NODE_OPTIONS is correctly set!" -ForegroundColor Green
    Write-Host "  Node.js processes will have 8GB memory limit" -ForegroundColor Green
} else {
    Write-Host "⚠ NODE_OPTIONS not set correctly" -ForegroundColor Yellow
    Write-Host "  Setting it now for this session..." -ForegroundColor Yellow
    $env:NODE_OPTIONS = $expectedValue
    Write-Host "  Done! Value is now: $env:NODE_OPTIONS" -ForegroundColor Green
}
Write-Host ""

# Check Node.js version
Write-Host "Node.js version:" -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "  $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "  Node.js not found in PATH" -ForegroundColor Red
}
Write-Host ""

# Test with a simple Node.js script
Write-Host "Testing memory configuration with Node.js..." -ForegroundColor Yellow
try {
    $testScript = @"
const v8 = require('v8');
const heapStats = v8.getHeapStatistics();
const maxHeapMB = Math.round(heapStats.heap_size_limit / 1024 / 1024);
console.log('Max heap size: ' + maxHeapMB + ' MB');
if (maxHeapMB >= 8000) {
    console.log('✓ Memory limit is correctly set (>= 8GB)');
    process.exit(0);
} else {
    console.log('⚠ Memory limit is lower than expected');
    process.exit(1);
}
"@

    $testScript | node
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Memory limit validated successfully!" -ForegroundColor Green
    } else {
        Write-Host "  Memory limit may not be applied correctly" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  Could not test Node.js memory limit" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "=== Validation Complete ===" -ForegroundColor Cyan
