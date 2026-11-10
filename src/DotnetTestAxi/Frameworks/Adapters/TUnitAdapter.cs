using DotnetTestAxi.Filtering;

namespace DotnetTestAxi.Frameworks.Adapters;

/// <summary>Builds a <c>--treenode-filter</c> of the form /Assembly/Namespace/Class/Method.</summary>
internal sealed class TUnitAdapter : IFrameworkAdapter
{
    public string Id => "tunit";

    public bool Matches(IEnumerable<string> packageIds) =>
        packageIds.Any(p => p.StartsWith("TUnit", StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> BuildFilterArguments(TestFilter filter)
    {
        if (filter.IsEmpty)
        {
            return [];
        }

        string? ns = filter.Namespace?.Raw();
        string? cls = null;
        string? method = null;

        // When several flags constrain the same segment, the most specific one wins:
        // method-derived, then class-derived, then --namespace.
        if (filter.Class is { } c)
        {
            var (n, k) = SplitLast(c.Raw());
            ns = n ?? ns;
            cls = k;
        }

        if (filter.Method is { } m)
        {
            var (rest, name) = SplitLast(m.Raw());
            method = name;
            if (rest is not null)
            {
                var (n, k) = SplitLast(rest);
                ns = n ?? ns;
                cls = k;
            }
        }

        return ["--treenode-filter", $"/*/{ns ?? "*"}/{cls ?? "*"}/{method ?? "*"}"];
    }

    /// <summary>Splits at the last dot: (before, after). Without a dot, before is null.</summary>
    private static (string? Before, string After) SplitLast(string value)
    {
        var i = value.LastIndexOf('.');
        return i < 0 ? (null, value) : (value[..i], value[(i + 1)..]);
    }
}
