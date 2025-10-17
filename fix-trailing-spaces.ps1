# Fix trailing spaces in markdown files
$files = @(
    '.specify/COORDINATION-STATUS.md',
    'scripts/docs_validate.py'
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "Fixing trailing spaces in: $file"
        $content = Get-Content $file -Raw
        $content = $content -replace ' +(\r?\n)', '$1'
        Set-Content $file -Value $content -NoNewline
    }
}

Write-Host "Done!"
