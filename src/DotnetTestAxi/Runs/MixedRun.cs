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
            var buildLog = TestRunner.NewLogPath(target, logDir);
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
        var logPath = TestRunner.NewLogPath(d.Project, logDir);
        var arguments = TestRunner.BuildArguments(d.Project, args, d.Adapter, noBuild: true);
        var exit = await TestRunner.RunAsync(arguments, cwd, logPath);
        var result = MtpParser.Parse(Ansi().Replace(File.ReadAllText(logPath), ""));
        result.ExitCode ??= exit != 0 ? exit : null;
        if (exit != 0 && result.Status == RunStatus.Pass)
        {
            result.Status = RunStatus.NoSummary; // never claim success when dotnet failed
        }

        return new ProjectRun(name, d.Adapter!.Id, result, logPath);
    }
}
