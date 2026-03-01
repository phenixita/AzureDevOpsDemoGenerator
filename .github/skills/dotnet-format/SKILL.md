---
name: dotnet-format
description: "Format C# code using dotnet format. Use when: preparing code for commits, enforcing code style consistency, checking formatting compliance, or linting a .NET solution before deployment."
argument-hint: "[solution-path] [--verify-no-changes] [--include <files>]"
---

# dotnet Format Utility Runner

Format C# code across your .NET solution to maintain consistent code style, enforce organizational standards, and prepare code for commits.

## When to Use

- **Before commits**: Format the entire solution to ensure style compliance
- **During development**: Format specific files to apply formatting rules
- **Pre-deployment**: Verify all code is formatted correctly before pushing to main
- **CI/CD integration**: Check formatting as part of code quality gates

## Procedure

### Format Entire Solution (Default)

1. Run the skill with your solution path:
   ```
   /dotnet-format src/VSTSDemoGeneratorV2.sln
   ```
2. Review the formatting changes suggested by the tool
3. If satisfied, stage and commit the changes:
   ```
   git add .
   git commit -m "style: apply dotnet format"
   ```

### Verify Without Auto-Fixing

Check if code is formatted correctly without making changes:

```
/dotnet-format src/VSTSDemoGeneratorV2.sln --verify-no-changes
```

This runs as a lint check suitable for CI pipelines.

### Format Specific Files or Projects

Format only particular projects or file patterns:

```
/dotnet-format src/VSTSDemoGeneratorV2.sln --include "src/VstsDemoBuilder/**"
```

## Implementation Details

The skill invokes the [format-solution.ps1](./scripts/format-solution.ps1) PowerShell script, which:

1. Validates the solution path exists
2. Runs `dotnet format` with specified options
3. Reports formatting changes and status
4. Returns exit code for CI/CD pipeline integration

### Configuration

Apply formatting rules from [.editorconfig](./assets/.editorconfig), which defines:
- Indentation (spaces vs. tabs)
- Line length and wrapping
- Brace placement and spacing
- Naming conventions
- Code style rules

For detailed rules and customization, see [editorconfig-guide.md](./references/editorconfig-guide.md).

## Quick Reference

| Command | Purpose |
|---------|---------|
| `/dotnet-format` | Format the solution with default config |
| `/dotnet-format --verify-no-changes` | Check formatting without changes |
| `/dotnet-format --include "path/**"` | Format specific files |
| `dotnet format --help` | Show all dotnet format options |

## Tips

- **Save before formatting**: Ensure unsaved changes are committed or stashed first
- **Review changes**: Always review what dotnet format changed before committing
- **Team alignment**: Commit `.editorconfig` to the repository so all team members use consistent rules
- **Pre-commit hook**: Consider adding formatting as a git pre-commit hook to prevent style violations from being committed

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "dotnet format not found" | Install tool: `dotnet tool install -g dotnet-format` |
| "Solution file not found" | Verify path is relative to workspace root |
| "formatting produced no changes" | Code is already formatted correctly |
| "unexpected formatting" | Check `.editorconfig` rules match your team style |
