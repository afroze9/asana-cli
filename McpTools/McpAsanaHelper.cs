using System.Text.Json;
using System.Text.Json.Serialization;
using AsanaCli.Services;

namespace AsanaCli.McpTools;

public static class McpAsanaHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ToJson(object? data) =>
        JsonSerializer.Serialize(data, JsonOptions);

    public static string Error(string code, string message)
    {
        Console.Error.WriteLine($"[asana-cli] {code}: {message}");
        return ToJson(new { error = code, message });
    }

    public static string HandleApiError(AsanaApiException ex)
    {
        Console.Error.WriteLine($"[asana-cli] ApiError: {ex.StatusCode} - {ex.Message}");
        return Error("api_error", ex.Message);
    }

    public static string HandleException(Exception ex)
    {
        Console.Error.WriteLine($"[asana-cli] {ex.GetType().Name}: {ex.Message}");
        return Error(ex.GetType().Name, ex.Message);
    }
}
