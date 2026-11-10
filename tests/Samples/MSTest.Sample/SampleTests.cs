using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Samples.MSTest;

[TestClass]
public class SampleTests
{
    [TestMethod]
    public void Passes() => Assert.AreEqual(2, 1 + 1);

    [TestMethod]
    public void Fails() => Assert.AreEqual(Scenario.Failing ? 3 : 2, 1 + 1);

    [TestMethod]
    public void Throws() => Scenario.FailIfRequested("boom");

    [TestMethod]
    [Ignore("not now")]
    public void Skipped() { }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void Theory(int x) => Assert.IsTrue(x != 2 || !Scenario.Failing, $"value {x} is rejected");

    [TestMethod]
    public void Crashes() => Scenario.CrashIfRequested();
}
