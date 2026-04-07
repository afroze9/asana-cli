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
                var list = AllowedProjectsService.Load();
                if (list.Projects.Count == 0)
                {
                    OutputService.Print(new { message = "No allowed projects configured. Projects will prompt on first use." });
                    return Task.CompletedTask;
                }

                var results = list.Projects.Select(p => new
                {
                    p.Gid,
                    p.DisplayName,
                    Actions = string.Join(", ", p.AllowedActions)
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

                // If no name provided, try to fetch it from API
                if (string.IsNullOrEmpty(name))
                {
                    try
                    {
                        var project = await AsanaClientProvider.GetAsync<AsanaProject>($"projects/{gid}?opt_fields=name", ct);
                        name = project?.Name ?? gid;
                    }
                    catch
                    {
                        name = gid;
                    }
                }

                var list = AllowedProjectsService.Load();
                var existing = list.Projects.FirstOrDefault(p =>
                    string.Equals(p.Gid, gid, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.DisplayName = name;
                    existing.AllowedActions = actions.ToList();
                }
                else
                {
                    list.Projects.Add(new AllowedProject
                    {
                        Gid = gid,
                        DisplayName = name,
                        AllowedActions = actions.ToList()
                    });
                }

                AllowedProjectsService.Save(list);
                OutputService.Print(new { status = "allowed", gid, name, actions });
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
                var list = AllowedProjectsService.Load();
                var removed = list.Projects.RemoveAll(p =>
                    string.Equals(p.Gid, gid, StringComparison.OrdinalIgnoreCase));

                if (removed == 0)
                {
                    OutputService.PrintError("not_found", $"Project '{gid}' not in allowed list.");
                    Environment.ExitCode = 1;
                    return Task.CompletedTask;
                }

                AllowedProjectsService.Save(list);
                OutputService.Print(new { status = "removed", gid });
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
