using System.Text.Json.Nodes;
using Toon.Format;

namespace DotnetTestAxi;

/// <summary>
/// Builds AXI-style output (https://github.com/kunchenguid/axi): plain objects converted to TOON at the boundary.
/// All output goes to stdout. Errors use the same structure with error, code and help.
/// </summary>
internal static class Output
{
    internal const int DefaultLimit = 10;
    internal const int MessageLimit = 300;
    internal const string Description = "Run dotnet test and print a compact summary of the result";
    internal const string ValidationError = "VALIDATION_ERROR";
    internal const string RuntimeError = "RUNTIME_ERROR";

    public static string Render(JsonObject output) => ToonEncoder.Encode(output) + "\n";

    public static string Error(string message, string code, params string[] help)
    {
        var o = new JsonObject { ["error"] = message, ["code"] = code };
        if (help.Length > 0)
        {
            o["help"] = Strings(help);
        }

        return Render(o);
    }

    public static string Run(RunResult r, string? logPath, string? target, bool full)
    {
        var o = new JsonObject { ["status"] = StatusName(r.Status) };
        var help = new List<string>();

        if (r.Status is RunStatus.UnsupportedRunner)
        {
            o["reason"] = "dotnet test rejected this target: dotnet-test-axi runs Microsoft Testing Platform projects only, and this one uses the VSTest runner";
            if (r.VSTestProjects.Count > 0 && r.Projects.Count == 0)
            {
                o["projects"] = Strings(r.VSTestProjects.ToArray());
            }

            help.Add("Run `dotnet test <path>` directly for VSTest projects");
            help.Add("Migrate to Microsoft Testing Platform: https://aka.ms/dotnet-test/mtp");
        }
        else if (r.Status is RunStatus.InvalidArguments)
        {
            o["reason"] = "the test application rejected the command line options";
            if (r.RejectedOptions.Count > 0)
            {
                o["options"] = Strings(r.RejectedOptions.ToArray());
            }

            help.Add("These options came from `--` passthrough or a framework that does not accept them; --class, --method and --namespace are translated for you");
            help.Add("Fix or remove the options after `--`, then rerun");
        }
        else if (r.Status is RunStatus.UnknownFramework)
        {
            o["reason"] = "the test framework of some projects could not be detected, so the filter cannot be translated";
            o["projects"] = Strings(r.FrameworkProjects.ToArray());
            help.AddRange(r.StatusHelp);
        }
        else if (r.Status is RunStatus.Crashed)
        {
            o["reason"] = "the test host process died before the run finished, so results are incomplete";
            if (r.Crash!.Module.Length > 0)
            {
                o["module"] = r.Crash.Module;
            }

            if (r.Crash.ExitCode is { } crashExit)
            {
                o["host_exit"] = crashExit;
            }

            if (r.Crash.Detail.Length > 0)
            {
                o["detail"] = r.Crash.Detail;
            }

            o["completed"] = r.Passed + r.Failed + r.Skipped;
            help.Add("Run the suspect class or test on its own to find the one that crashes the host");
            if (logPath is not null)
            {
                help.Add($"Read {logPath} for the crash output");
            }
        }
        else if (r.Status is RunStatus.BuildError)
        {
            var shown = full ? r.BuildErrors : r.BuildErrors.Take(DefaultLimit).ToList();
            o["errors"] = Rows(shown.Select(e => new JsonObject
            {
                ["file"] = e.File, ["line"] = e.Line, ["code"] = e.Code, ["message"] = e.Message,
            }));
            Count(o, shown.Count, r.BuildErrors.Count, "errors");
            help.Add("Fix the compile errors and rerun");
            if (shown.Count < r.BuildErrors.Count && logPath is not null)
            {
                help.Add($"Run `dotnet-test-axi --from-log {logPath} --full` for all {r.BuildErrors.Count} errors");
            }
        }
        else
        {
            o["total"] = r.Total;
            o["passed"] = r.Passed;
            o["failed"] = r.Failed;
            o["skipped"] = r.Skipped;
            if (r.Duration is not null)
            {
                o["duration"] = r.Duration;
            }

            var truncatedText = false;
            if (r.Failures.Count > 0)
            {
                var shown = full ? r.Failures : r.Failures.Take(DefaultLimit).ToList();
                o["failures"] = Rows(shown.Select(f =>
                {
                    var (message, cut) = full ? (f.Message, false) : Truncate(f.Message);
                    truncatedText |= cut;
                    var row = new JsonObject();
                    if (f.Project is not null) { row["project"] = f.Project; }
                    row["name"] = f.Name;
                    row["message"] = message;
                    row["at"] = f.Location ?? "";
                    return row;
                }));
                Count(o, shown.Count, r.Failures.Count, "failures");
            }
            else if (r.Status is RunStatus.Pass)
            {
                o["failures"] = 0;
            }

            if (r.ExitCode is { } code)
            {
                o["exit"] = code;
            }

            help.AddRange(FailureHelp(r, logPath, target, full, truncatedText));
        }

        if (r.Projects.Count > 0)
        {
            o["projects"] = Rows(r.Projects.Select(p => new JsonObject
            {
                ["name"] = p.Name, ["framework"] = p.Framework, ["status"] = StatusName(p.Status), ["total"] = p.Total, ["failed"] = p.Failed,
            }));
        }

        if (logPath is not null)
        {
            o["log"] = logPath;
        }
        else if (r.Logs.Count > 0)
        {
            o["logs"] = Strings(r.Logs.ToArray());
            help.Add("Read the logs listed above for each project's full output");
        }

        if (help.Count > 0)
        {
            o["help"] = Strings(help.ToArray());
        }

        return Render(o);
    }

