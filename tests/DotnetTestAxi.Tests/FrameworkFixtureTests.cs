using Xunit;

namespace DotnetTestAxi.Tests;

/// <summary>
/// Parser behaviour against real output captured from the sample projects (see scripts/capture-fixtures.ps1).
/// Each framework prints failures a little differently, so every case runs for every framework.
/// </summary>
public class FrameworkFixtureTests
{
    public static TheoryData<string, string> Frameworks => new()
    {
        { "xunit3", "Xunit3.Sample.dll" },
        { "mstest", "MSTest.Sample.dll" },
        { "nunit", "NUnit.Sample.dll" },
        { "tunit", "TUnit.Sample.dll" },
    };

    private static RunResult Parse(string key, string scenario) => MtpParser.Parse(Fixtures.Read($"{key}-{scenario}.log"));

    [Theory]
    [MemberData(nameof(Frameworks))]
    public void Pass_counts_skipped_tests_and_has_no_failures(string key, string _)
    {
        var r = Parse(key, "pass");

        Assert.Equal(RunStatus.Pass, r.Status);
        Assert.Equal((8, 7, 0, 1), (r.Total, r.Passed, r.Failed, r.Skipped));
        Assert.Empty(r.Failures);
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public void Fail_reports_each_failing_test_without_stack_noise_or_machine_paths(string key, string _)
    {
        var r = Parse(key, "fail");

        Assert.Equal(RunStatus.Fail, r.Status);
        Assert.Equal((8, 3, 4, 1), (r.Total, r.Failed, r.Passed, r.Skipped));
        Assert.Equal(3, r.Failures.Count);
        Assert.Contains(r.Failures, f => f.Name.Contains("Fails"));
        Assert.Contains(r.Failures, f => f.Name.Contains("Throws"));
        Assert.Contains(r.Failures, f => f.Name.Contains("Theory"));
        Assert.All(r.Failures, f =>
        {
            Assert.NotEmpty(f.Message);
            Assert.DoesNotContain("/work/", f.Message);
            Assert.DoesNotMatch(@"\d\)\s+at ", f.Message);
            Assert.DoesNotContain("TestFailedException:", f.Message);
            Assert.DoesNotContain("internals omitted", f.Message);
        });
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public void Fail_reports_a_source_location_for_thrown_exceptions(string key, string _)
    {
        var r = Parse(key, "fail");

        var thrown = r.Failures.Single(f => f.Name.Contains("Throws"));
        Assert.Equal("Scenario.cs:20", thrown.Location);
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public void Host_crash_is_crashed_with_module_and_exit_code_not_a_pass_or_a_plain_failure(string key, string dll)
    {
        var r = Parse(key, "crash");

        Assert.Equal(RunStatus.Crashed, r.Status);
        Assert.NotNull(r.Crash);
        Assert.Equal(dll, r.Crash.Module);
        Assert.Equal(134, r.Crash.ExitCode);
        Assert.Contains("sample crash", r.Crash.Detail);
    }

    [Fact]
    public void Xunit3_class_filter_that_matches_nothing_is_no_tests()
    {
        Assert.Equal(RunStatus.NoTests, Parse("xunit3", "class-filter").Status);
    }

    [Theory]
    [InlineData("mstest")]
    [InlineData("nunit")]
    [InlineData("tunit")]
    public void Frameworks_without_the_xunit_filters_report_invalid_arguments(string key)
    {
        var r = Parse(key, "class-filter");

        Assert.Equal(RunStatus.InvalidArguments, r.Status);
    }

    [Fact]
    public void Rejected_option_names_are_reported_when_the_framework_prints_them()
    {
        Assert.Equal(["--filter-class"], Parse("mstest", "class-filter").RejectedOptions);
    }

    [Fact]
    public void Build_error_has_the_compile_error_and_no_test_counts()
    {
        var r = MtpParser.Parse(Fixtures.Read("build-error.log"));

        Assert.Equal(RunStatus.BuildError, r.Status);
        var e = Assert.Single(r.BuildErrors);
        Assert.Equal(("CS0103", 5), (e.Code, e.Line));
    }
}
