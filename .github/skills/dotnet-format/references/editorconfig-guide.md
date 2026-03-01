# EditorConfig Customization Guide

This guide explains the formatting rules defined in [.editorconfig](../assets/.editorconfig) and how to customize them for your team's style.

## What is EditorConfig?

EditorConfig enforces consistent coding styles across your project. `dotnet format` reads these rules to apply formatting automatically.

## Common Customizations

### Indentation

Change `indent_size` to adjust tab width:

```ini
[*.cs]
indent_size = 4  # 2, 3, 4, or 8
```

### Line Length

Some rules enforce line length limits (typically 80-120 characters). Adjust based on team preference:

```ini
[*.cs]
max_line_length = 120
```

### Brace Style

**Allman style** (opening brace on new line):
```csharp
if (condition)
{
    DoSomething();
}
```

**K&R style** (opening brace on same line):
```csharp
if (condition) {
    DoSomething();
}
```

Configure in EditorConfig:
```ini
csharp_indent_braces = true  # Allman
csharp_indent_braces = false # K&R
```

### Null-Coalescing vs Conditional

```ini
# Prefer ?? over ?:
csharp_style_throw_expression = true
csharp_style_conditional_delegate_call = true
```

### Variable Declaration

```ini
# Prefer explicit types
csharp_style_var_elsewhere = false

# Allow var for obvious types
csharp_style_var_when_type_is_apparent = true
```

### Naming Conventions

Define custom naming rules for your organization:

```ini
# Example: Constant fields should be UPPER_CASE
dotnet_naming_rule.const_should_be_upper_case.symbols = const_fields
dotnet_naming_rule.const_should_be_upper_case.style = upper_case_style
dotnet_naming_rule.const_should_be_upper_case.severity = warning

dotnet_naming_symbols.const_fields.applicable_kinds = field
dotnet_naming_symbols.const_fields.applicable_accessibilities = *
dotnet_naming_symbols.const_fields.required_modifiers = const

dotnet_naming_style.upper_case_style.capitalization = all_upper
```

## Severity Levels

Control how violations are reported:

| Level | Meaning |
|-------|---------|
| `silent` | No warnings |
| `suggestion` | Light bulb suggestion |
| `warning` | Yellow squiggly (default) |
| `error` | Red squiggly, build fails |

Example:
```ini
dotnet_naming_rule.private_members_camel_case.severity = warning
```

## File-Specific Rules

Apply rules to specific file patterns:

```ini
# All C# files
[*.cs]
indent_size = 4

# Test files only
[**/*Tests.cs]
csharp_style_var_elsewhere = true  # Looser rules for tests

# Generated files
[GeneratedCode/**]
generated_code = true
```

## Overriding the Defaults

1. Copy `.editorconfig` from `.github/skills/dotnet-format/assets/` to your project root
2. Modify rules as needed
3. Commit to version control so all team members use the same style
4. Reference the [official EditorConfig documentation](https://editorconfig.org)

## Validation

Check if your solution matches the current EditorConfig rules:

```powershell
/dotnet-format --verify-no-changes
```

If violations exist, apply formatting:

```powershell
/dotnet-format
```

## Further Reading

- [EditorConfig Official Docs](https://editorconfig.org)
- [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [dotnet format Documentation](https://github.com/dotnet/format)
