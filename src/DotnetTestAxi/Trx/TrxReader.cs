using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DotnetTestAxi.Trx;

/// <summary>Counts and failures read from one TRX document. <see cref="Error"/> is set when the XML could not be read.</summary>
internal sealed record TrxResult(
    int Total,
    int Passed,
    int Failed,
    int Skipped,
    string? Duration,
    IReadOnlyList<TestFailure> Failures,
    string? Error = null)
{
    public bool IsValid => Error is null;

    public static TrxResult Invalid(string error) => new(0, 0, 0, 0, null, [], error);
}

/// <summary>Reads the Visual Studio TRX schema. Runner agnostic: it knows nothing about Microsoft Testing Platform.</summary>
internal static partial class TrxReader
{
    private const int MaxMessageLines = 40;

    // "   at Ns.Type.Method() in /path/File.cs:line 12" (the path may contain " in ").
    [GeneratedRegex(@"^\s*at .+ in (?<path>.+):line (?<line>\d+)\s*$")]
    private static partial Regex StackFrame();

    public static TrxResult Read(string xml)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException e)
        {
            return TrxResult.Invalid($"invalid TRX: {e.Message}");
        }

        if (doc.Root is null || doc.Root.Name.LocalName != "TestRun")
        {
            return TrxResult.Invalid("invalid TRX: no TestRun element");
        }

        var assemblies = ReadAssemblies(doc.Root);
        int passed = 0, failed = 0, skipped = 0;
        var failures = new List<TestFailure>();
        var summed = TimeSpan.Zero;

        foreach (var r in doc.Root.Descendants().Where(e => e.Name.LocalName == "UnitTestResult"))
        {
            if (TimeSpan.TryParse((string?)r.Attribute("duration"), CultureInfo.InvariantCulture, out var d))
            {
                summed += d;
            }

            switch (Classify((string?)r.Attribute("outcome")))
            {
                case Outcome.Passed:
                    passed++;
                    break;
                case Outcome.Skipped:
                    skipped++;
                    break;
                default:
                    failed++;
                    var id = (string?)r.Attribute("executionId");
                    failures.Add(ToFailure(r, id is not null && assemblies.TryGetValue(id, out var asm) ? asm : null));
                    break;
            }
        }

        return new TrxResult(passed + failed + skipped, passed, failed, skipped, FormatDuration(doc.Root, summed), failures);
    }

    private enum Outcome { Passed, Failed, Skipped }

    private static Outcome Classify(string? outcome) => outcome switch
    {
        "Passed" or "Completed" or "PassedButRunAborted" => Outcome.Passed,
        "NotExecuted" or "Inconclusive" or "NotRunnable" or "Pending" => Outcome.Skipped,
        _ => Outcome.Failed, // Failed, Error, Timeout, Aborted, Disconnected and anything unknown
    };

    private static TestFailure ToFailure(XElement result, string? assembly)
    {
        var info = result.Descendants().FirstOrDefault(e => e.Name.LocalName == "ErrorInfo");
        var message = info?.Elements().FirstOrDefault(e => e.Name.LocalName == "Message")?.Value ?? "";
        var stack = info?.Elements().FirstOrDefault(e => e.Name.LocalName == "StackTrace")?.Value;

        var lines = message.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Take(MaxMessageLines);
        return new TestFailure(
            (string?)result.Attribute("testName") ?? "(unnamed)",
            string.Join(" | ", lines),
            FirstLocation(stack),
            assembly);
    }

    /// <summary>The first frame with source info, as "File.cs:12" (the shape MtpParser produces).</summary>
    private static string? FirstLocation(string? stack)
    {
        if (stack is null)
        {
            return null;
        }

        foreach (var line in stack.Split('\n'))
        {
            if (StackFrame().Match(line.TrimEnd('\r')) is { Success: true } m)
            {
                return $"{Path.GetFileName(m.Groups["path"].Value.Replace('\\', '/'))}:{m.Groups["line"].Value}";
            }
        }

        return null;
    }

    /// <summary>Maps result execution id to the test assembly file name, from TestDefinitions.</summary>
    private static Dictionary<string, string> ReadAssemblies(XElement root)
    {
        var map = new Dictionary<string, string>();
        foreach (var test in root.Descendants().Where(e => e.Name.LocalName == "UnitTest"))
        {
            var id = test.Elements().FirstOrDefault(e => e.Name.LocalName == "Execution")?.Attribute("id")?.Value;
            var code = test.Elements().FirstOrDefault(e => e.Name.LocalName == "TestMethod")?.Attribute("codeBase")?.Value
                ?? (string?)test.Attribute("storage");
            if (id is not null && !string.IsNullOrEmpty(code))
            {
                map[id] = Path.GetFileName(code.Replace('\\', '/'));
            }
        }

        return map;
    }

    /// <summary>Wall clock from Times when present, otherwise the sum of test durations. Formatted like the MTP summary.</summary>
    private static string? FormatDuration(XElement root, TimeSpan summed)
    {
        var times = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Times");
        var span = summed;
        if (DateTimeOffset.TryParse((string?)times?.Attribute("start"), CultureInfo.InvariantCulture, out var start)
            && DateTimeOffset.TryParse((string?)times?.Attribute("finish"), CultureInfo.InvariantCulture, out var finish)
            && finish >= start)
        {
            span = finish - start;
        }

        if (span == TimeSpan.Zero)
        {
            return null;
        }

        var ms = (long)span.TotalMilliseconds;
        return ms < 1000 ? $"{ms}ms" : ms < 60_000 ? $"{ms / 1000}s {ms % 1000:000}ms" : $"{ms / 60_000}m {ms % 60_000 / 1000:00}s";
    }
}
