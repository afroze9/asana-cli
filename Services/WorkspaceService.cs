namespace AsanaCli.Services;

public static class WorkspaceService
{
    public static object List()
    {
        var auth = new AuthService();
        var (workspaces, activeGid) = auth.GetWorkspaces();

        if (workspaces.Count == 0)
            throw new InvalidOperationException("No workspaces stored. Run 'asana-cli auth login' first.");

        return workspaces.Select(w => new
        {
            w.Gid,
            w.Name,
            Active = w.Gid == activeGid
        }).ToList();
    }

    public static object Switch(string gid)
    {
        var auth = new AuthService();

        if (!auth.SwitchWorkspace(gid))
            throw new InvalidOperationException($"Workspace '{gid}' not found. Run 'asana-cli workspace list' to see available workspaces.");

        var (workspaces, _) = auth.GetWorkspaces();
        var ws = workspaces.FirstOrDefault(w => w.Gid == gid);
        return new { status = "switched", gid = ws?.Gid, name = ws?.Name };
    }

    public static async Task<object> RefreshAsync(CancellationToken ct = default)
    {
        var auth = new AuthService();
        var workspaces = await AsanaClientProvider.GetAsync<List<WorkspaceInfo>>("workspaces?opt_fields=gid,name", ct);

        if (workspaces == null || workspaces.Count == 0)
            throw new InvalidOperationException("No workspaces found in Asana.");

        auth.SetWorkspaces(workspaces);
        var (_, activeGid) = auth.GetWorkspaces();

        return workspaces.Select(w => new
        {
            w.Gid,
            w.Name,
            Active = w.Gid == activeGid
        }).ToList();
    }
}
