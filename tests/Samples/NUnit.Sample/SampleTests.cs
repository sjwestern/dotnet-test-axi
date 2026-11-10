using NUnit.Framework;

namespace Samples.NUnitSample;

public class SampleTests
{
    [Test]
    public void Passes() => Assert.That(1 + 1, Is.EqualTo(2));

    [Test]
    public void Fails() => Assert.That(1 + 1, Is.EqualTo(Scenario.Failing ? 3 : 2));

    [Test]
    public void Throws() => Scenario.FailIfRequested("boom");

    [Test]
    [Ignore("not now")]
    public void Skipped() { }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Theory(int x) => Assert.That(x != 2 || !Scenario.Failing, $"value {x} is rejected");

    [Test]
    public void Crashes() => Scenario.CrashIfRequested();
}
