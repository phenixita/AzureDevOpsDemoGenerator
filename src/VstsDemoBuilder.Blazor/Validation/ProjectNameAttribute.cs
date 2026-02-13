using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace VstsDemoBuilder.Blazor.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ProjectNameAttribute : ValidationAttribute
{
    private const int MaxLength = 64;
    private const string ProjectNamePattern = @"^(?!_)(?![.])[a-zA-Z0-9!^\-`)(]*[a-zA-Z0-9_!^\.)( ]*[^.\/\\~@#$*%+=[\]{\}'"",:;?<>|](?:[a-zA-Z!)(][a-zA-Z0-9!^\-` )(]+)?$";
    private static readonly Regex ProjectNameRegex = new(ProjectNamePattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Success;
        }

        string trimmedName = name.Trim();

        if (trimmedName.Length > MaxLength)
        {
            return new ValidationResult("Project name cannot exceed 64 characters");
        }

        if (!ProjectNameRegex.IsMatch(trimmedName))
        {
            return new ValidationResult("Project name contains invalid characters");
        }

        if (ReservedProjectNames.IsReserved(trimmedName))
        {
            return new ValidationResult($"'{trimmedName}' is a reserved name and cannot be used");
        }

        return ValidationResult.Success;
    }
}
