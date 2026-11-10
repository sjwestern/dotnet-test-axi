using DotnetTestAxi.Runs;
using Xunit;

namespace DotnetTestAxi.Tests;

public sealed class TrxMergeTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("trxmerge").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Trx(string fixture)
    {
        var path = Path.Combine(_dir, fixture + ".trx");
        File.WriteAllText(path, Fixtures.Read(fixture + ".trx"));
        return path;
    }

    [Fact]
    public void Matching_trx_supplies_failures_and_keeps_the_status()
    {
        var r = MtpParser.Parse(Fixtures.Read("mstest-fail.log"));

        ResultSources.Finish(r, 2, Trx("mstest-fail"));

        Assert.Equal(RunStatus.Fail, r.Status);
        Assert.Equal(3, r.Failures.Count);
        Assert.All(r.Failures, f => Assert.Matches(@"^\w+\.cs:\d+$", f.Location));
        Assert.Single(r.Trx);
    }

    [Fact]
    public void Matching_trx_on_a_pass_stays_a_pass()
    {
        var r = MtpParser.Parse(Fixtures.Read("mstest-pass.log"));

        ResultSources.Finish(r, 0, Trx("mstest-pass"));

        Assert.Equal(RunStatus.Pass, r.Status);
        Assert.Empty(r.Failures);
    }

    [Fact]
    public void Counts_that_disagree_are_no_summary_with_help_naming_the_trx()
    {
        var r = MtpParser.Parse(Fixtures.Read("mstest-pass.log"));
        var trx = Trx("tunit-fail"); // 3 failures against a console pass

        ResultSources.Finish(r, 0, trx);

        Assert.Equal(RunStatus.NoSummary, r.Status);
        Assert.Contains(trx, Assert.Single(r.StatusHelp));
    }

    [Fact]
    public void Trx_is_the_summary_when_the_console_printed_none()
    {
        var r = MtpParser.Parse("some unrecognised output");
        Assert.Equal(RunStatus.NoSummary, r.Status);

        ResultSources.Finish(r, 0, Trx("nunit-pass"));

        Assert.Equal(RunStatus.Pass, r.Status);
        Assert.Equal(8, r.Total);
    }

    [Fact]
    public void Trx_without_console_summary_reports_its_failures()
    {
        var r = MtpParser.Parse("some unrecognised output");

        ResultSources.Finish(r, 2, Trx("xunit3-fail"));

        Assert.Equal(RunStatus.Fail, r.Status);
        Assert.Equal(3, r.Failures.Count);
    }

    [Fact]
    public void A_non_zero_exit_never_passes_even_with_a_clean_trx()
    {
        var r = MtpParser.Parse("some unrecognised output");
        ResultSources.Finish(r, 1, Trx("nunit-pass"));
        Assert.Equal(RunStatus.NoSummary, r.Status);

        var console = MtpParser.Parse(Fixtures.Read("mstest-pass.log"));
        ResultSources.Finish(console, 1, Trx("mstest-pass"));
        Assert.Equal(RunStatus.NoSummary, console.Status);
    }

    [Fact]
    public void Missing_or_invalid_trx_leaves_the_console_result_alone()
    {
        var missing = MtpParser.Parse(Fixtures.Read("mstest-fail.log"));
        ResultSources.Finish(missing, 2, Path.Combine(_dir, "nope.trx"));
        Assert.Equal(RunStatus.Fail, missing.Status);
        Assert.Empty(missing.Trx);

        var bad = Path.Combine(_dir, "bad.trx");
        File.WriteAllText(bad, "<TestRun>");
        var invalid = MtpParser.Parse(Fixtures.Read("mstest-fail.log"));
        ResultSources.Finish(invalid, 2, bad);
        Assert.Equal(RunStatus.Fail, invalid.Status);
        Assert.Empty(invalid.Trx);
    }

    [Fact]
    public void Build_errors_and_crashes_are_not_overridden_by_a_trx()
    {
        var crash = MtpParser.Parse(Fixtures.Read("mstest-crash.log"));
        Assert.Equal(RunStatus.Crashed, crash.Status);

        ResultSources.Finish(crash, 134, Trx("mstest-pass"));

        Assert.Equal(RunStatus.Crashed, crash.Status);
    }
}