    public static string Home(Config? config, string binPath)
    {
        var o = new JsonObject
        {
            ["bin"] = CollapseHome(binPath),
            ["description"] = Description,
            ["config"] = config?.Path is { } p ? CollapseHome(p) : "none",
        };
        o["targets"] = config is { Targets.Count: > 0 }
            ? Rows(config.Targets.Select(t => new JsonObject { ["name"] = t.Key, ["path"] = t.Value }))
            : "none defined";
        o["help"] = Strings(
            "Run `dotnet-test-axi <target>` where target is a name above or a path to a project, solution or directory",
            "Run `dotnet-test-axi --help` for all options");
        return Render(o);
    }

    public static string UnknownTarget(string target, Config? config)
    {
        var known = config is { Targets.Count: > 0 } ? $"known targets: {string.Join(", ", config.Targets.Keys)}" : "no targets are defined";
        return Error($"target '{target}' is not a known name or an existing path ({known})", ValidationError,
            "Run `dotnet-test-axi` to list targets", "Pass a path to a project, solution or directory");
    }

    public static string StatusName(RunStatus s) => s switch
    {
        RunStatus.Pass => "pass",
        RunStatus.Fail => "fail",
        RunStatus.BuildError => "build-error",
        RunStatus.NoTests => "no-tests",
        RunStatus.UnsupportedRunner => "unsupported-runner",
        RunStatus.InvalidArguments => "invalid-arguments",
        RunStatus.UnknownFramework => "unknown-framework",
        RunStatus.Crashed => "crashed",
        _ => "no-summary",
    };

    private static IEnumerable<string> FailureHelp(RunResult r, string? logPath, string? target, bool full, bool truncatedText)
    {
        switch (r.Status)
        {
            case RunStatus.Fail:
                if (r.Failures.Select(f => ClassOf(f.Name)).FirstOrDefault(c => c is not null) is { } cls && target is not null)
                {
                    yield return $"Run `dotnet-test-axi {target} --class \"{cls}\"` to rerun one failing class";
                }

                if (!full && (r.Failures.Count > DefaultLimit || truncatedText) && logPath is not null)
                {
                    yield return $"Run `dotnet-test-axi --from-log {logPath} --full` for all failures and complete messages";
                }

                if (logPath is not null)
                {
                    yield return $"Read {logPath} for the full output instead of rerunning the suite";
                }

                break;
            case RunStatus.NoTests:
                yield return "No tests ran: check the --class, --method and --namespace filters and the target";
                break;
            case RunStatus.NoSummary:
                yield return "No test summary was printed, so the run may have crashed or the project does not use Microsoft Testing Platform"
                             + (logPath is null ? "" : $"; read {logPath}");
                break;
        }
    }

    private static void Count(JsonObject o, int shown, int total, string noun)
    {
        if (shown < total)
        {
            o["count"] = $"{shown} of {total} {noun}";
        }
    }

    private static (string Text, bool Cut) Truncate(string text) =>
        text.Length <= MessageLimit ? (text, false) : ($"{text[..MessageLimit]}... (truncated, {text.Length} chars total)", true);

    private static string? ClassOf(string testName)
    {
        var paren = testName.IndexOf('(');
        var bare = paren >= 0 ? testName[..paren] : testName;
        var dot = bare.LastIndexOf('.');
        return dot > 0 ? bare[..dot] : null;
    }

    internal static string CollapseHome(string path)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return home.Length > 0 && path.StartsWith(home, StringComparison.Ordinal) ? "~" + path[home.Length..] : path;
    }

    private static JsonArray Rows(IEnumerable<JsonObject> rows) => new(rows.Cast<JsonNode?>().ToArray());

    private static JsonArray Strings(params string[] values) => new(values.Select(v => (JsonNode?)JsonValue.Create(v)).ToArray());
}
