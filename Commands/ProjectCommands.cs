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
                if (string.IsNullOrEmpty(workspace))
                {
                    workspace = new AuthService().GetActiveWorkspaceGid();
                    if (string.IsNullOrEmpty(workspace))
                    {
                        OutputService.PrintError("no_workspace", "No active workspace. Run 'asana-cli workspace list' or 'asana-cli workspace switch <gid>'.");
                        Environment.ExitCode = 1;
                        return;
                    }
                }

                var endpoint = $"projects?workspace={Uri.EscapeDataString(workspace)}&opt_fields=gid,name,color,archived,created_at,modified_at&limit=100";

                var projects = await AsanaClientProvider.GetAsync<List<AsanaProject>>(endpoint, ct);
                var results = projects?.Select(p => new
                {
                    p.Gid,
                    p.Name,
                    p.Color,
                    p.Archived,
                    p.CreatedAt,
                    p.ModifiedAt
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

    private static Command BuildTasks(Option<string> formatOption)
    {
        var projectIdArg = new Argument<string>("project-id") { Description = "Project GID" };
        var cmd = new Command("tasks", "List tasks in a project") { projectIdArg };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var format = parseResult.GetValue(formatOption) ?? "json";
            var projectId = parseResult.GetValue(projectIdArg)!;

            if (!AllowedProjectsService.CheckAndPrompt(projectId, "read"))
            {
                Environment.ExitCode = 1;
                return;
            }

            try
            {
                var tasks = await AsanaClientProvider.GetAsync<List<AsanaTask>>(
                    $"projects/{Uri.EscapeDataString(projectId)}/tasks?opt_fields=gid,name,assignee.name,due_on,completed,completed_at,created_at,modified_at",
                    ct);
                var results = tasks?.Select(t => new
                {
                    t.Gid,
                    t.Name,
                    Assignee = t.Assignee?.Name,
                    t.DueOn,
                    t.Completed,
                    t.CreatedAt,
                    t.ModifiedAt
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
