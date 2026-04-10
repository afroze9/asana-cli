using System.CommandLine;
using AsanaCli;
using AsanaCli.Commands;

var rootCommand = new RootCommand("Asana CLI - manage projects, tasks, and more");
rootCommand.Options.Add(GlobalOptions.Format);

rootCommand.Subcommands.Add(AuthCommands.Build());
rootCommand.Subcommands.Add(WorkspaceCommands.Build(GlobalOptions.Format));
rootCommand.Subcommands.Add(ProjectCommands.Build(GlobalOptions.Format));
rootCommand.Subcommands.Add(TaskCommands.Build(GlobalOptions.Format));
rootCommand.Subcommands.Add(PermissionCommands.Build(GlobalOptions.Format));
rootCommand.Subcommands.Add(McpCommand.Build());

return await rootCommand.Parse(args).InvokeAsync();
