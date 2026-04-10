# asana-cli

A .NET global tool for interacting with Asana — manage workspaces, projects, and tasks from the command line. Authenticates using Personal Access Tokens (PAT). Output is JSON by default (`--format table` for human-readable output).

## Installation

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download) or later.

```bash
# Clone and install
git clone https://github.com/afroze9/asana-cli.git
cd asana-cli
dotnet pack -c Release
dotnet tool install -g asana-cli --add-source ./bin/Release
```

## Authentication

Uses Asana Personal Access Tokens stored at `~/.asana-cli/config.json`. On login, all accessible workspaces are fetched and the first is set as active.

```bash
asana-cli auth login --token <pat>   # Store PAT (validates against /users/me)
asana-cli auth login                 # Interactive prompt for token
asana-cli auth status                # Check auth status and active workspace
asana-cli auth logout                # Remove stored credentials
```

You can also set the `ASANA_PAT` environment variable instead of using `auth login`.

## Commands

### Workspaces

The active workspace is used as the default for commands like `project list`. Workspace config is stored alongside the PAT in `~/.asana-cli/config.json`.

```bash
asana-cli workspace list              # List workspaces (shows which is active)
asana-cli workspace switch <gid>      # Set active workspace
asana-cli workspace refresh           # Re-fetch workspaces from Asana API
```

The `ASANA_WORKSPACE` environment variable can override the active workspace.

### Projects

```bash
asana-cli project list [--workspace <gid>]   # List projects (defaults to active workspace)
asana-cli project tasks <project-gid>        # List tasks in a project
```

### Permissions

Commands that target a specific project are gated by an allowed-projects list stored at `~/.asana-cli/allowed-projects.json`. When run interactively, unapproved projects prompt for consent. When run non-interactively (piped stdin), unapproved projects are denied.

```bash
asana-cli permission allow <project-gid> [--name "Name"] [--actions read write delete]
asana-cli permission list
asana-cli permission remove <project-gid>
```

**Action mapping:**

| Action | Operations |
|--------|-----------|
| `read` | List tasks, view project details |
| `write` | Create/update tasks (future) |
| `delete` | Delete tasks (future) |

## MCP Server

asana-cli includes a built-in [Model Context Protocol](https://modelcontextprotocol.io/) server, allowing AI assistants (Claude Desktop, Cursor, etc.) to interact with Asana directly.

```bash
# Start the MCP server over stdio
asana-cli mcp
```

### Claude Desktop Configuration

Add to your Claude Desktop config (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "asana-cli": {
      "command": "asana-cli",
      "args": ["mcp"],
      "env": {
        "ASANA_PAT": "<your-pat>",
        "ASANA_WORKSPACE": "<workspace-gid>",
        "ASANA_CLI_SKIP_ALLOWLIST": "true"
      }
    }
  }
}
```

### Available MCP Tools

| Tool | Description |
|------|-------------|
| `workspace_list` | List workspaces and show active |
| `workspace_switch` | Set active workspace |
| `workspace_refresh` | Refresh workspace list from API |
| `project_list` | List projects in active workspace |
| `project_tasks` | List tasks in a project |
| `task_view` | View full task details (subtasks, comments) |
| `permission_list` | List allowed projects |
| `permission_allow` | Add/update an allowed project |
| `permission_remove` | Remove a project from allowed list |

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ASANA_PAT` | Personal Access Token (alternative to `auth login`) |
| `ASANA_WORKSPACE` | Override active workspace GID |
| `ASANA_CLI_SKIP_ALLOWLIST` | Set to `true` to bypass project permission checks |

## Global Options

| Option | Description |
|---|---|
| `--format json\|table` | Output format (default: `json`) |

## Configuration Files

| File | Purpose |
|------|---------|
| `~/.asana-cli/config.json` | PAT token, workspace list, active workspace |
| `~/.asana-cli/allowed-projects.json` | Allowed projects for permission gating |

## License

MIT
