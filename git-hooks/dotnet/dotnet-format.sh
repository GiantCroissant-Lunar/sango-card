#!/bin/bash
# dotnet format pre-commit hook
# Runs dotnet format on staged C# files to ensure code formatting consistency
# Part of the pre-commit framework - see .pre-commit-config.yaml

set -e

echo "Running dotnet format on staged files..."

# Get all staged .cs files
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' || true)

if [ -z "$STAGED_FILES" ]; then
    echo "No C# files staged for commit. Skipping dotnet format."
    exit 0
fi

# Find .NET solution or project files
SOLUTION_FILE=$(find . -maxdepth 2 -name "*.sln" | head -n 1)
if [ -z "$SOLUTION_FILE" ]; then
    echo "Warning: No solution file found. Checking for project files..."
    PROJECT_FILE=$(find . -maxdepth 3 -name "*.csproj" | head -n 1)
    if [ -z "$PROJECT_FILE" ]; then
        echo "Error: No .sln or .csproj file found"
        exit 1
    fi
    TARGET="$PROJECT_FILE"
else
    TARGET="$SOLUTION_FILE"
fi

# Create a temporary file list for dotnet format
TEMP_FILE_LIST=$(mktemp)
echo "$STAGED_FILES" > "$TEMP_FILE_LIST"

# Run dotnet format with verification
echo "Formatting C# files with dotnet format..."
if dotnet format "$TARGET" --verify-no-changes --include $(echo "$STAGED_FILES" | tr '\n' ',') > /dev/null 2>&1; then
    echo "✓ All staged files are properly formatted"
    rm -f "$TEMP_FILE_LIST"
    exit 0
else
    echo "✗ Formatting issues detected. Running auto-fix..."
    dotnet format "$TARGET" --include $(echo "$STAGED_FILES" | tr '\n' ',')

    # Re-stage the formatted files
    echo "$STAGED_FILES" | xargs git add

    echo "✓ Files have been formatted and re-staged"
    echo "  Please review the changes and commit again"
    rm -f "$TEMP_FILE_LIST"
    exit 0
fi
