using System.CommandLine;
using AsanaCli.Services;

namespace AsanaCli.Commands;

public static class PermissionCommands
{
    public static Command Build(Option<string> formatOption)
    {
        var permCommand = new Command("permission", "Manage allowed projects");

        permCommand.Subcommands.Add(BuildList(formatOption));
        permCommand.Subcommands.Add(BuildAllow());
        permCommand.Subcommands.Add(BuildRemove());

        return permCommand;
    }

    private static Command BuildList(Option<string> formatOption)
    {
        var cmd = new Command("list", "List allowed projects");
        cmd.SetAction((parseResult, ct) =>
        {
            var format = parseResult.GetValue(formatOption) ?? "json";
            try
            {
                var result = PermissionService.List();
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

    private static Command BuildAllow()
    {
        var gidArg = new Argument<string>("project-gid") { Description = "Project GID" };
        var nameOption = new Option<string?>("--name") { Description = "Display name for the project" };
        var actionsOption = new Option<string[]>("--actions")
        {
            Description = "Allowed actions (read, write, delete)",
            AllowMultipleArgumentsPerToken = true,
            DefaultValueFactory = _ => new[] { "read", "write" }
        };
        var cmd = new Command("allow", "Add a project to the allowed list") { gidArg, nameOption, actionsOption };
        cmd.SetAction(async (parseResult, ct) =>
        {
            try
            {
                var gid = parseResult.GetValue(gidArg)!;
                var name = parseResult.GetValue(nameOption);
                var actions = parseResult.GetValue(actionsOption) ?? ["read", "write"];
                var result = await PermissionService.AllowAsync(gid, name, actions, ct);
                OutputService.Print(result);
            }
            catch (Exception ex)
            {
                OutputService.PrintError("error", ex.Message);
                Environment.ExitCode = 1;
            }
        });
        return cmd;
    }

    private static Command BuildRemove()
    {
        var gidArg = new Argument<string>("project-gid") { Description = "Project GID to remove" };
        var cmd = new Command("remove", "Remove a project from the allowed list") { gidArg };
        cmd.SetAction((parseResult, ct) =>
        {
            try
            {
                var gid = parseResult.GetValue(gidArg)!;
                var result = PermissionService.Remove(gid);
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
}
