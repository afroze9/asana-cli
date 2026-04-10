using System.CommandLine;
using AsanaCli.Services;

namespace AsanaCli.Commands;

public static class TaskCommands
{
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
                var result = await TaskService.ViewAsync(taskId, includeSubtasks, includeComments, ct);
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
