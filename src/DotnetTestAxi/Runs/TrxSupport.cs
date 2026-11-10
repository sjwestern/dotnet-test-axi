using DotnetTestAxi.Frameworks;

namespace DotnetTestAxi.Runs;

internal enum TrxAvailability
{
    /// <summary>No TRX for this run: unknown framework, or the package ids could not be read.</summary>
    Unsupported,
    Available,

    /// <summary>The framework can write TRX but the project lacks the package that provides it.</summary>
    MissingPackage,
}

/// <summary>How one framework is asked for a TRX report. <see cref="ImpliedBy"/> is empty when no package is needed.</summary>
internal sealed record TrxOption(string Report, string FileName, IReadOnlyList<string> ImpliedBy);

/// <summary>Per-framework TRX options and the check for whether a project can produce one.</summary>
internal static class TrxSupport
{
    public const string Package = "Microsoft.Testing.Extensions.TrxReport";

    // The MSTest and TUnit metapackages depend on the extension; MSTest.TestAdapter or TUnit.Engine alone do not.
    // xUnit v3 has its own report option and needs no package.
    private static readonly Dictionary<string, TrxOption> Options = new(StringComparer.OrdinalIgnoreCase)
    {
        ["xunit3"] = new("--report-xunit-trx", "--report-xunit-trx-filename", []),
        ["mstest"] = new("--report-trx", "--report-trx-filename", ["MSTest", Package]),
        ["nunit"] = new("--report-trx", "--report-trx-filename", [Package]),
        ["tunit"] = new("--report-trx", "--report-trx-filename", ["TUnit", Package]),
    };

    public static TrxOption? OptionFor(string? adapterId) =>
        adapterId is not null && Options.TryGetValue(adapterId, out var o) ? o : null;

    /// <summary>The ids come from a direct-reference read, so a transitive package is invisible; the metapackage ids cover that.</summary>
    public static TrxAvailability Check(string? adapterId, IReadOnlyList<string>? packageIds)
    {
        if (OptionFor(adapterId) is not { } option) { return TrxAvailability.Unsupported; }
        if (option.ImpliedBy.Count == 0) { return TrxAvailability.Available; }
        if (packageIds is null or { Count: 0 }) { return TrxAvailability.Unsupported; } // could not tell, so do not nag
        return packageIds.Any(p => option.ImpliedBy.Contains(p, StringComparer.OrdinalIgnoreCase))
            ? TrxAvailability.Available
            : TrxAvailability.MissingPackage;
    }

    public static TrxAvailability Check(Detection d) => Check(d.Adapter?.Id, d.PackageIds);

    /// <summary>The report arguments that write <paramref name="trxPath"/>.</summary>
    public static IReadOnlyList<string> Arguments(string adapterId, string trxPath)
    {
        var o = OptionFor(adapterId) ?? throw new ArgumentException($"no TRX option for '{adapterId}'", nameof(adapterId));
        return [o.Report, o.FileName, Path.GetFileName(trxPath), "--results-directory", Path.GetDirectoryName(trxPath)!];
    }
}
