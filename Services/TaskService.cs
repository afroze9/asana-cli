using AsanaCli.Commands;

namespace AsanaCli.Services;

public static class TaskService
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

    public static async Task<object> ViewAsync(string taskId, bool includeSubtasks = true, bool includeComments = true, CancellationToken ct = default)
    {
        var task = await AsanaClientProvider.GetAsync<AsanaTaskDetail>(
            $"tasks/{Uri.EscapeDataString(taskId)}?opt_fields={TaskOptFields}", ct);

        if (task == null)
            throw new InvalidOperationException($"Task '{taskId}' not found.");

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

        return new
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
    }
}
