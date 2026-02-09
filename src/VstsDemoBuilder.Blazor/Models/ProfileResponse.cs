using System.Text.Json.Serialization;

namespace VstsDemoBuilder.Blazor.Models;

internal sealed class ProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;
}
