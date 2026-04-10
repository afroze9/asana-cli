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
                var result = WorkspaceService.List();
                OutputService.Print(result, format);
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
                var result = WorkspaceService.Switch(gid);
                OutputService.Print(result);
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
                var result = await WorkspaceService.RefreshAsync(ct);
                OutputService.Print(result, format);
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
