using System.CommandLine;
using AsanaCli.Services;

namespace AsanaCli.Commands;

public static class AuthCommands
{
    public static Command Build()
    {
        var authCommand = new Command("auth", "Authentication management");

        authCommand.Subcommands.Add(BuildLogin());
        authCommand.Subcommands.Add(BuildStatus());
        authCommand.Subcommands.Add(BuildLogout());

        return authCommand;
    }

    private static Command BuildLogin()
    {
        var tokenOption = new Option<string?>("--token") { Description = "Asana Personal Access Token" };
        var cmd = new Command("login", "Store an Asana PAT for authentication") { tokenOption };
        cmd.SetAction(async (parseResult, ct) =>
        {
            try
            {
                var token = parseResult.GetValue(tokenOption);
                if (string.IsNullOrEmpty(token))
                {
                    Console.Write("Enter your Asana Personal Access Token: ");
                    token = Console.ReadLine()?.Trim();
                }

                if (string.IsNullOrEmpty(token))
                {
                    OutputService.PrintError("auth_failed", "No token provided.");
                    Environment.ExitCode = 1;
                    return;
                }

                // Save token first so API calls work
                var auth = new AuthService();
                auth.Login(token);

                // Validate token
                var me = await AsanaClientProvider.GetAsync<AsanaUser>("users/me?opt_fields=name,email", ct);

                // Fetch and store workspaces
                var workspaces = await AsanaClientProvider.GetAsync<List<WorkspaceInfo>>("workspaces?opt_fields=gid,name", ct);
                if (workspaces != null && workspaces.Count > 0)
                    auth.SetWorkspaces(workspaces);

                var (_, activeGid) = auth.GetWorkspaces();
                var activeWs = workspaces?.FirstOrDefault(w => w.Gid == activeGid);

                OutputService.Print(new
                {
                    status = "success",
                    name = me?.Name,
                    email = me?.Email,
                    activeWorkspace = activeWs?.Name,
                    activeWorkspaceGid = activeWs?.Gid,
                    workspaceCount = workspaces?.Count ?? 0
                });
            }
            catch (AsanaApiException ex)
            {
                new AuthService().Logout();
                OutputService.PrintError("auth_failed", ex.Message);
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                new AuthService().Logout();
                OutputService.PrintError("auth_failed", ex.Message);
                Environment.ExitCode = 1;
            }
        });
        return cmd;
    }

    private static Command BuildStatus()
    {
        var cmd = new Command("status", "Show current authentication status");
        cmd.SetAction(async (parseResult, ct) =>
        {
            try
            {
                var auth = new AuthService();
                var status = auth.GetStatus();

                if (!status.IsLoggedIn)
                {
                    OutputService.Print(status);
                    return;
                }

                // Verify token still works
                var me = await AsanaClientProvider.GetAsync<AsanaUser>("users/me?opt_fields=name,email", ct);
                OutputService.Print(new
                {
                    isLoggedIn = true,
                    name = me?.Name,
                    email = me?.Email,
                    tokenConfigured = status.TokenConfigured,
                    activeWorkspace = status.ActiveWorkspace,
                    activeWorkspaceGid = status.ActiveWorkspaceGid
                });
            }
            catch (Exception ex)
            {
                OutputService.PrintError("status_failed", ex.Message);
                Environment.ExitCode = 1;
            }
        });
        return cmd;
    }

    private static Command BuildLogout()
    {
        var cmd = new Command("logout", "Remove stored credentials");
        cmd.SetAction((parseResult, ct) =>
        {
            try
            {
                var auth = new AuthService();
                auth.Logout();
                OutputService.Print(new { status = "logged_out" });
            }
            catch (Exception ex)
            {
                OutputService.PrintError("logout_failed", ex.Message);
                Environment.ExitCode = 1;
            }
            return Task.CompletedTask;
        });
        return cmd;
    }
}

public class AsanaUser
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}
