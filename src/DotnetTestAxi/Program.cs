using System.Reflection;
using DotnetTestAxi.Frameworks;
using System.Text.RegularExpressions;

namespace DotnetTestAxi;

internal static partial class Program
{
    [GeneratedRegex("\u001b\\[[0-9;?]*[ -/]*[@-~]")]
    private static partial Regex Ansi();

    private static async Task<int> Main(string[] argv)
    {
        // Fast path: answer the version before touching config or the disk.
        if (argv is [("-v" or "-V" or "--version")])
        {
            Console.WriteLine(Version());
            return 0;
        }

        try
        {
            return await RunAsync(argv);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Write(Output.Error($"unexpected failure: {ex.Message}", Output.RuntimeError,
                "Run `dotnet-test-axi --help` to check the usage"));
            return 1;
        }
    }

    private static async Task<int> RunAsync(string[] argv)
    {
        var args = Args.Parse(argv);

        if (args.Error is not null)
        {
            Console.Write(Output.Error(args.Error, Output.ValidationError,
                args.ErrorHelp ?? $"valid flags: {Args.ValidFlags}", "Run `dotnet-test-axi --help` for usage"));
            return 2;
        }

        if (args.Version)
        {
            Console.WriteLine(Version());
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

        var cwd = Directory.GetCurrentDirectory();
        var config = Config.Find(cwd);

        if (args.Target is null)
        {
            Console.Write(Output.Home(config, Environment.ProcessPath ?? "dotnet-test-axi"));
            return 0;
        }

        var target = Config.ResolveTarget(config, args.Target, cwd);
        if (target is null)
        {
            Console.Write(Output.UnknownTarget(args.Target, config));
            return 2;
        }

        RunPlan plan = new(Strategy.Unfiltered, null, []);
        if (!args.Filter.IsEmpty)
        {
            var detections = await FrameworkDetector.DetectAsync(TestRunner.ProjectsOf(target), config);
            plan = RunPlan.Choose(false, detections);
            if (plan.Strategy is Strategy.Unknown or Strategy.Mixed)
            {
                Console.Write(Output.Run(PlanFailure(plan), null, args.Target, args.Full));
                return 1;
            }
        }

        var logDir = config?.LogDir is { } d && config.Directory is { } cd ? Path.GetFullPath(d, cd) : null;
        var logPath = TestRunner.NewLogPath(target, logDir);
        var exit = await TestRunner.RunAsync(TestRunner.BuildArguments(target, args, plan.Adapter), cwd, logPath);

        var result = MtpParser.Parse(Ansi().Replace(File.ReadAllText(logPath), ""));
        result.ExitCode ??= exit != 0 ? exit : null;
        if (exit != 0 && result.Status == RunStatus.Pass)
        {
            result.Status = RunStatus.NoSummary; // dotnet failed although a summary said pass: never claim success
        }

        Console.Write(Output.Run(result, logPath, args.Target, args.Full));
        return result.Status == RunStatus.Pass ? 0 : 1;
    }

    private static RunResult PlanFailure(RunPlan plan)
    {
        var r = new RunResult();
        if (plan.Strategy is Strategy.Mixed)
        {
            r.Status = RunStatus.MixedFrameworks;
            r.FrameworkProjects.AddRange(plan.Detections.Select(d => $"{Path.GetFileNameWithoutExtension(d.Project)}: {d.Adapter!.Id}"));
            r.StatusHelp.Add("Run each project on its own: `dotnet-test-axi <project path> --class <name>`");
            return r;
        }

        r.Status = RunStatus.UnknownFramework;
        if (plan.Detections.Count == 0)
        {
            r.StatusHelp.Add("No test projects were found for this target; pass a project or solution path");
            return r;
        }

        foreach (var d in plan.Detections.Where(d => d.IsUnknown))
        {
            r.FrameworkProjects.Add(Path.GetFileNameWithoutExtension(d.Project));
            r.StatusHelp.Add(d.Help);
        }

        return r;
    }

    private static int FromLog(Args args)
    {
        if (!File.Exists(args.FromLog))
        {
            Console.Write(Output.Error($"log file '{args.FromLog}' not found", Output.ValidationError,
                "Pass the log: path printed by a previous run"));
            return 2;
        }

        var result = MtpParser.Parse(Ansi().Replace(File.ReadAllText(args.FromLog!), ""));
        Console.Write(Output.Run(result, Path.GetFullPath(args.FromLog!), args.Target, args.Full));
        return result.Status == RunStatus.Pass ? 0 : 1;
    }

    private static string Version() =>
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";
}
