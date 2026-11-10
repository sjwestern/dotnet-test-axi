using DotnetTestAxi.Trx;
using Xunit;

namespace DotnetTestAxi.Tests;

public class TrxReaderTests
{
    [Theory]
    [InlineData("mstest-pass")]
    [InlineData("tunit-pass")]
    [InlineData("mstest-fail")]
    [InlineData("tunit-fail")]
    public void Counts_match_the_matching_log_fixture(string name)
    {
        var trx = TrxReader.Read(Fixtures.Read(name + ".trx"));
        var log = MtpParser.Parse(Fixtures.Read(name + ".log"));

        Assert.True(trx.IsValid);
        Assert.Equal((log.Total, log.Passed, log.Failed, log.Skipped), (trx.Total, trx.Passed, trx.Failed, trx.Skipped));
        Assert.Equal(log.Failures.Select(f => f.Name).Order(), trx.Failures.Select(f => f.Name).Order());
    }

    [Theory]
    [InlineData("mstest-pass")]
    [InlineData("tunit-pass")]
    public void Pass_has_no_failures(string name)
    {
        var trx = TrxReader.Read(Fixtures.Read(name + ".trx"));

        Assert.Equal(0, trx.Failed);
        Assert.Empty(trx.Failures);
        Assert.NotNull(trx.Duration);
    }

    [Theory]
    [InlineData("mstest-fail", "MSTest.Sample.dll")]
    [InlineData("tunit-fail", "TUnit.Sample.dll")]
    public void Failures_carry_message_location_and_assembly(string name, string assembly)
    {
        var trx = TrxReader.Read(Fixtures.Read(name + ".trx"));
        var log = MtpParser.Parse(Fixtures.Read(name + ".log"));

        Assert.Equal(3, trx.Failures.Count);
        var fails = trx.Failures.Single(f => f.Name == "Fails");
        Assert.Equal("SampleTests.cs:12", fails.Location);
        Assert.Equal(assembly, fails.Assembly);
        Assert.Contains("3", fails.Message);
        Assert.Equal("Scenario.cs:20", trx.Failures.Single(f => f.Name == "Throws").Location);
        Assert.Contains("boom", trx.Failures.Single(f => f.Name == "Throws").Message);

        // Location shape equals what MtpParser reports for the same failure.
        if (log.Failures.Single(f => f.Name == "Fails").Location is { } logLocation)
        {
            Assert.Equal(logLocation, fails.Location);
        }
    }

    [Fact]
    public void Skipped_outcome_is_skipped_not_failed()
    {
        var trx = TrxReader.Read(Fixtures.Read("mstest-pass.trx"));

        Assert.Equal(1, trx.Skipped);
    }

    [Fact]
    public void Paths_in_fixtures_are_sanitised()
    {
        Assert.DoesNotContain("/home/", Fixtures.Read("mstest-fail.trx"));
        Assert.DoesNotContain("/home/", Fixtures.Read("tunit-fail.trx"));
    }

    [Fact]
    public void Empty_run_is_valid_with_zero_counts()
    {
        var trx = TrxReader.Read("""<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results /></TestRun>""");

        Assert.True(trx.IsValid);
        Assert.Equal((0, 0, 0, 0), (trx.Total, trx.Passed, trx.Failed, trx.Skipped));
        Assert.Null(trx.Duration);
        Assert.Empty(trx.Failures);
    }

    [Fact]
    public void Failure_without_error_info_still_counts()
    {
        var trx = TrxReader.Read("""
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>
              <UnitTestResult testName="Bad" outcome="Failed" duration="00:00:00.0100000" />
              <UnitTestResult testName="Timed" outcome="Timeout" />
            </Results></TestRun>
            """);

        Assert.Equal(2, trx.Failed);
        Assert.Equal("", trx.Failures[0].Message);
        Assert.Null(trx.Failures[0].Location);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<TestRun><Results>")]
    [InlineData("not xml")]
    [InlineData("<Other />")]
    public void Malformed_xml_returns_an_error_result(string xml)
    {
        var trx = TrxReader.Read(xml);

        Assert.False(trx.IsValid);
        Assert.StartsWith("invalid TRX", trx.Error);
        Assert.Empty(trx.Failures);
    }
}
