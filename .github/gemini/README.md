# Gemini Code Assist Configuration

This directory contains configuration for Google Gemini Code Assist to provide intelligent code reviews and suggestions.

## Documentation

- [Customize Gemini Behavior](https://developers.google.com/gemini-code-assist/docs/customize-gemini-behavior-github)
- [Code Review Configuration](https://developers.google.com/gemini-code-assist/docs/code-review)

## Files

- `code-review-config.yaml` - Main configuration for code review preferences and rules

## Integration

Gemini Code Assist will automatically:

1. Review pull requests based on project-specific rules
2. Check Unity best practices and performance patterns
3. Validate security requirements
4. Enforce architectural patterns
5. Verify spec-kit workflow compliance

All rules reference the project's canonical rules in `.agent/base/20-rules.md`.
