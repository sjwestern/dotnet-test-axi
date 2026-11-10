using Xunit;

namespace DotnetTestAxi.EndToEndTests;

/// <summary>
/// Runs the real tool against each sample project with --no-build (the samples are built by the project references).
/// These are slow compared with the parser tests: they start dotnet test for real.
/// </summary>
public class SampleRunTests
{
    public static TheoryData<string> Frameworks => new() { "Xunit3.Sample", "MSTest.Sample", "NUnit.Sample", "TUnit.Sample" };

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task Passing_run_is_pass_with_exit_0(string sample)
    {
        var (exit, _, o) = await ToolRun.RunAsync(sample, "pass", "--no-build");

        Assert.Equal(0, exit);
        Assert.Equal("pass", (string?)o["status"]);
        Assert.Equal(8L, (long?)o["total"]);
        Assert.Equal(1L, (long?)o["skipped"]);
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task Failing_run_lists_three_failures_with_exit_1(string sample)
    {
        var (exit, _, o) = await ToolRun.RunAsync(sample, "fail", "--no-build");

        Assert.Equal(1, exit);
        Assert.Equal("fail", (string?)o["status"]);
        Assert.Equal(3, o["failures"]!.AsArray().Count);
        Assert.NotNull(o["log"]);
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task Crashing_host_is_crashed_not_a_pass(string sample)
    {
        var (exit, _, o) = await ToolRun.RunAsync(sample, "crash", "--no-build");

        Assert.Equal(1, exit);
        Assert.Equal("crashed", (string?)o["status"]);
        // 134 (SIGABRT) without the TRX extension; the extension makes Microsoft Testing Platform report 7 instead.
        Assert.NotEqual(0L, (long?)o["host_exit"]);
    }

    [Theory]
    [InlineData("Xunit3.Sample", "Samples.Xunit3")]
    [InlineData("MSTest.Sample", "Samples.MSTest")]
    [InlineData("NUnit.Sample", "Samples.NUnitSample")]
    [InlineData("TUnit.Sample", "Samples.TUnitSample")]
    public async Task Class_filter_selects_the_class_on_every_framework(string sample, string ns)
    {
        var (exit, _, o) = await ToolRun.RunAsync(sample, "fail", "--no-build", "--class", ns + ".SampleTests");

        Assert.Equal(1, exit);
        Assert.Equal("fail", (string?)o["status"]);
        Assert.Equal(8L, (long?)o["total"]);
    }

    [Theory]
    [InlineData("Xunit3.Sample", "Samples.Xunit3")]
    [InlineData("MSTest.Sample", "Samples.MSTest")]
    [InlineData("NUnit.Sample", "Samples.NUnitSample")]
    [InlineData("TUnit.Sample", "Samples.TUnitSample")]
    public async Task Method_filter_selects_one_test_on_every_framework(string sample, string ns)
    {
        var (_, _, o) = await ToolRun.RunAsync(sample, "fail", "--no-build", "--method", ns + ".SampleTests.Fails");

        Assert.Equal("fail", (string?)o["status"]);
        Assert.Equal(1L, (long?)o["total"]);
    }

    [Fact]
    public async Task Build_error_is_build_error_with_the_location()
    {
        var (exit, _, o) = await ToolRun.RunAsync("Xunit3.Sample", "pass", "--", "-p:SampleBuildError=true");

        Assert.Equal(1, exit);
        Assert.Equal("build-error", (string?)o["status"]);
        Assert.Equal("CS0103", (string?)o["errors"]![0]!["code"]);
    }

    [Fact]
    public async Task Vstest_project_is_unsupported_runner_not_a_build_error()
    {
        var (exit, _, o) = await ToolRun.RunAsync("VSTest.Sample", "pass");

        Assert.Equal(1, exit);
        Assert.Equal("unsupported-runner", (string?)o["status"]);
    }

    [Fact]
    public async Task Unknown_flag_is_a_usage_error_with_exit_2()
    {
        var (exit, _, o) = await ToolRun.RunAsync("Xunit3.Sample", "pass", "--nope");

        Assert.Equal(2, exit);
        Assert.Equal("VALIDATION_ERROR", (string?)o["code"]);
    }
}
