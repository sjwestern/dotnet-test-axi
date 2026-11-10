using System.Text.Json.Nodes;
using Toon.Format;
using Xunit;

namespace DotnetTestAxi.Tests;

public class OutputTests
{
    private static JsonNode Decode(string toon) => ToonDecoder.Decode(toon)!;

    private static RunResult Fixture(string name) => MtpParser.Parse(Fixtures.Read(name));

    [Fact]
    public void Fail_output_round_trips_through_a_toon_decoder_with_colons_intact()
    {
        var text = Output.Run(Fixture("mtp-fail.log"), "/tmp/x.log", "unit", full: false);

        var o = Decode(text);

        Assert.Equal("fail", (string?)o["status"]);
        Assert.Equal(6L, (long?)o["total"]);
        var failures = o["failures"]!.AsArray();
        Assert.Equal(3, failures.Count);
        Assert.Equal("Demo.MathTests.Theory(x: 2)", (string?)failures[2]!["name"]);
        Assert.StartsWith("Assert.Equal() Failure: Values differ", (string?)failures[0]!["message"]);
        Assert.Equal("T.cs:6", (string?)failures[0]!["at"]);
        Assert.Equal("/tmp/x.log", (string?)o["log"]);
    }

    [Fact]
    public void Fail_help_suggests_class_rerun_and_reading_the_log()
    {
        var o = Decode(Output.Run(Fixture("mtp-fail.log"), "/tmp/x.log", "unit", full: false));

        var help = o["help"]!.AsArray().Select(h => (string?)h).ToList();

        Assert.Contains(help, h => h!.Contains("dotnet-test-axi unit --class \"Demo.MathTests\""));
        Assert.Contains(help, h => h!.StartsWith("Read /tmp/x.log"));
        Assert.DoesNotContain(help, h => h!.Contains("--full")); // nothing truncated
    }

    [Fact]
    public void Long_lists_are_truncated_with_a_count_and_a_from_log_hint_that_does_not_rerun()
    {
        var r = new RunResult { Status = RunStatus.Fail, Total = 30, Failed = 30 };
        for (var i = 0; i < 30; i++)
        {
            r.Failures.Add(new TestFailure($"A.B.T{i}", "m", null, null));
        }

        var o = Decode(Output.Run(r, "/tmp/x.log", "unit", full: false));
        var all = Decode(Output.Run(r, "/tmp/x.log", "unit", full: true));

        Assert.Equal(10, o["failures"]!.AsArray().Count);
        Assert.Equal("10 of 30 failures", (string?)o["count"]);
        Assert.Contains(o["help"]!.AsArray(), h => ((string?)h)!.Contains("--from-log /tmp/x.log --full"));
        Assert.Equal(30, all["failures"]!.AsArray().Count);
        Assert.Null(all["count"]);
    }

    [Fact]
    public void Long_messages_are_truncated_with_the_total_size_unless_full()
    {
        var r = new RunResult { Status = RunStatus.Fail, Total = 1, Failed = 1 };
        r.Failures.Add(new TestFailure("A.B.T", new string('x', 1000), null, null));

        var cut = Decode(Output.Run(r, "/tmp/x.log", "unit", full: false));
        var full = Decode(Output.Run(r, "/tmp/x.log", "unit", full: true));

        var message = (string?)cut["failures"]![0]!["message"];
        Assert.EndsWith("... (truncated, 1000 chars total)", message);
        Assert.Contains(cut["help"]!.AsArray(), h => ((string?)h)!.Contains("--full"));
        Assert.Equal(1000, ((string?)full["failures"]![0]!["message"])!.Length);
    }

    [Fact]
    public void Pass_is_definitive_and_has_no_suggestions()
    {
        var o = Decode(Output.Run(Fixture("mtp-pass.log"), "/tmp/x.log", "unit", full: false));

        Assert.Equal("pass", (string?)o["status"]);
        Assert.Equal(0L, (long?)o["failures"]);
        Assert.Null(o["help"]);
    }

    [Fact]
    public void Build_error_output_lists_errors_with_location()
    {
        var o = Decode(Output.Run(Fixture("mtp-build-error.log"), "/tmp/x.log", "unit", full: false));

        Assert.Equal("build-error", (string?)o["status"]);
        var e = o["errors"]![0]!;
        Assert.Equal("CS0246", (string?)e["code"]);
        Assert.Equal(6L, (long?)e["line"]);
        Assert.Null(o["total"]);
    }

    [Fact]
    public void No_tests_and_no_summary_explain_the_next_step()
    {
        var none = Decode(Output.Run(Fixture("mtp-zero.log"), "/tmp/x.log", "unit", full: false));
        var crash = Decode(Output.Run(MtpParser.Parse("boom"), "/tmp/x.log", "unit", full: false));

        Assert.Equal("no-tests", (string?)none["status"]);
        Assert.Contains("filters", (string?)none["help"]![0]);
        Assert.Equal("no-summary", (string?)crash["status"]);
        Assert.Contains("/tmp/x.log", (string?)crash["help"]![0]);
    }

