using System.ComponentModel;
using AsanaCli.Services;
using ModelContextProtocol.Server;

namespace AsanaCli.McpTools;

[McpServerToolType]
public static class TaskTools
{
    [McpServerTool(Name = "task_view"), Description("View full details of an Asana task including subtasks and comments")]
    public static async Task<string> View(
        [Description("Task GID")] string taskId,
        [Description("Include subtasks (default: true)")] bool includeSubtasks = true,
        [Description("Include comments (default: true)")] bool includeComments = true)
    {
        try
        {
            var result = await TaskService.ViewAsync(taskId, includeSubtasks, includeComments);
            return McpAsanaHelper.ToJson(result);
        }
        catch (AsanaApiException ex) { return McpAsanaHelper.HandleApiError(ex); }
        catch (Exception ex) { return McpAsanaHelper.HandleException(ex); }
    }
}
