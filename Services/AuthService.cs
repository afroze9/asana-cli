using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AsanaCli.Services;

public class AuthService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".asana-cli");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public string GetAccessToken()
    {
        var config = LoadConfig();
        if (config == null || string.IsNullOrEmpty(config.PatToken))
        {
            Console.Error.WriteLine("Not authenticated. Run 'asana-cli auth login' first.");
            Environment.Exit(1);
        }
        return config!.PatToken!;
    }

    public string? GetActiveWorkspaceGid()
    {
        var config = LoadConfig();
        if (config == null || string.IsNullOrEmpty(config.ActiveWorkspace))
            return null;

        var ws = config.Workspaces?.FirstOrDefault(w => w.Gid == config.ActiveWorkspace);
        return ws?.Gid;
    }

    public void Login(string patToken)
    {
        var config = LoadConfig() ?? new AsanaCliConfig();
        config.PatToken = patToken;
        SaveConfig(config);
    }

    private static string ProtectToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.StartsWith("enc:"))
            return token;

        if (!OperatingSystem.IsWindows())
            return token;

        var bytes = Encoding.UTF8.GetBytes(token);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return "enc:" + Convert.ToBase64String(encrypted);
    }

    private static string UnprotectToken(string token)
    {
        if (string.IsNullOrEmpty(token) || !token.StartsWith("enc:"))
            return token;

        if (!OperatingSystem.IsWindows())
            return token;

        var encrypted = Convert.FromBase64String(token["enc:".Length..]);
        var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }

    public void SetWorkspaces(List<WorkspaceInfo> workspaces)
    {
        var config = LoadConfig() ?? new AsanaCliConfig();
        config.Workspaces = workspaces;

        // If no active workspace or active workspace no longer exists, set the first one
        if (string.IsNullOrEmpty(config.ActiveWorkspace) ||
            !workspaces.Any(w => w.Gid == config.ActiveWorkspace))
        {
            config.ActiveWorkspace = workspaces.FirstOrDefault()?.Gid;
        }

        SaveConfig(config);
    }

    public bool SwitchWorkspace(string gid)
    {
        var config = LoadConfig();
        if (config == null) return false;

        var ws = config.Workspaces?.FirstOrDefault(w => w.Gid == gid);
        if (ws == null) return false;

        config.ActiveWorkspace = gid;
        SaveConfig(config);
        return true;
    }

    public (List<WorkspaceInfo> Workspaces, string? ActiveGid) GetWorkspaces()
    {
        var config = LoadConfig();
        return (config?.Workspaces ?? [], config?.ActiveWorkspace);
    }

    public void Logout()
    {
        if (File.Exists(ConfigPath))
            File.Delete(ConfigPath);
    }

    public AuthStatus GetStatus()
    {
        var config = LoadConfig();
        if (config == null || string.IsNullOrEmpty(config.PatToken))
            return new AuthStatus { IsLoggedIn = false };

        var activeWs = config.Workspaces?.FirstOrDefault(w => w.Gid == config.ActiveWorkspace);
        return new AuthStatus
        {
            IsLoggedIn = true,
            TokenConfigured = true,
            ActiveWorkspace = activeWs?.Name,
            ActiveWorkspaceGid = activeWs?.Gid
        };
    }

    private static AsanaCliConfig? LoadConfig()
    {
        // Check environment variable first
        var envToken = Environment.GetEnvironmentVariable("ASANA_PAT");
        if (!string.IsNullOrEmpty(envToken))
        {
            var envWorkspace = Environment.GetEnvironmentVariable("ASANA_WORKSPACE");
            return new AsanaCliConfig { PatToken = envToken, ActiveWorkspace = envWorkspace };
        }

        if (!File.Exists(ConfigPath))
            return null;

        var json = File.ReadAllText(ConfigPath);
        var config = JsonSerializer.Deserialize<AsanaCliConfig>(json);
        if (config != null && !string.IsNullOrEmpty(config.PatToken))
            config.PatToken = UnprotectToken(config.PatToken);
        return config;
    }

    private static void SaveConfig(AsanaCliConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var toSave = new AsanaCliConfig
        {
            PatToken = ProtectToken(config.PatToken ?? ""),
            ActiveWorkspace = config.ActiveWorkspace,
            Workspaces = config.Workspaces
        };
        var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}

public class AsanaCliConfig
{
    public string? PatToken { get; set; }
    public string? ActiveWorkspace { get; set; }
    public List<WorkspaceInfo>? Workspaces { get; set; }
}

public class WorkspaceInfo
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
}

public class AuthStatus
{
    public bool IsLoggedIn { get; set; }
    public bool? TokenConfigured { get; set; }
    public string? ActiveWorkspace { get; set; }
    public string? ActiveWorkspaceGid { get; set; }
    public string? Message { get; set; }
}
