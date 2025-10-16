#!/bin/bash
# Setup shell completion for Task

set -e

echo "Setting up Task shell completion..."
echo ""

# Detect shell
if [ -n "$ZSH_VERSION" ]; then
    SHELL_TYPE="zsh"
    SHELL_RC="$HOME/.zshrc"
    COMPLETION_DIR="$HOME/.zsh/completion"
elif [ -n "$BASH_VERSION" ]; then
    SHELL_TYPE="bash"
    SHELL_RC="$HOME/.bashrc"

    # Determine bash completion directory
    if [ -d "/usr/local/etc/bash_completion.d" ]; then
        COMPLETION_DIR="/usr/local/etc/bash_completion.d"
    elif [ -d "/etc/bash_completion.d" ]; then
        COMPLETION_DIR="/etc/bash_completion.d"
    else
        COMPLETION_DIR="$HOME/.bash_completion.d"
    fi
else
    echo "✗ Unsupported shell. Please set up completion manually."
    exit 1
fi

echo "Detected shell: $SHELL_TYPE"
echo "Completion directory: $COMPLETION_DIR"
echo ""

# Create completion directory if it doesn't exist
mkdir -p "$COMPLETION_DIR"

# Generate completion script
case "$SHELL_TYPE" in
    zsh)
        COMPLETION_FILE="$COMPLETION_DIR/_task"
        task --completion zsh > "$COMPLETION_FILE"

        # Add to fpath if not already there
        if ! grep -q "$COMPLETION_DIR" "$SHELL_RC" 2>/dev/null; then
            echo "" >> "$SHELL_RC"
            echo "# Task completion" >> "$SHELL_RC"
            echo "fpath=($COMPLETION_DIR \$fpath)" >> "$SHELL_RC"
            echo "autoload -U compinit && compinit" >> "$SHELL_RC"
        fi
        ;;

    bash)
        COMPLETION_FILE="$COMPLETION_DIR/task"
        task --completion bash | sudo tee "$COMPLETION_FILE" > /dev/null 2>&1 || task --completion bash > "$COMPLETION_FILE"

        # Source completion in bashrc if not already there
        if ! grep -q "bash_completion.d/task" "$SHELL_RC" 2>/dev/null; then
            echo "" >> "$SHELL_RC"
            echo "# Task completion" >> "$SHELL_RC"
            echo "[ -f \"$COMPLETION_FILE\" ] && . \"$COMPLETION_FILE\"" >> "$SHELL_RC"
        fi
        ;;
esac

echo "✓ Completion script installed to: $COMPLETION_FILE"
echo ""
echo "To activate completion, either:"
echo "  1. Restart your terminal"
echo "  2. Or run: source $SHELL_RC"
echo ""
echo "After activation, you can use TAB completion with task:"
echo "  task <TAB>           # Show available tasks"
echo "  task build<TAB>      # Complete 'build' tasks"
echo "  task --<TAB>         # Show available flags"
