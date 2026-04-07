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
