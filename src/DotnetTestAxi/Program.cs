using System.Reflection;
using System.Text.RegularExpressions;

namespace DotnetTestAxi;

internal static partial class Program
{
    [GeneratedRegex("\u001b\\[[0-9;?]*[ -/]*[@-~]")]
    private static partial Regex Ansi();

    private static async Task<int> Main(string[] argv)
    {
        var args = Args.Parse(argv);
        var cwd = Directory.GetCurrentDirectory();
        var config = Config.Find(cwd);

        if (args.Error is not null)
        {
            Console.WriteLine($"error: {args.Error}");
            Console.WriteLine("next: dotnet-test-axi --help");
            return 2;
        }

        if (args.Version)
        {
            Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0");
            return 0;
        }

        if (args.Help)
        {
            Console.Write(Help.Text);
            return 0;
        }

        if (args.FromLog is not null)
        {
            return FromLog(args);
        }

        if (args.Target is null)
        {
            Console.Write(Help.Dashboard(config));
            return 0;
        }

        var target = Config.ResolveTarget(config, args.Target, cwd);
        if (target is null)
        {
            Console.WriteLine($"error: target '{args.Target}' is not a known name or an existing path");
            Console.WriteLine(Help.Targets(config));
            return 2;
        }

        var logPath = TestRunner.NewLogPath(target, config?.LogDir is { } d && config.Directory is { } cd ? Path.GetFullPath(d, cd) : null);
        var exit = await TestRunner.RunAsync(TestRunner.BuildArguments(target, args), cwd, logPath);

        var result = MtpParser.Parse(Ansi().Replace(File.ReadAllText(logPath), ""));
        result.ExitCode ??= exit != 0 ? exit : null;
        if (exit != 0 && result.Status == RunStatus.Pass)
        {
            result.Status = RunStatus.NoSummary; // dotnet failed even though a summary said pass: do not claim success
        }

        Console.Write(Toon.Format(result, logPath, args.Target, args.Full));
        return result.Status == RunStatus.Pass ? 0 : exit != 0 ? exit : 1;
    }

    private static int FromLog(Args args)
    {
        if (!File.Exists(args.FromLog))
        {
            Console.WriteLine($"error: log file '{args.FromLog}' not found");
            return 2;
        }

        var result = MtpParser.Parse(Ansi().Replace(File.ReadAllText(args.FromLog!), ""));
        Console.Write(Toon.Format(result, Path.GetFullPath(args.FromLog!), args.Target, args.Full));
        return result.Status == RunStatus.Pass ? 0 : 1;
    }
}
