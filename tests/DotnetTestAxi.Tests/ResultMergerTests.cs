using DotnetTestAxi.Runs;
using Xunit;

namespace DotnetTestAxi.Tests;

public class ResultMergerTests
{
    private static ProjectRun Run(string name, RunStatus status, int total = 0, int failed = 0)
    {
        var r = new RunResult { Status = status, Total = total, Failed = failed, Passed = total - failed };
        if (failed > 0) { r.Failures.Add(new TestFailure("T", "boom", null, null)); }
        return new ProjectRun(name, "mstest", r, $"/logs/{name}.log");
    }

    private static RunStatus Status(params RunStatus[] statuses) =>
        ResultMerger.Merge(statuses.Select((s, i) => Run("P" + i, s)).ToList()).Status;

    [Fact]
    public void Counts_are_summed_and_failures_get_their_project()
    {
        var m = ResultMerger.Merge([Run("A", RunStatus.Pass, 3), Run("B", RunStatus.Fail, 5, 2)]);
        Assert.Equal(8, m.Total);
        Assert.Equal(6, m.Passed);
        Assert.Equal(2, m.Failed);
        Assert.Equal("B", Assert.Single(m.Failures).Project);
        Assert.Equal(2, m.Projects.Count);
        Assert.Equal(["/logs/A.log", "/logs/B.log"], m.Logs);
    }

    [Fact]
    public void No_tests_only_when_nothing_ran_anywhere()
    {
        Assert.Equal(RunStatus.NoTests, Status(RunStatus.NoTests, RunStatus.NoTests));
        Assert.Equal(RunStatus.Pass, Status(RunStatus.NoTests, RunStatus.Pass));
        Assert.Equal(RunStatus.NoTests, Status());
    }

    [Fact]
    public void Severity_order_is_build_error_crash_fail_pass()
    {
        Assert.Equal(RunStatus.BuildError, Status(RunStatus.Pass, RunStatus.Fail, RunStatus.Crashed, RunStatus.BuildError));
        Assert.Equal(RunStatus.Crashed, Status(RunStatus.Pass, RunStatus.Fail, RunStatus.Crashed));
        Assert.Equal(RunStatus.Fail, Status(RunStatus.Pass, RunStatus.NoTests, RunStatus.Fail));
        Assert.Equal(RunStatus.NoSummary, Status(RunStatus.Fail, RunStatus.NoSummary));
    }

    [Fact]
    public void Default_parallelism_is_half_the_processors_between_1_and_8()
    {
        Assert.Equal(1, MixedRun.DefaultParallel(1));
        Assert.Equal(4, MixedRun.DefaultParallel(8));
        Assert.Equal(8, MixedRun.DefaultParallel(64));
    }

    [Fact]
    public void Parallel_flag_needs_a_positive_integer()
    {
        Assert.Equal(3, Args.Parse(["x", "--parallel", "3"]).Parallel);
        Assert.NotNull(Args.Parse(["x", "--parallel", "0"]).Error);
        Assert.NotNull(Args.Parse(["x", "--parallel", "many"]).Error);
        Assert.NotNull(Args.Parse(["x", "--parallel"]).Error);
    }
}
