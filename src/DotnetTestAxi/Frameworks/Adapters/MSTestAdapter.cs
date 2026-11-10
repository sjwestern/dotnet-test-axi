using DotnetTestAxi.Filtering;

namespace DotnetTestAxi.Frameworks.Adapters;

internal sealed class MSTestAdapter : IFrameworkAdapter
{
    public string Id => "mstest";

    public bool Matches(IEnumerable<string> packageIds) =>
        packageIds.Any(p => p.StartsWith("MSTest", StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> BuildFilterArguments(TestFilter filter) => VSTestExpression.Build(filter);
}
