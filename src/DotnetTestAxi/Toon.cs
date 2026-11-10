using System.Text;

namespace DotnetTestAxi;

/// <summary>Formats results as compact TOON-style text.</summary>
internal static class Toon
{
    private const int DefaultLimit = 10;

    public static string Format(RunResult r, string? logPath, string? target, bool full)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"status: {StatusName(r.Status)}");

        if (r.Status is RunStatus.BuildError)
        {
            var errors = r.BuildErrors;
            var shown = full ? errors : errors.Take(DefaultLimit).ToList();
            sb.AppendLine($"errors[{errors.Count}]{{file,line,code,message}}:");
            foreach (var e in shown)
            {
                sb.AppendLine($"  {Cell(e.File)},{e.Line},{e.Code},{Cell(e.Message)}");
            }
            Remainder(sb, errors.Count, shown.Count);
        }
        else
        {
            sb.AppendLine($"total: {r.Total}");
            sb.AppendLine($"passed: {r.Passed}");
            sb.AppendLine($"failed: {r.Failed}");
            sb.AppendLine($"skipped: {r.Skipped}");
            if (r.Duration is not null)
            {
                sb.AppendLine($"duration: {r.Duration}");
            }

            if (r.Failures.Count > 0)
            {
                var shown = full ? r.Failures : r.Failures.Take(DefaultLimit).ToList();
                sb.AppendLine($"failures[{r.Failures.Count}]{{name,message,at}}:");
                foreach (var f in shown)
                {
                    sb.AppendLine($"  {Cell(f.Name)},{Cell(f.Message)},{Cell(f.Location ?? "")}");
                }
                Remainder(sb, r.Failures.Count, shown.Count);
            }
        }

        if (r.ExitCode is { } code)
        {
            sb.AppendLine($"exit: {code}");
        }

        if (logPath is not null)
        {
            sb.AppendLine($"log: {logPath}");
        }

        sb.AppendLine($"next: {Next(r, logPath, target, full)}");
        return sb.ToString();
    }

    public static string StatusName(RunStatus s) => s switch
    {
        RunStatus.Pass => "pass",
        RunStatus.Fail => "fail",
        RunStatus.BuildError => "build-error",
        RunStatus.NoTests => "no-tests",
        _ => "no-summary",
    };

    private static void Remainder(StringBuilder sb, int total, int shown)
    {
        if (total > shown)
        {
            sb.AppendLine($"more: {total - shown} not shown (use --full)");
        }
    }

    private static string Next(RunResult r, string? logPath, string? target, bool full)
    {
        var tgt = target is null ? "" : $" {target}";
        switch (r.Status)
        {
            case RunStatus.Pass:
                return "none";
            case RunStatus.Fail:
            {
                var cls = r.Failures.Select(f => ClassOf(f.Name)).FirstOrDefault(c => c is not null);
                var rerun = cls is null ? "" : $"rerun one class: dotnet-test-axi{tgt} --class \"{cls}\". ";
                var more = !full && r.Failures.Count > DefaultLimit ? "all failures: add --full. " : "";
                return $"{rerun}{more}full output: read the log, do not rerun the whole suite".Trim();
            }
            case RunStatus.BuildError:
                return "fix the compile errors and rerun; full build output is in the log";
            case RunStatus.NoTests:
                return "no tests matched; check the --class/--method/--namespace filter and target";
            default:
                return logPath is null
                    ? "no test summary found; the run may have crashed"
                    : "no test summary found; the run may have crashed, read the log";
        }
    }

    private static string? ClassOf(string testName)
    {
        var paren = testName.IndexOf('(');
        var bare = paren >= 0 ? testName[..paren] : testName;
        var dot = bare.LastIndexOf('.');
        return dot > 0 ? bare[..dot] : null;
    }

    /// <summary>Quotes a table cell only when it contains a delimiter or quote.</summary>
    internal static string Cell(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        return value.AsSpan().IndexOfAny(",\"\n") >= 0 ? $"\"{value.Replace("\"", "\\\"").Replace("\n", " ")}\"" : value;
    }
}
