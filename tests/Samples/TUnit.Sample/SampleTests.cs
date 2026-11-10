using TUnit.Assertions;
using TUnit.Core;

namespace Samples.TUnitSample;

public class SampleTests
{
    [Test]
    public async Task Passes() => await Assert.That(1 + 1).IsEqualTo(2);

    [Test]
    public async Task Fails() => await Assert.That(1 + 1).IsEqualTo(Scenario.Failing ? 3 : 2);

    [Test]
    public void Throws() => Scenario.FailIfRequested("boom");

    [Test]
    [Skip("not now")]
    public void Skipped() { }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task Theory(int x) => await Assert.That(x != 2 || !Scenario.Failing).IsTrue();

    [Test]
    public void Crashes() => Scenario.CrashIfRequested();
}
