#!/bin/bash
# JetBrains InspectCode pre-commit hook
# Runs JetBrains inspectcode on staged C# files to detect code quality issues
# Part of the pre-commit framework - see .pre-commit-config.yaml
# Requires: JetBrains CLI tools (jb) installed

set -e

echo "Running JetBrains InspectCode on staged files..."

# Check if jb CLI is installed
if ! command -v jb &> /dev/null; then
    echo "Warning: JetBrains CLI (jb) is not installed - skipping InspectCode"
    echo "Install from: https://www.jetbrains.com/help/resharper/InspectCode.html"
    echo "Or via dotnet tool: dotnet tool install -g JetBrains.ReSharper.GlobalTools"
    exit 0  # Exit successfully to allow commit
fi

# Get all staged .cs files
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' || true)

if [ -z "$STAGED_FILES" ]; then
    echo "No C# files staged for commit. Skipping InspectCode."
    exit 0
fi

# Find .NET solution file
SOLUTION_FILE=$(find . -maxdepth 2 -name "*.sln" | head -n 1)
if [ -z "$SOLUTION_FILE" ]; then
    echo "Error: No solution file found. InspectCode requires a .sln file"
    exit 1
fi

# Create output directory
OUTPUT_DIR=".inspectcode-temp"
mkdir -p "$OUTPUT_DIR"
OUTPUT_FILE="$OUTPUT_DIR/inspect-results.xml"

# Run inspectcode
echo "Analyzing code quality with InspectCode..."
jb inspectcode "$SOLUTION_FILE" \
    --output="$OUTPUT_FILE" \
    --format=Xml \
    --severity=WARNING \
    --build

# Check if there are any issues
if [ -f "$OUTPUT_FILE" ]; then
    # Count issues (excluding INFO level)
    ISSUE_COUNT=$(grep -c '<Issue ' "$OUTPUT_FILE" 2>/dev/null || echo "0")

    if [ "$ISSUE_COUNT" -gt 0 ]; then
        echo "✗ InspectCode found $ISSUE_COUNT issue(s)"
        echo "  Review the report at: $OUTPUT_FILE"
        echo ""
        echo "  Top issues:"
        grep '<Issue ' "$OUTPUT_FILE" | head -n 10 | sed 's/.*Message="\([^"]*\)".*/  - \1/'
        echo ""
        echo "To bypass this check, use: git commit --no-verify"
        exit 1
    else
        echo "✓ No code quality issues found"
        rm -rf "$OUTPUT_DIR"
        exit 0
    fi
else
    echo "Error: InspectCode output file not found"
    exit 1
fi
