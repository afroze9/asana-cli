using System.Text.Json;

namespace AsanaCli.Services;

public static class AllowedProjectsService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".asana-cli");
    private static readonly string FilePath = Path.Combine(ConfigDir, "allowed-projects.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static bool IsBypassed =>
        string.Equals(Environment.GetEnvironmentVariable("ASANA_CLI_SKIP_ALLOWLIST"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the given project is allowed for the requested action.
    /// If not, prompts the user interactively to allow once, allow and save, or deny.
    /// </summary>
    public static bool CheckAndPrompt(string projectGid, string action, string? displayName = null)
    {
        if (IsBypassed) return true;

        var list = Load();
        var entry = list.Projects.FirstOrDefault(p =>
            string.Equals(p.Gid, projectGid, StringComparison.OrdinalIgnoreCase));

        if (entry != null && entry.AllowedActions.Contains(action, StringComparer.OrdinalIgnoreCase))
            return true;

        // Non-interactive: deny
        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine($"Project '{displayName ?? projectGid}' is not in the allowed list for '{action}'. " +
                                    "Add it with 'asana-cli permission allow'.");
            return false;
        }

        // Interactive prompt
        var label = displayName != null ? $"{displayName} ({projectGid})" : projectGid;
        Console.Write($"Project '{label}' is not allowed for '{action}'. Allow? [y]es once / [a]llow and save / [N]o: ");
        var input = Console.ReadLine()?.Trim().ToLower();

        switch (input)
        {
            case "y":
                return true;

            case "a":
                if (entry == null)
                {
                    entry = new AllowedProject { Gid = projectGid, DisplayName = displayName ?? projectGid, AllowedActions = [action] };
                    list.Projects.Add(entry);
                }
                else if (!entry.AllowedActions.Contains(action, StringComparer.OrdinalIgnoreCase))
                {
                    entry.AllowedActions.Add(action);
                }
                Save(list);
                return true;

            default:
                Console.Error.WriteLine("Denied.");
                return false;
        }
    }

    public static AllowedProjectsList Load()
    {
        if (!File.Exists(FilePath))
            return new AllowedProjectsList();

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<AllowedProjectsList>(json, JsonOptions) ?? new AllowedProjectsList();
    }

    public static void Save(AllowedProjectsList list)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(list, JsonOptions));
    }
}

public class AllowedProjectsList
{
    public List<AllowedProject> Projects { get; set; } = [];
}

public class AllowedProject
{
    public string Gid { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<string> AllowedActions { get; set; } = [];
}
