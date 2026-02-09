using System.Text.Json.Serialization;

namespace VstsDemoBuilder.Blazor.Models;

internal sealed class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}
