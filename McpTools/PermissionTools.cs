using System.ComponentModel;
using AsanaCli.Services;
using ModelContextProtocol.Server;

namespace AsanaCli.McpTools;

[McpServerToolType]
public static class PermissionTools
{
    [McpServerTool(Name = "permission_list"), Description("List allowed projects and their permitted actions")]
    public static Task<string> List()
    {
        try
        {
            var result = PermissionService.List();
            return Task.FromResult(McpAsanaHelper.ToJson(result));
        }
        catch (Exception ex) { return Task.FromResult(McpAsanaHelper.HandleException(ex)); }
    }

    [McpServerTool(Name = "permission_allow"), Description("Add or update an allowed project. Controls which projects can be accessed.")]
    public static async Task<string> Allow(
        [Description("Project GID")] string gid,
        [Description("Comma-separated allowed actions: read, write, delete")] string actions = "read,write",
        [Description("Display name for the project")] string? name = null)
    {
        try
        {
            var actionArray = actions.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToArray();
            var result = await PermissionService.AllowAsync(gid, name, actionArray);
            return McpAsanaHelper.ToJson(result);
        }
        catch (Exception ex) { return McpAsanaHelper.HandleException(ex); }
    }

    [McpServerTool(Name = "permission_remove"), Description("Remove a project from the allowed list")]
    public static Task<string> Remove(
        [Description("Project GID to remove")] string gid)
    {
        try
        {
            var result = PermissionService.Remove(gid);
            return Task.FromResult(McpAsanaHelper.ToJson(result));
        }
        catch (Exception ex) { return Task.FromResult(McpAsanaHelper.HandleException(ex)); }
    }
}
