namespace VstsDemoBuilder.Blazor.Models;

public sealed record AuthenticatedSession(
    string AccessToken,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Organizations);
