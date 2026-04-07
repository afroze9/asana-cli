using System.CommandLine;
using AsanaCli.Services;

namespace AsanaCli.Commands;

public static class WorkspaceCommands
{
    public static Command Build(Option<string> formatOption)
    {
        var wsCommand = new Command("workspace", "Workspace management");

        wsCommand.Subcommands.Add(BuildList(formatOption));
        wsCommand.Subcommands.Add(BuildSwitch());
        wsCommand.Subcommands.Add(BuildRefresh(formatOption));

        return wsCommand;
    }

    private static Command BuildList(Option<string> formatOption)
    {
        var cmd = new Command("list", "List available workspaces and show active");
        cmd.SetAction((parseResult, ct) =>
        {
            var format = parseResult.GetValue(formatOption) ?? "json";
            try
            {
                var auth = new AuthService();
                var (workspaces, activeGid) = auth.GetWorkspaces();

                if (workspaces.Count == 0)
                {
                    OutputService.PrintError("no_workspaces", "No workspaces stored. Run 'asana-cli auth login' first.");
                    Environment.ExitCode = 1;
                    return Task.CompletedTask;
                }

                var results = workspaces.Select(w => new
                {
                    w.Gid,
                    w.Name,
                    Active = w.Gid == activeGid
                }).ToList();
                OutputService.Print(results, format);
            }
            catch (Exception ex)
            {
                OutputService.PrintError("error", ex.Message);
                Environment.ExitCode = 1;
            }
            return Task.CompletedTask;
        });
        return cmd;
    }

    private static Command BuildSwitch()
    {
        var gidArg = new Argument<string>("workspace-gid") { Description = "Workspace GID to set as active" };
        var cmd = new Command("switch", "Set the active workspace") { gidArg };
        cmd.SetAction((parseResult, ct) =>
        {
            try
            {
                var gid = parseResult.GetValue(gidArg)!;
                var auth = new AuthService();

                if (!auth.SwitchWorkspace(gid))
                {
                    OutputService.PrintError("not_found", $"Workspace '{gid}' not found. Run 'asana-cli workspace list' to see available workspaces.");
                    Environment.ExitCode = 1;
                    return Task.CompletedTask;
                }

                var (workspaces, _) = auth.GetWorkspaces();
                var ws = workspaces.FirstOrDefault(w => w.Gid == gid);
                OutputService.Print(new
                {
                    status = "switched",
                    gid = ws?.Gid,
                    name = ws?.Name
                });
            }
            catch (Exception ex)
            {
                OutputService.PrintError("error", ex.Message);
                Environment.ExitCode = 1;
            }
            return Task.CompletedTask;
        });
        return cmd;
    }

    private static Command BuildRefresh(Option<string> formatOption)
    {
        var cmd = new Command("refresh", "Refresh workspace list from Asana API");
        cmd.SetAction(async (parseResult, ct) =>
        {
            var format = parseResult.GetValue(formatOption) ?? "json";
            try
            {
                var auth = new AuthService();
                var workspaces = await AsanaClientProvider.GetAsync<List<WorkspaceInfo>>("workspaces?opt_fields=gid,name", ct);

                if (workspaces == null || workspaces.Count == 0)
                {
                    OutputService.PrintError("no_workspaces", "No workspaces found in Asana.");
                    Environment.ExitCode = 1;
                    return;
                }

                auth.SetWorkspaces(workspaces);
                var (_, activeGid) = auth.GetWorkspaces();

                var results = workspaces.Select(w => new
                {
                    w.Gid,
                    w.Name,
                    Active = w.Gid == activeGid
                }).ToList();
                OutputService.Print(results, format);
            }
            catch (AsanaApiException ex)
            {
                OutputService.PrintError("api_error", ex.Message);
                Environment.ExitCode = 1;
            }
        });
        return cmd;
    }
}
