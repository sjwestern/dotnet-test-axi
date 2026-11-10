using DotnetTestAxi.Filtering;

namespace DotnetTestAxi.Frameworks.Adapters;

internal sealed class Xunit3Adapter : IFrameworkAdapter
{
    public string Id => "xunit-v3";

    public bool Matches(IEnumerable<string> packageIds) =>
        packageIds.Any(p => p.StartsWith("xunit.v3", StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> BuildFilterArguments(TestFilter filter)
    {
        var list = new List<string>();
        if (filter.Namespace is { } ns) { list.Add("--filter-namespace"); list.Add(ns.Raw()); }
        if (filter.Class is { } c) { list.Add("--filter-class"); list.Add(c.Raw()); }
        if (filter.Method is { } m) { list.Add("--filter-method"); list.Add(m.Raw()); }
        return list;
    }
}
