using System.Text.Json.Serialization;

namespace VstsDemoBuilder.Blazor.Models;

internal sealed class AccountListResponse
{
    [JsonPropertyName("value")]
    public List<AccountItemResponse> Value { get; set; } = [];
}

internal sealed class AccountItemResponse
{
    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;
}
