using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Services;

public interface IAzureDevOpsAuthService
{
    string BuildAuthorizationUrl();

    Task<AuthenticatedSession?> CompleteSignInAsync(string code, CancellationToken cancellationToken = default);
}
