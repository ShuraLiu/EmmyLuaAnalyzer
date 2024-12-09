using CommandLine;

namespace EmmyLua.Cli.Linter;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckOptions
{
    [Option('w', "workspace", Required = true, HelpText = "Workspace directory")]
    public string Workspace { get; set; } = string.Empty;
    
    [Option('f', "files", Required = false, HelpText = "Check specified files if declared")]
    public IEnumerable<string> files { get; set; }

    [Option('c', "config", Required = false, HelpText = "Use custom .emmyrc.json file")]
    public String? Config { get; set; }
}