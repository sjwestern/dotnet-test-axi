using System.Reflection;
using DotnetTestAxi.Frameworks;
using DotnetTestAxi.Runs;
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
        if (args.Filter.IsEmpty)
        {
            // TRX needs the framework, so detect when the target is one project. Several projects share one
            // report file name, so a multi-project run without a filter stays on the console log.
            if (TestRunner.ProjectsOf(target) is [var only])
            {
                plan = RunPlan.Choose(true, [await FrameworkDetector.DetectAsync(only, config)]);
            }
        }
        else
        {
            var detections = await FrameworkDetector.DetectAsync(TestRunner.ProjectsOf(target), config);
            plan = RunPlan.Choose(false, detections);
            if (plan.Strategy is Strategy.Unknown)
            {
                Console.Write(Output.Run(PlanFailure(plan), null, args.Target, args.Full));
                return 1;
            }

            if (plan.Strategy is Strategy.Mixed)
            {
                var cfgLogDir = config?.LogDir is { } ld && config.Directory is { } cdir ? Path.GetFullPath(ld, cdir) : null;
                var merged = await MixedRun.RunAsync(plan, args, target, cwd, cfgLogDir);
                Console.Write(Output.Run(merged, null, args.Target, args.Full));
                return merged.Status == RunStatus.Pass ? 0 : 1;
            }
        }

        var logDir = config?.LogDir is { } d && config.Directory is { } cd ? Path.GetFullPath(d, cd) : null;
        var artifacts = TestRunner.NewArtifacts(target, logDir);
        var logPath = artifacts.Log;
        string? trxPath = null;
        string? missingTrx = null;
        IReadOnlyList<string>? trxArguments = null;
        if (plan.Detections is [var single])
        {
            var detection = await FrameworkDetector.WithPackageIdsAsync(single);
            switch (TrxSupport.Check(detection))
            {
                case TrxAvailability.Available:
                    trxPath = artifacts.Trx;
                    trxArguments = TrxSupport.Arguments(detection.Adapter!.Id, trxPath);
                    break;
                case TrxAvailability.MissingPackage:
                    missingTrx = detection.Project;
                    break;
            }
        }

        var exit = await TestRunner.RunAsync(TestRunner.BuildArguments(target, args, plan.Adapter, trxArguments: trxArguments), cwd, logPath);

        var result = MtpParser.Parse(Ansi().Replace(File.ReadAllText(logPath), ""));
        ResultSources.Finish(result, exit, trxPath);
        if (missingTrx is not null) { result.TrxMissing.Add(missingTrx); }

        Console.Write(Output.Run(result, logPath, args.Target, args.Full));
        return result.Status == RunStatus.Pass ? 0 : 1;
    }

    private static RunResult PlanFailure(RunPlan plan)
    {
        var r = new RunResult();
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
