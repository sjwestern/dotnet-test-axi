using Xunit;

namespace Samples.Xunit3;

public class SampleTests
{
    [Fact]
    public void Passes() => Assert.Equal(2, 1 + 1);

    [Fact]
    public void Fails() => Assert.Equal(Scenario.Failing ? 3 : 2, 1 + 1);

    [Fact]
    public void Throws() => Scenario.FailIfRequested("boom");

    [Fact(Skip = "not now")]
    public void Skipped() { }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Theory(int x) => Assert.True(x != 2 || !Scenario.Failing, $"value {x} is rejected");

    [Fact]
    public void Crashes() => Scenario.CrashIfRequested();
}
