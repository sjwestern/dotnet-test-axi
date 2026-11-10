using System.Text;

namespace DotnetTestAxi;

internal static class Help
{
    public const string Text = """
        usage: dotnet-test-axi <target> [--class <name>] [--method <name>] [--namespace <name>] [--no-build] [--full] [-- <extra dotnet test args>]
               dotnet-test-axi --from-log <file> [--full]
        target: a name from .dotnet-test-axi.json, or a path to a project, solution or directory
        runs: dotnet test (Microsoft Testing Platform) and prints a compact summary; the full output is always saved to a log file
        output: status (pass|fail|build-error|no-tests|no-summary), counts, failures (name, message, at), log, next
        exit: 0 on pass, otherwise non-zero
        examples:
          dotnet-test-axi tests/MyApp.Tests
          dotnet-test-axi unit --class "MyApp.Orders.OrderTests"
          dotnet-test-axi --from-log results.log
        config: .dotnet-test-axi.json (found by walking up from the working directory)
          { "targets": { "unit": "tests/MyApp.Tests", "all": "MyApp.slnx" }, "logDir": ".test-logs" }

        """;

    public static string Dashboard(Config? config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("dotnet-test-axi: run dotnet test and print a compact summary");
        sb.AppendLine(config is null ? $"config: none (add {Config.FileName} to name targets)" : $"config: {config.Path}");
        sb.Append(Targets(config));
        sb.AppendLine("next: dotnet-test-axi <target>   (dotnet-test-axi --help for all options)");
        return sb.ToString();
    }

    public static string Targets(Config? config)
    {
        if (config is null || config.Targets.Count == 0)
        {
            return "targets: none defined; pass a project, solution or directory path\n";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"targets[{config.Targets.Count}]{{name,path}}:");
        foreach (var (name, path) in config.Targets)
        {
            sb.AppendLine($"  {name},{path}");
        }

        return sb.ToString();
    }
}
