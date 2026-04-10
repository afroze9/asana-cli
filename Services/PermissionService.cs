using AsanaCli.Commands;

namespace AsanaCli.Services;

public static class PermissionService
{
    public static object List()
    {
        var list = AllowedProjectsService.Load();
        if (list.Projects.Count == 0)
            return new { message = "No allowed projects configured. Projects will prompt on first use." };

        return list.Projects.Select(p => new
        {
            p.Gid,
            p.DisplayName,
            Actions = string.Join(", ", p.AllowedActions)
        }).ToList();
    }

    public static async Task<object> AllowAsync(string gid, string? name, string[] actions, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(name))
        {
            try
            {
                var project = await AsanaClientProvider.GetAsync<AsanaProject>($"projects/{gid}?opt_fields=name", ct);
                name = project?.Name ?? gid;
            }
            catch
            {
                name = gid;
            }
        }

        var list = AllowedProjectsService.Load();
        var existing = list.Projects.FirstOrDefault(p =>
            string.Equals(p.Gid, gid, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.DisplayName = name;
            existing.AllowedActions = actions.ToList();
        }
        else
        {
            list.Projects.Add(new AllowedProject
            {
                Gid = gid,
                DisplayName = name,
                AllowedActions = actions.ToList()
            });
        }

        AllowedProjectsService.Save(list);
        return new { status = "allowed", gid, name, actions };
    }

    public static object Remove(string gid)
    {
        var list = AllowedProjectsService.Load();
        var removed = list.Projects.RemoveAll(p =>
            string.Equals(p.Gid, gid, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
            throw new InvalidOperationException($"Project '{gid}' not in allowed list.");

        AllowedProjectsService.Save(list);
        return new { status = "removed", gid };
    }
}
