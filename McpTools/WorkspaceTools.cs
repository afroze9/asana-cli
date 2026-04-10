using System.ComponentModel;
using AsanaCli.Services;
using ModelContextProtocol.Server;

namespace AsanaCli.McpTools;

[McpServerToolType]
public static class WorkspaceTools
{
    [McpServerTool(Name = "workspace_list"), Description("List available Asana workspaces and show which is active")]
    public static Task<string> List()
    {
        try
        {
            var result = WorkspaceService.List();
            return Task.FromResult(McpAsanaHelper.ToJson(result));
        }
        catch (Exception ex) { return Task.FromResult(McpAsanaHelper.HandleException(ex)); }
    }

    [McpServerTool(Name = "workspace_switch"), Description("Set the active Asana workspace")]
    public static Task<string> Switch(
        [Description("Workspace GID to set as active")] string gid)
    {
        try
        {
            var result = WorkspaceService.Switch(gid);
            return Task.FromResult(McpAsanaHelper.ToJson(result));
        }
        catch (Exception ex) { return Task.FromResult(McpAsanaHelper.HandleException(ex)); }
    }

    [McpServerTool(Name = "workspace_refresh"), Description("Refresh workspace list from Asana API")]
    public static async Task<string> Refresh()
    {
        try
        {
            var result = await WorkspaceService.RefreshAsync();
            return McpAsanaHelper.ToJson(result);
        }
        catch (AsanaApiException ex) { return McpAsanaHelper.HandleApiError(ex); }
        catch (Exception ex) { return McpAsanaHelper.HandleException(ex); }
    }
}
