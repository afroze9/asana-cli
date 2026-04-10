using AsanaCli.Commands;

namespace AsanaCli.Services;

public static class ProjectService
{
    public static async Task<object> ListAsync(string? workspace = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(workspace))
        {
            workspace = new AuthService().GetActiveWorkspaceGid();
            if (string.IsNullOrEmpty(workspace))
                throw new InvalidOperationException("No active workspace. Run 'asana-cli workspace list' or 'asana-cli workspace switch <gid>'.");
        }

        var endpoint = $"projects?workspace={Uri.EscapeDataString(workspace)}&opt_fields=gid,name,color,archived,created_at,modified_at&limit=100";
        var projects = await AsanaClientProvider.GetAsync<List<AsanaProject>>(endpoint, ct);

        return projects?.Select(p => new
        {
            p.Gid,
            p.Name,
            p.Color,
            p.Archived,
            p.CreatedAt,
            p.ModifiedAt
        }).ToList() ?? [];
    }

    public static async Task<object> GetTasksAsync(string projectId, CancellationToken ct = default)
    {
        if (!AllowedProjectsService.CheckAndPrompt(projectId, "read"))
            throw new UnauthorizedAccessException($"Project '{projectId}' is not allowed for 'read'. Use 'asana-cli permission allow' to add it.");

        var tasks = await AsanaClientProvider.GetAsync<List<AsanaTask>>(
            $"projects/{Uri.EscapeDataString(projectId)}/tasks?opt_fields=gid,name,assignee.name,due_on,completed,completed_at,created_at,modified_at",
            ct);

        return tasks?.Select(t => new
        {
            t.Gid,
            t.Name,
            Assignee = t.Assignee?.Name,
            t.DueOn,
            t.Completed,
            t.CreatedAt,
            t.ModifiedAt
        }).ToList() ?? [];
    }
}
