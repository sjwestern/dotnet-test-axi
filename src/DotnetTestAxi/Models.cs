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
    Crashed,
}

internal sealed record TestFailure(string Name, string Message, string? Location, string? Assembly, string? Project = null);

/// <summary>One row of the projects table of a multi-project run.</summary>
internal sealed record ProjectRow(string Name, string Framework, RunStatus Status, int Total, int Failed);

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

    /// <summary>Per-project rows; filled only for multi-project runs.</summary>
    public List<ProjectRow> Projects { get; } = [];

    /// <summary>Per-project log paths of a multi-project run.</summary>
    public List<string> Logs { get; } = [];

    /// <summary>TRX files that were produced and read, one per project that has one.</summary>
    public List<string> Trx { get; } = [];

    /// <summary>Projects that need the Microsoft.Testing.Extensions.TrxReport package for TRX output and lack it.</summary>
    public List<string> TrxMissing { get; } = [];

    /// <summary>Projects dotnet reported as using the VSTest runner when Microsoft Testing Platform is required.</summary>
    public List<string> VSTestProjects { get; } = [];

    /// <summary>Command line options the test application rejected (for example a filter it does not support).</summary>
    public List<string> RejectedOptions { get; } = [];

    /// <summary>Projects whose framework could not be detected (unknown-framework status).</summary>
    public List<string> FrameworkProjects { get; } = [];

    /// <summary>Help lines specific to the status, shown ahead of the generic ones.</summary>
    public List<string> StatusHelp { get; } = [];

    /// <summary>Set when a test host process died without finishing the run.</summary>
    public CrashInfo? Crash { get; set; }
}
