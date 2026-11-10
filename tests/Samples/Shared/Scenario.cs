namespace Samples;

/// <summary>
/// Switches the sample tests between outcomes so one set of projects can produce pass, fail and crash output.
/// Set the SAMPLE_SCENARIO environment variable: pass (default), fail or crash.
/// </summary>
internal static class Scenario
{
    public static string Current => Environment.GetEnvironmentVariable("SAMPLE_SCENARIO") ?? "pass";

    public static bool Failing => Current == "fail";

    public static bool Crashing => Current == "crash";

    /// <summary>Throws only in the fail scenario.</summary>
    public static void FailIfRequested(string message)
    {
        if (Failing)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Kills the test host without a summary, only in the crash scenario.</summary>
    public static void CrashIfRequested()
    {
        if (Crashing)
        {
            Environment.FailFast("sample crash");
        }
    }
}
