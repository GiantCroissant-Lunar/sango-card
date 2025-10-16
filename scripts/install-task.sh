#!/bin/bash
# Install Task (go-task/task) on Linux/macOS

set -e

echo "╔════════════════════════════════════════════════╗"
echo "║     Task Runner Installation Script            ║"
echo "╚════════════════════════════════════════════════╝"
echo ""

# Check if Task is already installed
if command -v task &> /dev/null; then
    VERSION=$(task --version)
    echo "✓ Task is already installed: $VERSION"
    echo ""
    echo "Run 'task --list' to see available tasks"
    exit 0
fi

echo "Task is not installed. Attempting to install..."
echo ""

# Detect OS
OS="$(uname -s)"
case "$OS" in
    Linux*)     OS_TYPE=Linux;;
    Darwin*)    OS_TYPE=Mac;;
    *)          OS_TYPE="UNKNOWN:$OS"
esac

# macOS installation
if [ "$OS_TYPE" = "Mac" ]; then
    # Try Homebrew
    if command -v brew &> /dev/null; then
        echo "Installing Task via Homebrew..."
        brew install go-task/tap/go-task
        echo ""
        echo "✓ Task installed successfully via Homebrew!"
        echo ""
        echo "Run 'task --version' to verify"
        exit 0
    fi
fi

# Linux installation
if [ "$OS_TYPE" = "Linux" ]; then
    # Try snap
    if command -v snap &> /dev/null; then
        echo "Installing Task via snap..."
        sudo snap install task --classic
        echo ""
        echo "✓ Task installed successfully via snap!"
        echo ""
        echo "Run 'task --version' to verify"
        exit 0
    fi
fi

# Universal installation script (works on both Linux and macOS)
echo "Installing Task using official install script..."
echo ""

INSTALL_DIR="${HOME}/.local/bin"
mkdir -p "$INSTALL_DIR"

# Download and run official install script
sh -c "$(curl --location https://taskfile.dev/install.sh)" -- -d -b "$INSTALL_DIR"

# Add to PATH if not already there
if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
    echo ""
    echo "Adding Task to PATH..."
    
    # Detect shell
    if [ -n "$ZSH_VERSION" ]; then
        SHELL_RC="$HOME/.zshrc"
    elif [ -n "$BASH_VERSION" ]; then
        SHELL_RC="$HOME/.bashrc"
    else
        SHELL_RC="$HOME/.profile"
    fi
    
    echo "export PATH=\"\$PATH:$INSTALL_DIR\"" >> "$SHELL_RC"
    export PATH="$PATH:$INSTALL_DIR"
fi

echo ""
echo "✓ Task installed successfully to: $INSTALL_DIR"
echo ""
echo "Please restart your terminal or run: source $SHELL_RC"
echo "Then run 'task --version' to verify"
echo ""
echo "Quick start:"
echo "  task --list      # List all available tasks"
echo "  task setup       # Setup development environment"
echo "  task build       # Build the project"
echo "  task dev         # Run complete development workflow"
