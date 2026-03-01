using AzdoGenCli.Models;

namespace AzdoGenCli
{
    /// <summary>
    /// Container for all collected input required for project provisioning
    /// Populated by InputCollector after authentication and user prompts
    /// </summary>
    public class CliInput
    {
        /// <summary>Target Azure DevOps project name to create</summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>Azure DevOps organization name (e.g., 'myorg' from dev.azure.com/myorg)</summary>
        public string OrganizationName { get; set; } = string.Empty;

        /// <summary>Personal Access Token or OAuth access token for API calls</summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>OAuth refresh token (nullable, only for OAuth flow)</summary>
        public string? RefreshToken { get; set; }

        /// <summary>Template key or short name selected by user</summary>
        public string SelectedTemplate { get; set; } = string.Empty;

        /// <summary>Full template metadata (nullable, resolved from SelectedTemplate)</summary>
        public TemplateInfo? TemplateInfo { get; set; }

        /// <summary>True if using OAuth authentication, false if using PAT</summary>
        public bool IsOAuth { get; set; }

        /// <summary>Azure DevOps account name (nullable, for compatibility with web app code)</summary>
        public string? AccountName { get; set; }
    }
}
