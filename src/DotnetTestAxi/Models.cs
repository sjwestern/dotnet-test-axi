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
    UnknownFramework,
    MixedFrameworks,
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

    /// <summary>Projects involved in an unknown-framework or mixed-frameworks status, as "name" or "name: framework".</summary>
    public List<string> FrameworkProjects { get; } = [];

    /// <summary>Help lines specific to the status, shown ahead of the generic ones.</summary>
    public List<string> StatusHelp { get; } = [];

    /// <summary>Set when a test host process died without finishing the run.</summary>
    public CrashInfo? Crash { get; set; }
}
