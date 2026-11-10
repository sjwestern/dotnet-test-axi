using System.Text.RegularExpressions;

namespace DotnetTestAxi;

/// <summary>
/// Parses the console output of Microsoft Testing Platform runs (as printed by <c>dotnet test</c>).
/// Pure function: text in, <see cref="RunResult"/> out.
/// </summary>
internal static partial class MtpParser
{
    private const int MaxMessageLines = 3;
    private const int MaxMessageChars = 300;

    // "failed Ns.Class.Method(x: 1) (17ms)"
    [GeneratedRegex(@"^failed (?<name>.+) \((?<dur>(?:\d+(?:\.\d+)?\s?(?:h|m|s|ms|µs)\s?)+)\)\s*$")]
    private static partial Regex FailedLine();

    [GeneratedRegex(@"^\s+at .+ in (?<path>.+):(?:line )?(?<line>\d+)\s*$")]
    private static partial Regex StackFrame();

    [GeneratedRegex(@"^\s+from (?<asm>.+?) \(")]
    private static partial Regex FromLine();

    [GeneratedRegex(@"^Test run summary: (?<outcome>.+?)!?\s*$")]
    private static partial Regex SummaryHeader();

    [GeneratedRegex(@"^\s+(?<key>total|failed|succeeded|skipped|duration):\s*(?<value>.+?)\s*$")]
    private static partial Regex SummaryField();

    [GeneratedRegex(@"non-success exit code: (?<code>-?\d+)")]
    private static partial Regex ExitCodeLine();

    // "/path/T.cs(6,44): error CS0246: message [/path/proj.csproj]"
    [GeneratedRegex(@"^(?<file>.+?)\((?<line>\d+),\d+\): error (?<code>[A-Za-z]+\d+): (?<msg>.*?)(?: \[[^\]]+\])?\s*$")]
    private static partial Regex CompilerError();

    // "error MSB1009: ..." / "error NETSDK1045: ..." without a source location
    [GeneratedRegex(@"^(?:.*?: )?error (?<code>[A-Za-z]+\d+): (?<msg>.*?)(?: \[[^\]]+\])?\s*$")]
    private static partial Regex GenericError();

    public static RunResult Parse(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var result = new RunResult();
        var summaries = 0;
        var outcomes = new List<string>();
        var seenErrors = new HashSet<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            var failed = FailedLine().Match(line);
            if (failed.Success)
            {
                result.Failures.Add(ReadFailure(lines, ref i, failed.Groups["name"].Value));
                continue;
            }

            var header = SummaryHeader().Match(line);
            if (header.Success)
            {
                summaries++;
                outcomes.Add(header.Groups["outcome"].Value);
                while (i + 1 < lines.Length && SummaryField().Match(lines[i + 1]) is { Success: true } f)
                {
                    ApplySummaryField(result, f.Groups["key"].Value, f.Groups["value"].Value);
                    i++;
                }
                continue;
            }

            var exit = ExitCodeLine().Match(line);
            if (exit.Success)
            {
                result.ExitCode = int.Parse(exit.Groups["code"].Value);
                continue;
            }

            var err = CompilerError().Match(line);
            if (err.Success)
            {
                var issue = new BuildIssue(err.Groups["file"].Value, int.Parse(err.Groups["line"].Value),
                    err.Groups["code"].Value, err.Groups["msg"].Value);
                if (seenErrors.Add($"{issue.File}:{issue.Line}:{issue.Code}"))
                {
                    result.BuildErrors.Add(issue);
                }
            }
            else if (summaries == 0 && !line.StartsWith(' ') && GenericError().Match(line) is { Success: true } g)
            {
                var issue = new BuildIssue("", 0, g.Groups["code"].Value, g.Groups["msg"].Value);
                if (seenErrors.Add($"{issue.Code}:{issue.Message}"))
                {
                    result.BuildErrors.Add(issue);
                }
            }
        }

        result.Status = Classify(result, summaries, outcomes);
        return result;
    }

    private static RunStatus Classify(RunResult result, int summaries, List<string> outcomes)
    {
        if (summaries == 0)
        {
            return result.BuildErrors.Count > 0 ? RunStatus.BuildError : RunStatus.NoSummary;
        }

        if (outcomes.Any(o => o.StartsWith("Failed", StringComparison.OrdinalIgnoreCase)) || result.Failed > 0
            || result.Failures.Count > 0)
        {
            return RunStatus.Fail;
        }

        if (result.Total == 0 || outcomes.Any(o => o.Contains("Zero tests", StringComparison.OrdinalIgnoreCase)))
        {
            return RunStatus.NoTests;
        }

        return RunStatus.Pass;
    }

    private static TestFailure ReadFailure(string[] lines, ref int i, string name)
    {
        var message = new List<string>();
        string? location = null;
        string? assembly = null;

        while (i + 1 < lines.Length && lines[i + 1].StartsWith("  ", StringComparison.Ordinal)
               && !FailedLine().IsMatch(lines[i + 1]))
        {
            i++;
            var line = lines[i];

            if (FromLine().Match(line) is { Success: true } from)
            {
                assembly = Path.GetFileName(from.Groups["asm"].Value.Replace('\\', '/'));
                continue;
            }

            if (StackFrame().Match(line) is { Success: true } frame)
            {
                location ??= $"{Path.GetFileName(frame.Groups["path"].Value.Replace('\\', '/'))}:{frame.Groups["line"].Value}";
                continue;
            }

            if (line.TrimStart().StartsWith("at ", StringComparison.Ordinal) && line.StartsWith("    ", StringComparison.Ordinal))
            {
                continue; // stack frame without source info
            }

            if (message.Count < MaxMessageLines && line.Trim().Length > 0)
            {
                message.Add(line.Trim());
            }
        }

        var text = string.Join(" | ", message);
        if (text.Length > MaxMessageChars)
        {
            text = text[..MaxMessageChars] + "...";
        }

        return new TestFailure(name, text, location, assembly);
    }

    private static void ApplySummaryField(RunResult result, string key, string value)
    {
        switch (key)
        {
            case "total": result.Total += ToInt(value); break;
            case "failed": result.Failed += ToInt(value); break;
            case "succeeded": result.Passed += ToInt(value); break;
            case "skipped": result.Skipped += ToInt(value); break;
            case "duration": result.Duration = value; break;
        }
    }

    private static int ToInt(string value) => int.TryParse(value, out var n) ? n : 0;
}
