## Description

<!-- Briefly describe what this PR does and why -->

## Type of Change

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Refactoring (no functional changes)
- [ ] Build/CI configuration change

## Documentation Changes

- [ ] ✅ No documentation changes
- [ ] ✅ Documentation added to `docs/_inbox/` with proper front-matter
- [ ] ✅ Updated existing canonical documentation (listed below)
- [ ] ✅ Checked `docs/index/registry.json` for existing docs before creating new
- [ ] ⚠️  **Created docs outside `docs/_inbox/`** (explain why below)

**If you created/modified docs, list them here:**
-

-

**Documentation Rules:**

- All new docs go to `docs/_inbox/` first
- Include YAML front-matter (see `docs/DOCUMENTATION-SCHEMA.md`)
- Check registry before creating new docs
- Prefer updating canonical docs over creating new ones
- See `.agent/base/40-documentation.md` for complete rules

## Testing

- [ ] Tests added/updated
- [ ] All tests pass locally (`task test`)
- [ ] Manual testing completed (describe below)

**Test coverage:**
<!-- Describe what you tested -->

## Checklist

- [ ] Code follows project style guidelines (R-CODE-xxx)
- [ ] Build succeeds locally (`task clean && task build`)
- [ ] Pre-commit hooks pass
- [ ] Commit messages follow conventional commits format
- [ ] No secrets or sensitive data committed
- [ ] Unity .meta files committed alongside assets (if applicable)
- [ ] Documentation updated if needed

## Related Issues/Specs

Closes #
Related to #
Implements spec: SPEC-XXX

## Screenshots (if applicable)

<!-- Add screenshots for UI changes -->

## Additional Notes

<!-- Any additional context, concerns, or questions -->

---

**For Reviewers:**

- [ ] Code quality and style
- [ ] Test coverage
- [ ] Documentation completeness
- [ ] Security concerns
- [ ] Performance impact
- [ ] Breaking changes communicated
