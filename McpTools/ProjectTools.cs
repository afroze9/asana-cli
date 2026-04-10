using System.ComponentModel;
using AsanaCli.Services;
using ModelContextProtocol.Server;

namespace AsanaCli.McpTools;

[McpServerToolType]
public static class ProjectTools
{
    [McpServerTool(Name = "project_list"), Description("List Asana projects in the active workspace")]
    public static async Task<string> List(
        [Description("Workspace GID (uses active workspace if not specified)")] string? workspace = null)
    {
        try
        {
            var result = await ProjectService.ListAsync(workspace);
            return McpAsanaHelper.ToJson(result);
        }
        catch (AsanaApiException ex) { return McpAsanaHelper.HandleApiError(ex); }
        catch (Exception ex) { return McpAsanaHelper.HandleException(ex); }
    }

    [McpServerTool(Name = "project_tasks"), Description("List tasks in an Asana project")]
    public static async Task<string> Tasks(
        [Description("Project GID")] string projectId)
    {
        try
        {
            var result = await ProjectService.GetTasksAsync(projectId);
            return McpAsanaHelper.ToJson(result);
        }
        catch (AsanaApiException ex) { return McpAsanaHelper.HandleApiError(ex); }
        catch (Exception ex) { return McpAsanaHelper.HandleException(ex); }
    }
}