    [Fact]
    public void Errors_have_error_code_and_help_on_stdout_format()
    {
        var o = Decode(Output.Error("unknown flag --stat", Output.ValidationError, "valid flags: --full"));

        Assert.Equal("unknown flag --stat", (string?)o["error"]);
        Assert.Equal("VALIDATION_ERROR", (string?)o["code"]);
        Assert.Equal("valid flags: --full", (string?)o["help"]![0]);
    }

    [Fact]
    public void Home_identifies_the_tool_first_then_shows_live_targets()
    {
        var config = new Config
        {
            Targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["unit"] = "tests/Unit" },
            Path = "/r/.dotnet-test-axi.json",
        };

        var text = Output.Home(config, "/usr/bin/dotnet-test-axi");
        var o = Decode(text);

        Assert.StartsWith("bin: ", text);
        Assert.Equal(Output.Description, (string?)o["description"]);
        Assert.Equal("unit", (string?)o["targets"]![0]!["name"]);
        Assert.NotEmpty(o["help"]!.AsArray());
    }

    [Fact]
    public void Home_without_config_says_so_definitively()
    {
        var o = Decode(Output.Home(null, "/usr/bin/dotnet-test-axi"));

        Assert.Equal("none", (string?)o["config"]);
        Assert.Equal("none defined", (string?)o["targets"]);
    }

    [Fact]
    public void Unsupported_runner_explains_and_names_the_projects()
    {
        var o = Decode(Output.Run(Fixture("vstest-in-mtp-solution.log"), "/tmp/x.log", "all", full: false));

        Assert.Equal("unsupported-runner", (string?)o["status"]);
        Assert.Contains("VSTest", (string?)o["reason"]);
        Assert.Equal("VSTest.Sample.csproj", (string?)o["projects"]![0]);
        Assert.Null(o["total"]);
        Assert.Contains(o["help"]!.AsArray(), h => ((string?)h)!.Contains("dotnet test <path>"));
    }

    [Fact]
    public void Trx_is_printed_beside_log_only_when_one_was_read()
    {
        var withTrx = Fixture("mtp-fail.log");
        withTrx.Trx.Add("/tmp/x.trx");

        var o = Decode(Output.Run(withTrx, "/tmp/x.log", "unit", full: false));
        var none = Decode(Output.Run(Fixture("mtp-fail.log"), "/tmp/x.log", "unit", full: false));

        Assert.Equal("/tmp/x.trx", (string?)o["trx"]);
        Assert.Null(none["trx"]);
        Assert.Null(none["warning"]);
    }

    [Fact]
    public void Multi_project_runs_list_every_trx()
    {
        var r = Fixture("mtp-fail.log");
        r.Trx.AddRange(["/tmp/a/A.trx", "/tmp/b/B.trx"]);

        var o = Decode(Output.Run(r, null, "all", full: false));

        Assert.Equal(["/tmp/a/A.trx", "/tmp/b/B.trx"], o["trxs"]!.AsArray().Select(n => (string?)n));
        Assert.Null(o["trx"]);
    }

    [Fact]
    public void Truncated_failures_point_at_the_trx()
    {
        var r = Fixture("mtp-fail.log");
        r.Failures.Add(new TestFailure("Demo.Long", new string('x', 400), null, null));
        r.Trx.Add("/tmp/x.trx");

        var help = Decode(Output.Run(r, "/tmp/x.log", "unit", full: false))["help"]!.AsArray().Select(n => (string?)n).ToList();

        Assert.Contains("Read /tmp/x.trx for complete messages and stack traces", help);
    }

    [Fact]
    public void Missing_trx_package_warns_once_with_a_complete_command_per_project()
    {
        var r = Fixture("mtp-pass.log");
        r.TrxMissing.Add("/work/App/App.csproj");
        r.TrxMissing.Add("/work/Other/Other.csproj");

        var o = Decode(Output.Run(r, "/tmp/x.log", "unit", full: false));

        Assert.Contains("Microsoft.Testing.Extensions.TrxReport", (string?)o["warning"]);
        Assert.Contains("App, Other", (string?)o["warning"]);
        var help = o["help"]!.AsArray().Select(n => (string?)n).ToList();
        Assert.Contains("Run `dotnet add /work/App/App.csproj package Microsoft.Testing.Extensions.TrxReport` for TRX results", help);
        Assert.Contains("Run `dotnet add /work/Other/Other.csproj package Microsoft.Testing.Extensions.TrxReport` for TRX results", help);
    }
}
