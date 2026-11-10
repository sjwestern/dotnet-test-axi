using DotnetTestAxi.Filtering;

namespace DotnetTestAxi.Frameworks.Adapters;

internal sealed class NUnitAdapter : IFrameworkAdapter
{
    public string Id => "nunit";

    public bool Matches(IEnumerable<string> packageIds) =>
        packageIds.Any(p => p.Equals("NUnit", StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> BuildFilterArguments(TestFilter filter) => VSTestExpression.Build(filter);
}
