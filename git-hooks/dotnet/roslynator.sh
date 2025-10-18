#!/bin/bash
# Roslynator pre-commit hook
# Runs Roslynator analyzers on staged C# files to enforce code quality rules
# Part of the pre-commit framework - see .pre-commit-config.yaml
# Requires: dotnet tool install -g roslynator.dotnet.cli

set -e

echo "Running Roslynator analysis on staged files..."

# Check if roslynator CLI is installed
if ! command -v roslynator &> /dev/null; then
    echo "Error: Roslynator CLI is not installed"
    echo "Install with: dotnet tool install -g roslynator.dotnet.cli"
    exit 1
fi

# Get all staged .cs files
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' || true)

if [ -z "$STAGED_FILES" ]; then
    echo "No C# files staged for commit. Skipping Roslynator."
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

# Create output directory
OUTPUT_DIR=".roslynator-temp"
mkdir -p "$OUTPUT_DIR"
OUTPUT_FILE="$OUTPUT_DIR/roslynator-results.xml"

# Run roslynator analyze
echo "Analyzing code with Roslynator..."
set +e  # Don't exit on non-zero, we'll handle it
roslynator analyze "$TARGET" \
    --severity-level warning \
    --output "$OUTPUT_FILE" \
    --file-log-verbosity detailed \
    --execution-time

EXIT_CODE=$?
set -e

if [ $EXIT_CODE -eq 0 ]; then
    echo "✓ No Roslynator issues found"
    rm -rf "$OUTPUT_DIR"
    exit 0
elif [ $EXIT_CODE -eq 1 ]; then
    # Exit code 1 means diagnostics were found
    echo "✗ Roslynator found code quality issues"

    # Try to extract and display issues
    if [ -f "$OUTPUT_FILE" ]; then
        echo "  Review the full report at: $OUTPUT_FILE"
        echo ""

        # Try to show summary if available
        if command -v xmllint &> /dev/null; then
            ISSUE_COUNT=$(xmllint --xpath 'count(//Issue)' "$OUTPUT_FILE" 2>/dev/null || echo "unknown")
            echo "  Total issues: $ISSUE_COUNT"
        fi
    fi

    # Show staged files that likely have issues
    echo ""
    echo "  Staged files analyzed:"
    echo "$STAGED_FILES" | sed 's/^/  - /'
    echo ""
    echo "  Fix the issues or use: git commit --no-verify to bypass"
    exit 1
else
    # Other error codes (2 = other error)
    echo "✗ Roslynator analysis failed with exit code: $EXIT_CODE"
    echo "  Check the output above for details"
    exit 1
fi
