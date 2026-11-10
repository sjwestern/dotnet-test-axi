using DotnetTestAxi.Filtering;

namespace DotnetTestAxi.Frameworks.Adapters;

/// <summary>Translates a <see cref="TestFilter"/> into the native <c>dotnet test</c> arguments of one framework.</summary>
internal interface IFrameworkAdapter
{
    string Id { get; }

    bool Matches(IEnumerable<string> packageIds);

    IReadOnlyList<string> BuildFilterArguments(TestFilter filter);
}

internal static class PatternExtensions
{
    /// <summary>The value as the user wrote it, including any leading or trailing wildcard.</summary>
    public static string Raw(this Pattern p) =>
        (p.LeadingWildcard ? "*" : "") + p.Text + (p.TrailingWildcard ? "*" : "");
}

internal static class AdapterRegistry
{
    public static IReadOnlyList<IFrameworkAdapter> All { get; } =
    [
        new Xunit3Adapter(),
        new MSTestAdapter(),
        new NUnitAdapter(),
        new TUnitAdapter(),
    ];

    public static IFrameworkAdapter? Find(IEnumerable<string> packageIds)
    {
        var ids = packageIds.ToList();
        return All.FirstOrDefault(a => a.Matches(ids));
    }
}
