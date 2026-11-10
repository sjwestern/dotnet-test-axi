using Xunit;

namespace Samples.Xunit3Other;

public class OtherTests
{
    [Fact]
    public void Passes() => Assert.Equal(2, 1 + 1);

    [Fact]
    public void Fails() => Assert.Equal(2, Scenario.Failing ? 3 : 2);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Theory(int x) => Assert.True(x != 2 || !Scenario.Failing);
}
