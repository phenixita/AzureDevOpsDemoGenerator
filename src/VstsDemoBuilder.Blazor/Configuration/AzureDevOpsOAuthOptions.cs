namespace VstsDemoBuilder.Blazor.Configuration;

public sealed class AzureDevOpsOAuthOptions
{
    public const string SectionName = "AzureDevOpsOAuth";

    public string AuthorityUri { get; set; } = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

    public string TokenEndpoint { get; set; } = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string Scope { get; set; } = "vso.profile vso.project";

    public string BaseAddress { get; set; } = "https://app.vssps.visualstudio.com";
}
