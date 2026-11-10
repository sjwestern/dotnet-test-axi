namespace DotnetTestAxi;

internal enum RunStatus
{
    Pass,
    Fail,
    BuildError,
    NoTests,
    NoSummary,
}

internal sealed record TestFailure(string Name, string Message, string? Location, string? Assembly);

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
}
