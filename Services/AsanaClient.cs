using System.Net.Http.Headers;
using System.Text.Json;

namespace AsanaCli.Services;

public static class AsanaClientProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static HttpClient Create()
    {
        var auth = new AuthService();
        var token = auth.GetAccessToken();

        var client = new HttpClient
        {
            BaseAddress = new Uri("https://app.asana.com/api/1.0/")
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public static async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        using var client = Create();
        var response = await client.GetAsync(endpoint, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new AsanaApiException(response.StatusCode, body);

        var wrapper = JsonSerializer.Deserialize<AsanaResponse<T>>(body, JsonOptions);
        return wrapper != null ? wrapper.Data : default;
    }
}

public class AsanaResponse<T>
{
    public T? Data { get; set; }
}

public class AsanaApiException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }

    public AsanaApiException(System.Net.HttpStatusCode statusCode, string responseBody)
        : base(ParseErrorMessage(statusCode, responseBody))
    {
        StatusCode = statusCode;
    }

    private static string ParseErrorMessage(System.Net.HttpStatusCode statusCode, string responseBody)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(responseBody);
            if (doc.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
            {
                var msg = errors[0].TryGetProperty("message", out var m) ? m.GetString() : null;
                if (!string.IsNullOrEmpty(msg))
                    return $"Asana API error ({(int)statusCode}): {msg}";
            }
        }
        catch { }
        return $"Asana API error ({(int)statusCode})";
    }
}
