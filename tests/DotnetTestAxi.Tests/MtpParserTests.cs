using Xunit;

namespace DotnetTestAxi.Tests;

public class MtpParserTests
{
    [Fact]
    public void Pass_log_is_pass_with_counts()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-pass.log"));

        Assert.Equal(RunStatus.Pass, r.Status);
        Assert.Equal(1, r.Total);
        Assert.Equal(1, r.Passed);
        Assert.Equal(0, r.Failed);
        Assert.Empty(r.Failures);
        Assert.Equal("1s 014ms", r.Duration);
    }

    [Fact]
    public void Fail_log_lists_each_failure_with_message_and_location()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-fail.log"));

        Assert.Equal(RunStatus.Fail, r.Status);
        Assert.Equal((6, 3, 2, 1), (r.Total, r.Failed, r.Passed, r.Skipped));
        Assert.Equal(2, r.ExitCode);
        Assert.Equal(
            ["Demo.MathTests.Fails", "Demo.MathTests.Throws", "Demo.MathTests.Theory(x: 2)"],
            r.Failures.Select(f => f.Name));

        var fails = r.Failures[0];
        Assert.Equal("T.cs:7", r.Failures[1].Location);
        Assert.Equal("T.cs:6", fails.Location);
        Assert.Equal("demo.dll", fails.Assembly);
        Assert.StartsWith("Assert.Equal() Failure: Values differ | Expected: 3 | Actual:   2", fails.Message);
        Assert.Contains("InvalidOperationException : boom", r.Failures[1].Message);
    }

    [Fact]
    public void Skipped_tests_are_not_failures()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-fail.log"));

        Assert.DoesNotContain(r.Failures, f => f.Name.Contains("Skipped"));
    }

    [Fact]
    public void Noisy_application_logs_after_failures_are_ignored_and_summaries_are_summed()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-noisy-fail.log"));

        Assert.Equal(RunStatus.Fail, r.Status);
        Assert.Equal(2, r.Failures.Count);
        Assert.Equal(9, r.Total);
        Assert.All(r.Failures, f => Assert.DoesNotContain("testcontainers", f.Message));
        Assert.Equal("Metadata.cs:41", r.Failures[0].Location);
    }

    [Fact]
    public void Zero_tests_is_no_tests_not_pass()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-zero.log"));

        Assert.Equal(RunStatus.NoTests, r.Status);
        Assert.Equal(8, r.ExitCode);
    }

    [Fact]
    public void Build_error_log_is_build_error_with_location()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-build-error.log"));

        Assert.Equal(RunStatus.BuildError, r.Status);
        var e = Assert.Single(r.BuildErrors);
        Assert.Equal(("/work/demo/T.cs", 6, "CS0246"), (e.File, e.Line, e.Code));
        Assert.StartsWith("The type or namespace name 'InvalidOperationException'", e.Message);
        Assert.DoesNotContain("demo.csproj", e.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unhandled exception. System.Exception: crash\n   at Foo.Bar()\n")]
    public void Output_without_a_summary_is_never_a_pass(string text)
    {
        var r = MtpParser.Parse(text);

        Assert.Equal(RunStatus.NoSummary, r.Status);
    }

    [Fact]
    public void Windows_line_endings_are_handled()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-fail.log").Replace("\n", "\r\n"));

        Assert.Equal(RunStatus.Fail, r.Status);
        Assert.Equal(3, r.Failures.Count);
    }

    [Fact]
    public void Solution_run_lists_projects_inside_the_summary_before_the_totals()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-solution-pass.log"));

        Assert.Equal(RunStatus.Pass, r.Status);
        Assert.Equal((662, 662, 0, 0), (r.Total, r.Passed, r.Failed, r.Skipped));
        Assert.Equal("5s 227ms", r.Duration);
    }

    [Fact]
    public void Solution_run_failure_keeps_totals_and_failures()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-solution-fail.log"));

        Assert.Equal(RunStatus.Fail, r.Status);
        Assert.Equal((662, 1, 661), (r.Total, r.Failed, r.Passed));
        var f = Assert.Single(r.Failures);
        Assert.Equal("Unit.Orders.OrderTests.Rejects_empty", f.Name);
        Assert.Equal("OrderTests.cs:21", f.Location);
    }
}
