namespace DotnetTestAxi;

internal enum RunStatus
{
    Pass,
    Fail,
    BuildError,
    NoTests,
    NoSummary,
    UnsupportedRunner,
    InvalidArguments,
    Crashed,
}

internal sealed record TestFailure(string Name, string Message, string? Location, string? Assembly);

internal sealed record CrashInfo(string Module, int? ExitCode, string Detail);

internal sealed record BuildIssue(string File, int Line, string Code, string Message);

internal sealed class RunResult
{
    public RunStatus Status { get; set; }
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public string? Duration { get; set; }
    public int? ExitCode { get; set; }
    public List<TestFailure> Failures { get; } = [];
    public List<BuildIssue> BuildErrors { get; } = [];

    /// <summary>Projects dotnet reported as using the VSTest runner when Microsoft Testing Platform is required.</summary>
    public List<string> VSTestProjects { get; } = [];

    /// <summary>Command line options the test application rejected (for example a filter it does not support).</summary>
    public List<string> RejectedOptions { get; } = [];

    /// <summary>Set when a test host process died without finishing the run.</summary>
    public CrashInfo? Crash { get; set; }
}
