using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace VstsDemoBuilder.Blazor.Session;

public static class SessionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void SetStringList(this ISession session, string key, IReadOnlyList<string> values)
    {
        session.SetString(key, JsonSerializer.Serialize(values, JsonOptions));
    }

    public static IReadOnlyList<string> GetStringList(this ISession session, string key)
    {
        var payload = session.GetString(key);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(payload, JsonOptions) ?? [];
    }

    public static string? GetEmail(this ISession session)
    {
        return session.GetString(SessionKeys.Email);
    }

    public static string? GetAccessToken(this ISession session)
    {
        return session.GetString(SessionKeys.AccessToken);
    }

    public static string? GetUserId(this ISession session)
    {
        return session.GetString(SessionKeys.DisplayName);
    }
}
