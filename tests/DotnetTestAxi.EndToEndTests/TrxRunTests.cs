using System.Text.Json.Nodes;
using DotnetTestAxi.Trx;
using Xunit;

namespace DotnetTestAxi.EndToEndTests;

/// <summary>TRX as a result source, fallback to the console log, and the missing-package warning, against the real samples.</summary>
public class TrxRunTests
{
    public static TheoryData<string> Frameworks => new() { "Xunit3.Sample", "MSTest.Sample", "NUnit.Sample", "TUnit.Sample" };

    private static IEnumerable<string?> Help(JsonNode o) => o["help"]?.AsArray().Select(n => (string?)n) ?? [];

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task Passing_run_prints_a_trx_beside_the_log_that_parses_to_the_same_counts(string sample)
    {
        var (exit, _, o) = await ToolRun.RunAsync(sample, "pass", "--no-build");

        Assert.Equal(0, exit);
        Assert.Equal("pass", (string?)o["status"]);
        var (log, trx) = Paths(o);
        Assert.True(File.Exists(trx));
        var read = TrxReader.Read(File.ReadAllText(trx));
        Assert.True(read.IsValid);
        Assert.Equal(((long?)o["total"], (long?)o["passed"], (long?)o["skipped"]), (read.Total, read.Passed, read.Skipped));
        Assert.Null(o["warning"]);
        Assert.Equal(Path.GetDirectoryName(log), Path.GetDirectoryName(trx));
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task Failing_run_takes_failures_from_the_trx_with_locations(string sample)
    {
        var (exit, _, o) = await ToolRun.RunAsync(sample, "fail", "--no-build");

        Assert.Equal(1, exit);
        Assert.Equal("fail", (string?)o["status"]);
        var failures = o["failures"]!.AsArray();
        Assert.Equal(3, failures.Count);
        Assert.All(failures, f => Assert.Matches(@"^\w+\.cs:\d+$", (string?)f!["at"]));
        Assert.True(File.Exists((string?)o["trx"]));
    }

    [Fact]
    public async Task Log_and_trx_share_a_stem_inside_a_dated_run_folder()
    {
        var (_, _, o) = await ToolRun.RunAsync("MSTest.Sample", "pass", "--no-build");

        var (log, trx) = Paths(o);
        Assert.Equal(Path.ChangeExtension(log, ".trx"), trx);
        var runFolder = Path.GetDirectoryName(log)!;
        Assert.Matches(@"^MSTest-\d{6}(-\d+)?$", Path.GetFileName(runFolder));
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", Path.GetFileName(Path.GetDirectoryName(runFolder)));
    }

    [Fact]
    public async Task Project_without_the_trx_package_falls_back_to_the_console_and_warns()
    {
        var (exit, _, o) = await ToolRun.RunAsync("NUnit.NoTrx.Sample", "fail", "--no-build");

        Assert.Equal(1, exit);
        Assert.Equal("fail", (string?)o["status"]);
        Assert.Equal(8L, (long?)o["total"]);
        Assert.Equal(3, o["failures"]!.AsArray().Count);
        Assert.Null(o["trx"]);
        Assert.Contains("Microsoft.Testing.Extensions.TrxReport", (string?)o["warning"]);
        Assert.Contains(Help(o), h => h!.StartsWith("Run `dotnet add ") && h.Contains("NUnit.NoTrx.Sample.csproj package Microsoft.Testing.Extensions.TrxReport`"));
    }

    [Fact]
    public async Task Fallback_on_a_passing_run_warns_too()
    {
        var (exit, _, o) = await ToolRun.RunAsync("NUnit.NoTrx.Sample", "pass", "--no-build");

        Assert.Equal(0, exit);
        Assert.Equal("pass", (string?)o["status"]);
        Assert.NotNull(o["warning"]);
    }

    [Fact]
    public async Task Filtered_mixed_solution_reports_a_trx_per_project()
    {
        var (exit, _, o) = await ToolRun.RunAsync("Mixed.slnx", "fail", "--no-build", "--method", "*Fails", "--parallel", "1");

        Assert.Equal(1, exit);
        var trxs = o["trxs"]!.AsArray().Select(n => (string)n!).ToList();
        Assert.Equal(4, trxs.Count);
        Assert.All(trxs, t => Assert.True(File.Exists(t)));
        Assert.Equal(4, trxs.Distinct().Count());
        Assert.Equal(4, o["failures"]!.AsArray().Count);
        Assert.Null(o["warning"]);
    }

    [Fact]
    public async Task Library_project_without_a_test_framework_is_skipped_not_no_summary()
    {
        var (exit, _, o) = await ToolRun.RunAsync("WithLibrary.slnx", "pass");

        Assert.Equal(0, exit);
        Assert.Equal("pass", (string?)o["status"]);
        Assert.Equal(["Library.Sample"], o["skipped_projects"]!.AsArray().Select(n => (string)n!).ToArray());
        Assert.Equal(8L, (long?)o["total"]);
    }

    [Fact]
    public async Task Multi_project_run_without_a_filter_gets_one_trx_per_project()
    {
        var (exit, _, o) = await ToolRun.RunAsync("SameFramework.slnx", "pass", "--no-build");

        Assert.Equal(0, exit);
        Assert.Equal("pass", (string?)o["status"]);
        Assert.Equal(12L, (long?)o["total"]);
        Assert.Null(o["trx"]);
        var trxs = o["trxs"]!.AsArray().Select(n => (string)n!).ToList();
        Assert.Equal(2, trxs.Count);
        Assert.Equal(2, trxs.Distinct().Count());
        Assert.All(trxs, t => Assert.True(File.Exists(t)));
        Assert.Equal(2, o["projects"]!.AsArray().Count);
        Assert.Null(o["warning"]);
    }

    [Theory]
    [InlineData("MSTest.Sample")]
    [InlineData("TUnit.Sample")]
    public async Task Crash_is_still_crashed_with_the_trx_extension_active(string sample)
    {
        var (exit, _, o) = await ToolRun.RunAsync(sample, "crash", "--no-build");

        Assert.Equal(1, exit);
        Assert.Equal("crashed", (string?)o["status"]);
        Assert.True(File.Exists((string?)o["trx"])); // the extension writes a partial report as the session ends abnormally
    }

    private static (string Log, string Trx) Paths(JsonNode o) => ((string)o["log"]!, (string)o["trx"]!);
}
