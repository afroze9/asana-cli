using System.CommandLine;
using AsanaCli.Services;

namespace AsanaCli.Commands;

public static class TaskCommands
{
    private static readonly string TaskOptFields = string.Join(",", [
        "gid", "name", "notes", "html_notes", "resource_type",
        "assignee.name", "assignee.email",
        "due_on", "due_at", "start_on", "start_at",
        "completed", "completed_at", "created_at", "modified_at",
        "parent.name", "parent.gid",
        "projects.name", "projects.gid",
        "tags.name",
        "custom_fields.name", "custom_fields.display_value", "custom_fields.type",
        "num_subtasks", "permalink_url"
    ]);

    public static Command Build(Option<string> formatOption)
    {
        var taskCommand = new Command("task", "Asana task operations");

        taskCommand.Subcommands.Add(BuildView(formatOption));

        return taskCommand;
    }

    private static Command BuildView(Option<string> formatOption)
    {
        var taskIdArg = new Argument<string>("task-gid") { Description = "Task GID" };
        var includeSubtasksOption = new Option<bool>("--subtasks") { Description = "Include subtasks", DefaultValueFactory = _ => true };
        var includeCommentsOption = new Option<bool>("--comments") { Description = "Include comments/stories", DefaultValueFactory = _ => true };
        var cmd = new Command("view", "View full task details") { taskIdArg, includeSubtasksOption, includeCommentsOption };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var format = parseResult.GetValue(formatOption) ?? "json";
            var taskId = parseResult.GetValue(taskIdArg)!;
            var includeSubtasks = parseResult.GetValue(includeSubtasksOption);
            var includeComments = parseResult.GetValue(includeCommentsOption);

            try
            {
                var task = await AsanaClientProvider.GetAsync<AsanaTaskDetail>(
                    $"tasks/{Uri.EscapeDataString(taskId)}?opt_fields={TaskOptFields}", ct);

                if (task == null)
                {
                    OutputService.PrintError("not_found", $"Task '{taskId}' not found.");
                    Environment.ExitCode = 1;
                    return;
                }

                List<AsanaSubtask>? subtasks = null;
                if (includeSubtasks && (task.NumSubtasks ?? 0) > 0)
                {
                    subtasks = await AsanaClientProvider.GetAsync<List<AsanaSubtask>>(
                        $"tasks/{Uri.EscapeDataString(taskId)}/subtasks?opt_fields=gid,name,completed,assignee.name,due_on", ct);
                }

                List<AsanaStory>? comments = null;
                if (includeComments)
                {
                    var stories = await AsanaClientProvider.GetAsync<List<AsanaStory>>(
                        $"tasks/{Uri.EscapeDataString(taskId)}/stories?opt_fields=gid,type,resource_subtype,text,created_by.name,created_at", ct);
                    comments = stories?.Where(s => s.ResourceSubtype == "comment_added").ToList();
                }

                var result = new
                {
                    task.Gid,
                    task.Name,
                    task.Notes,
                    Assignee = task.Assignee != null ? new { task.Assignee.Name, task.Assignee.Email } : null,
                    task.StartOn,
                    task.StartAt,
                    task.DueOn,
                    task.DueAt,
                    task.Completed,
                    task.CompletedAt,
                    task.CreatedAt,
                    task.ModifiedAt,
                    Parent = task.Parent != null ? new { task.Parent.Gid, task.Parent.Name } : null,
                    Projects = task.Projects?.Select(p => new { p.Gid, p.Name }).ToList(),
                    Tags = task.Tags?.Select(t => t.Name).ToList(),
                    CustomFields = task.CustomFields?
                        .Where(cf => cf.DisplayValue != null)
                        .Select(cf => new { cf.Name, cf.DisplayValue, cf.Type }).ToList(),
                    task.PermalinkUrl,
                    Subtasks = subtasks?.Select(s => new
                    {
                        s.Gid,
                        s.Name,
                        s.Completed,
                        Assignee = s.Assignee?.Name,
                        s.DueOn
                    }).ToList(),
                    Comments = comments?.Select(c => new
                    {
                        c.Gid,
                        Author = c.CreatedBy?.Name,
                        c.Text,
                        c.CreatedAt
                    }).ToList()
                };
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

public class AsanaTaskDetail
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public string? HtmlNotes { get; set; }
    public AsanaAssignee? Assignee { get; set; }
    public string? StartOn { get; set; }
    public string? StartAt { get; set; }
    public string? DueOn { get; set; }
    public string? DueAt { get; set; }
    public bool? Completed { get; set; }
    public string? CompletedAt { get; set; }
    public string? CreatedAt { get; set; }
    public string? ModifiedAt { get; set; }
    public AsanaNamedResource? Parent { get; set; }
    public List<AsanaNamedResource>? Projects { get; set; }
    public List<AsanaNamedResource>? Tags { get; set; }
    public List<AsanaCustomField>? CustomFields { get; set; }
    public int? NumSubtasks { get; set; }
    public string? PermalinkUrl { get; set; }
}

public class AsanaAssignee
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class AsanaCustomField
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
    public string? DisplayValue { get; set; }
    public string? Type { get; set; }
}

public class AsanaSubtask
{
    public string? Gid { get; set; }
    public string? Name { get; set; }
    public bool? Completed { get; set; }
    public AsanaNamedResource? Assignee { get; set; }
    public string? DueOn { get; set; }
}

public class AsanaStory
{
    public string? Gid { get; set; }
    public string? Type { get; set; }
    public string? ResourceSubtype { get; set; }
    public string? Text { get; set; }
    public AsanaNamedResource? CreatedBy { get; set; }
    public string? CreatedAt { get; set; }
}
