namespace DotnetTestAxi.Runs;

/// <summary>One project's run inside a mixed-framework solution run.</summary>
internal sealed record ProjectRun(string Name, string Framework, RunResult Result, string? LogPath);

internal static class ResultMerger
{
    // Worst first. A project where the filter matched nothing (NoTests) is normal and ranks lowest.
    private static readonly RunStatus[] Severity =
    [
        RunStatus.BuildError,
        RunStatus.Crashed,
        RunStatus.UnsupportedRunner,
        RunStatus.InvalidArguments,
        RunStatus.NoSummary,
        RunStatus.UnknownFramework,
        RunStatus.Fail,
        RunStatus.Pass,
        RunStatus.NoTests,
    ];

    public static RunResult Merge(IReadOnlyList<ProjectRun> runs)
    {
        var merged = new RunResult();
        foreach (var run in runs)
        {
            var r = run.Result;
            merged.Total += r.Total;
            merged.Passed += r.Passed;
            merged.Failed += r.Failed;
            merged.Skipped += r.Skipped;
            merged.Failures.AddRange(r.Failures.Select(f => f with { Project = run.Name }));
            merged.BuildErrors.AddRange(r.BuildErrors);
            merged.VSTestProjects.AddRange(r.VSTestProjects);
            merged.RejectedOptions.AddRange(r.RejectedOptions.Except(merged.RejectedOptions));
            merged.Trx.AddRange(r.Trx);
            merged.TrxMissing.AddRange(r.TrxMissing);
            merged.Crash ??= r.Crash;
            merged.Projects.Add(new ProjectRow(run.Name, run.Framework, r.Status, r.Total, r.Failed));
            if (run.LogPath is not null)
            {
                merged.Logs.Add(run.LogPath);
            }
        }

        merged.Status = runs.Count == 0
            ? RunStatus.NoTests
            : runs.Select(r => r.Result.Status).OrderBy(s => Array.IndexOf(Severity, s)).First();
        return merged;
    }
}
