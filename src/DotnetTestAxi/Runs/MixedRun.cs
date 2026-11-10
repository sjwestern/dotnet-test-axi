using System.Text.RegularExpressions;
using DotnetTestAxi.Frameworks;

namespace DotnetTestAxi.Runs;

/// <summary>Builds a mixed-framework solution once, then runs each project with its own filter in parallel.</summary>
internal static partial class MixedRun
{
    public const int MaxDefaultParallel = 8;

    [GeneratedRegex("\u001b\\[[0-9;?]*[ -/]*[@-~]")]
    private static partial Regex Ansi();

    public static int DefaultParallel(int processorCount) => Math.Clamp(processorCount / 2, 1, MaxDefaultParallel);

    public static async Task<RunResult> RunAsync(RunPlan plan, Args args, string target, string cwd, string? logDir)
    {
        if (!args.NoBuild)
        {
            var buildLog = TestRunner.NewArtifacts(target, logDir).Log;
            var exit = await TestRunner.RunAsync(["build", target, "--nologo"], cwd, buildLog);
            if (exit != 0)
            {
                var build = MtpParser.Parse(Ansi().Replace(File.ReadAllText(buildLog), ""));
                build.Status = build.BuildErrors.Count > 0 ? RunStatus.BuildError : RunStatus.NoSummary;
                build.ExitCode = exit;
                build.Logs.Add(buildLog);
                return build;
            }
        }

        var limit = args.Parallel ?? DefaultParallel(Environment.ProcessorCount);
        using var gate = new SemaphoreSlim(limit);
        var tasks = plan.Detections.Select(async d =>
        {
            await gate.WaitAsync();
            try
            {
                return await RunProjectAsync(d, args, cwd, logDir);
            }
            finally
            {
                gate.Release();
            }
        });
        var runs = await Task.WhenAll(tasks);
        return ResultMerger.Merge(runs);
    }

    private static async Task<ProjectRun> RunProjectAsync(Detection d, Args args, string cwd, string? logDir)
    {
        var name = Path.GetFileNameWithoutExtension(d.Project);
        var artifacts = TestRunner.NewArtifacts(d.Project, logDir);
        var logPath = artifacts.Log;
        var detection = await FrameworkDetector.WithPackageIdsAsync(d);
        var availability = TrxSupport.Check(detection);
        var trxPath = availability == TrxAvailability.Available ? artifacts.Trx : null;
        var arguments = TestRunner.BuildArguments(d.Project, args, d.Adapter, noBuild: true,
            trxArguments: trxPath is null ? null : TrxSupport.Arguments(d.Adapter!.Id, trxPath));
        var exit = await TestRunner.RunAsync(arguments, cwd, logPath);
        var result = MtpParser.Parse(Ansi().Replace(File.ReadAllText(logPath), ""));
        ResultSources.Finish(result, exit, trxPath);
        if (availability == TrxAvailability.MissingPackage) { result.TrxMissing.Add(d.Project); }

        return new ProjectRun(name, d.Adapter!.Id, result, logPath);
    }
}
