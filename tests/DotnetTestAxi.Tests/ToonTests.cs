using Xunit;

namespace DotnetTestAxi.Tests;

public class ToonTests
{
    [Fact]
    public void Fail_output_has_counts_failures_log_and_rerun_hint()
    {
        var r = MtpParser.Parse(Fixtures.Read("mtp-fail.log"));

        var text = Toon.Format(r, "/tmp/x.log", "unit", full: false);

        Assert.Contains("status: fail", text);
        Assert.Contains("total: 6", text);
        Assert.Contains("failures[3]{name,message,at}:", text);
        Assert.Contains("  Demo.MathTests.Fails,", text);
        Assert.Contains("log: /tmp/x.log", text);
        Assert.Contains("dotnet-test-axi unit --class \"Demo.MathTests\"", text);
    }

    [Fact]
    public void Failures_are_truncated_unless_full()
    {
        var r = new RunResult { Status = RunStatus.Fail, Total = 30, Failed = 30 };
        for (var i = 0; i < 30; i++)
        {
            r.Failures.Add(new TestFailure($"A.B.T{i}", "m", null, null));
        }

        var truncated = Toon.Format(r, null, null, full: false);
        var full = Toon.Format(r, null, null, full: true);

        Assert.Contains("failures[30]", truncated);
        Assert.Contains("more: 20 not shown (use --full)", truncated);
        Assert.DoesNotContain("A.B.T29", truncated);
        Assert.Contains("A.B.T29", full);
        Assert.DoesNotContain("more:", full);
    }

    [Fact]
    public void Pass_output_has_no_next_step()
    {
        var text = Toon.Format(MtpParser.Parse(Fixtures.Read("mtp-pass.log")), null, null, full: false);

        Assert.Contains("status: pass", text);
        Assert.Contains("next: none", text);
    }

    [Fact]
    public void Build_error_output_lists_errors()
    {
        var text = Toon.Format(MtpParser.Parse(Fixtures.Read("mtp-build-error.log")), null, null, full: false);

        Assert.Contains("status: build-error", text);
        Assert.Contains("errors[1]{file,line,code,message}:", text);
        Assert.Contains("CS0246", text);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [InlineData("", "\"\"")]
    public void Cells_are_quoted_only_when_needed(string input, string expected)
    {
        Assert.Equal(expected, Toon.Cell(input));
    }
}
