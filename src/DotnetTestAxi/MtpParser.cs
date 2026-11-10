using System.Text.RegularExpressions;

namespace DotnetTestAxi;

/// <summary>
/// Parses the console output of Microsoft Testing Platform runs (as printed by <c>dotnet test</c>).
/// Pure function: text in, <see cref="RunResult"/> out.
/// </summary>
internal static partial class MtpParser
{
    private const int MaxMessageLines = 40;

    // "failed Ns.Class.Method(x: 1) (17ms)"
    [GeneratedRegex(@"^failed (?<name>.+) \((?<dur>(?:\d+(?:\.\d+)?\s?(?:h|m|s|ms|µs)\s?)+)\)\s*$")]
    private static partial Regex FailedLine();

    [GeneratedRegex(@"^\s+\d+\)\s+at ")]
    private static partial Regex NumberedFrame();

    [GeneratedRegex(@"^\s+(?:\d+\)\s+)?at .+ in (?<path>.+):(?:line )?(?<line>\d+)\s*$")]
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

    // dotnet test (MTP mode) rejecting VSTest projects, and VSTest mode rejecting the MTP-only --project/--solution flags.
    [GeneratedRegex(@"All projects must use that test runner")]
    private static partial Regex MtpRequired();

    [GeneratedRegex(@"^Switch: --(project|solution)\s*$")]
    private static partial Regex MtpOnlySwitch();

    // "<dll> (net10.0|x64) failed (337ms)" is a module that died; a module with failing tests says "failed with N error(s)".
    [GeneratedRegex(@"^(?<dll>.+?) \((?<tfm>[^)]*)\) failed \(\d")]
    private static partial Regex CrashedModule();

    // Any per-module result line: "<dll> (net10.0|x64) passed|failed|Zero tests ran (1s)".
    [GeneratedRegex(@"^(?<dll>.+?\.dll) \((?<tfm>[^)]*)\) (?:passed|failed|skipped|Zero tests ran)")]
    private static partial Regex ModuleLine();

    [GeneratedRegex(@"^Exit code: (?<code>-?\d+)\s*$")]
    private static partial Regex ModuleExitCode();

    [GeneratedRegex(@"Unknown option '(?<opt>[^']+)'")]
    private static partial Regex UnknownOption();

    [GeneratedRegex(@"^Handshake failures:")]
    private static partial Regex HandshakeFailures();

    [GeneratedRegex(@"without a corresponding test session end")]
    private static partial Regex SessionEndMissing();

    public static RunResult Parse(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var result = new RunResult();
        var unsupported = false;
        var invalidArguments = false;
        var lastModuleIndex = -1;
        var lastModuleName = "";
        var summaries = 0;
        var outcomes = new List<string>();
        var seenErrors = new HashSet<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (MtpRequired().IsMatch(line))
            {
                unsupported = true;
                while (i + 1 < lines.Length && !lines[i + 1].StartsWith("The following test projects", StringComparison.Ordinal)) { i++; }
                i++;
                while (i + 1 < lines.Length && lines[i + 1].Trim().Length > 0)
                {
                    i++;
                    result.VSTestProjects.Add(lines[i].Trim());
                }

                continue;
            }

            if (ModuleLine().Match(line) is { Success: true } module)
            {
                lastModuleIndex = i;
                lastModuleName = Path.GetFileName(module.Groups["dll"].Value.Replace('\\', '/'));
            }

            if (CrashedModule().Match(line) is { Success: true } crashed)
            {
                result.Crash = ReadCrash(lines, ref i, Path.GetFileName(crashed.Groups["dll"].Value.Replace('\\', '/')));
                continue;
            }

            if (UnknownOption().Match(line) is { Success: true } unknown)
            {
                if (!result.RejectedOptions.Contains(unknown.Groups["opt"].Value))
                {
                    result.RejectedOptions.Add(unknown.Groups["opt"].Value);
                }

                invalidArguments = true;
                continue;
            }

            if (HandshakeFailures().IsMatch(line))
            {
                invalidArguments = true;
                continue;
            }

            if (SessionEndMissing().IsMatch(line))
            {
                if (result.Crash is null)
                {
                    var at = lastModuleIndex;
                    result.Crash = at >= 0
                        ? ReadCrash(lines, ref at, lastModuleName)
                        : new CrashInfo("", null, "the test host exited before the run finished");
                }

                continue;
            }

            if (MtpOnlySwitch().IsMatch(line))
            {
                unsupported = true; // VSTest mode does not know --project or --solution
                continue;
            }

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
                // A solution run lists one indented line per project (and blank lines) before the totals.
                while (i + 1 < lines.Length && (lines[i + 1].Length == 0 || char.IsWhiteSpace(lines[i + 1][0])))
                {
                    i++;
                    if (SummaryField().Match(lines[i]) is { Success: true } f)
                    {
                        ApplySummaryField(result, f.Groups["key"].Value, f.Groups["value"].Value);
                    }
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

        result.Status = unsupported && summaries == 0 ? RunStatus.UnsupportedRunner
            : invalidArguments ? RunStatus.InvalidArguments
            : result.Crash is not null ? RunStatus.Crashed
            : Classify(result, summaries, outcomes);
        if (result.Status == RunStatus.UnsupportedRunner)
        {
            result.BuildErrors.Clear(); // the MSBuild switch error is not a compile error
        }
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

    private static CrashInfo ReadCrash(string[] lines, ref int i, string module)
    {
        int? exit = null;
        var detail = new List<string>();
        while (i + 1 < lines.Length && lines[i + 1].Trim().Length > 0)
        {
            var next = lines[i + 1];
            if (ModuleExitCode().Match(next) is { Success: true } code)
            {
                exit = int.Parse(code.Groups["code"].Value);
            }
            else if (next.StartsWith("  ", StringComparison.Ordinal) && !next.TrimStart().StartsWith("at ", StringComparison.Ordinal))
            {
                detail.Add(next.Trim().Replace("Error output: ", "", StringComparison.Ordinal));
            }
            else
            {
                break;
            }

            i++;
        }

        return new CrashInfo(module, exit, string.Join(" | ", detail.Where(d => d.Length > 0).Take(3)));
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

            if (NumberedFrame().IsMatch(line) || line.TrimStart().StartsWith("--- ", StringComparison.Ordinal))
            {
                continue; // NUnit "1)    at ..." frames and framework banners such as "--- TUnit internals omitted ---"
            }

            if (line.TrimStart().StartsWith("TUnit.Engine.Exceptions.TestFailedException:", StringComparison.Ordinal))
            {
                continue; // repeats the message printed above it
            }

            if (message.Count < MaxMessageLines && line.Trim().Length > 0)
            {
                message.Add(line.Trim());
            }
        }

        return new TestFailure(name, string.Join(" | ", message), location, assembly);
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
