#!/bin/bash
# Quick setup script for Unix-based systems (macOS/Linux)
# Sango Card development environment setup

set -e

echo "=== Sango Card Development Environment Setup ==="
echo ""

# Check if Ansible is installed
if ! command -v ansible-playbook &> /dev/null; then
    echo "Ansible not found. Installing..."
    
    # Try to install with pip
    if command -v pip3 &> /dev/null; then
        pip3 install ansible
    elif command -v pip &> /dev/null; then
        pip install ansible
    else
        echo "ERROR: pip not found. Please install Python and pip first."
        exit 1
    fi
fi

echo "Ansible version: $(ansible --version | head -n1)"
echo ""

# Run the playbook
echo "Running Ansible playbook..."
ansible-playbook -i inventory/hosts playbook.yml "$@"

echo ""
echo "=== Setup Complete ==="
echo "Please restart your terminal or source your shell configuration:"
echo "  source ~/.bashrc  # or ~/.zshrc"
echo ""
echo "Next steps:"
echo "  1. Install Unity Hub from https://unity.com/download"
echo "  2. Install Unity 2022.3.x with Android and iOS build support"
echo "  3. Run: task setup"
echo "  4. Run: task build"
