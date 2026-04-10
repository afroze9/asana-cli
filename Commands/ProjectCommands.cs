using System.CommandLine;
using AsanaCli.Services;

namespace AsanaCli.Commands;

public static class ProjectCommands
{
    public static Command Build(Option<string> formatOption)
    {
        var projectCommand = new Command("project", "Asana project operations");

        projectCommand.Subcommands.Add(BuildList(formatOption));
        projectCommand.Subcommands.Add(BuildTasks(formatOption));

        return projectCommand;
    }

    private static Command BuildList(Option<string> formatOption)
    {
        var workspaceOption = new Option<string?>("--workspace") { Description = "Workspace GID (uses default if not specified)" };
        var cmd = new Command("list", "List projects") { workspaceOption };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var format = parseResult.GetValue(formatOption) ?? "json";
            var workspace = parseResult.GetValue(workspaceOption);
            try
            {
                var result = await ProjectService.ListAsync(workspace, ct);
                OutputService.Print(result, format);
            }
            catch (AsanaApiException ex)
            {
                OutputService.PrintError("api_error", ex.Message);
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                OutputService.PrintError("error", ex.Message);
                Environment.ExitCode = 1;
            }
        });
        return cmd;
    }

    private static Command BuildTasks(Option<string> formatOption)
    {
        var projectIdArg = new Argument<string>("project-id") { Description = "Project GID" };
        var cmd = new Command("tasks", "List tasks in a project") { projectIdArg };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var format = parseResult.GetValue(formatOption) ?? "json";
            var projectId = parseResult.GetValue(projectIdArg)!;
            try
            {
                var result = await ProjectService.GetTasksAsync(projectId, ct);
                OutputService.Print(result, format);
            }
            catch (UnauthorizedAccessException ex)
            {
                OutputService.PrintError("not_allowed", ex.Message);
                Environment.ExitCode = 1;
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

public class AsanaProject
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public bool? Archived { get; set; }
    public string? CreatedAt { get; set; }
    public string? ModifiedAt { get; set; }
}

public class AsanaTask
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
    public AsanaNamedResource? Assignee { get; set; }
    public string? DueOn { get; set; }
    public bool? Completed { get; set; }
    public string? CompletedAt { get; set; }
    public string? CreatedAt { get; set; }
    public string? ModifiedAt { get; set; }
}

public class AsanaNamedResource
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
}
