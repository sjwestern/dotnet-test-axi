using Xunit;

namespace DotnetTestAxi.EndToEndTests;

/// <summary>Filters across frameworks, same-framework solutions and mixed solutions, run against the real samples.</summary>
public class FilterRunTests
{
    public static TheoryData<string, string> Namespaces => new()
    {
        { "Xunit3.Sample", "Samples.Xunit3" },
        { "MSTest.Sample", "Samples.MSTest" },
        { "NUnit.Sample", "Samples.NUnitSample" },
        { "TUnit.Sample", "Samples.TUnitSample" },
    };

    [Theory]
    [MemberData(nameof(Namespaces))]
    public async Task Namespace_filter_selects_the_namespace_on_every_framework(string sample, string ns)
    {
        var (_, _, o) = await ToolRun.RunAsync(sample, "pass", "--no-build", "--namespace", ns);

        Assert.Equal("pass", (string?)o["status"]);
        Assert.Equal(8L, (long?)o["total"]);
    }

    [Theory]
    [MemberData(nameof(Namespaces))]
    public async Task Wildcard_method_selects_by_prefix_on_every_framework(string sample, string ns)
    {
        var (_, _, o) = await ToolRun.RunAsync(sample, "pass", "--no-build", "--method", ns + ".SampleTests.Th*");

        Assert.Equal("pass", (string?)o["status"]);
        Assert.Equal(4L, (long?)o["total"]);
    }

    [Theory]
    [InlineData("--method", "Fails")]
    [InlineData("--class", "Samples.*.SampleTests")]
    [InlineData("--namespace", "A.*.B")]
    public async Task Invalid_filter_values_are_usage_errors_before_anything_runs(string flag, string value)
    {
        var (exit, _, o) = await ToolRun.RunAsync("Xunit3.Sample", "pass", flag, value);

        Assert.Equal(2, exit);
        Assert.Equal("VALIDATION_ERROR", (string?)o["code"]);
        Assert.NotNull(o["help"]);
    }

    [Fact]
    public async Task Same_framework_solution_runs_once_with_merged_counts()
    {
        var (exit, _, o) = await ToolRun.RunAsync("SameFramework.slnx", "pass", "--no-build", "--method", "*Passes");

        Assert.Equal(0, exit);
        Assert.Equal("pass", (string?)o["status"]);
        Assert.Equal(2L, (long?)o["total"]);
        Assert.Null(o["projects"]); // single dotnet test run, no per-project table
    }

    [Fact]
    public async Task Mixed_solution_runs_per_project_and_lists_each_one()
    {
        var (exit, _, o) = await ToolRun.RunAsync("Mixed.slnx", "pass", "--no-build", "--class", "Samples.Xunit3.SampleTests");

        Assert.Equal(0, exit);
        Assert.Equal("pass", (string?)o["status"]);
        Assert.Equal(8L, (long?)o["total"]);
        var rows = o["projects"]!.AsArray();
        Assert.Equal(4, rows.Count);
        Assert.Equal(["mstest", "no-tests"], Row(rows, "MSTest.Sample"));
        Assert.Equal(["xunit3", "pass"], Row(rows, "Xunit3.Sample"));
    }

    [Fact]
    public async Task Mixed_solution_wildcard_selects_in_every_project_and_failures_name_their_project()
    {
        var (exit, _, o) = await ToolRun.RunAsync("Mixed.slnx", "fail", "--no-build", "--method", "*Fails", "--parallel", "1");

        Assert.Equal(1, exit);
        Assert.Equal("fail", (string?)o["status"]);
        Assert.Equal(4L, (long?)o["total"]);
        var failures = o["failures"]!.AsArray();
        Assert.Equal(4, failures.Count);
        Assert.All(failures, f => Assert.NotNull(f!["project"]));
        Assert.Equal(4, o["logs"]!.AsArray().Count);
    }

    private static string?[] Row(System.Text.Json.Nodes.JsonArray rows, string name)
    {
        var row = rows.Single(r => (string?)r!["name"] == name)!;
        return [(string?)row["framework"], (string?)row["status"]];
    }
}
