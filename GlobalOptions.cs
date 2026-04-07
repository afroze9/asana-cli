using System.CommandLine;

namespace AsanaCli;

public static class GlobalOptions
{
    public static readonly Option<string> Format = new("--format")
    {
        Description = "Output format: json or table",
        DefaultValueFactory = _ => "json",
        Recursive = true
    };
}
