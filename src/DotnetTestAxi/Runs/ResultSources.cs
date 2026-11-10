using DotnetTestAxi.Trx;

namespace DotnetTestAxi.Runs;

/// <summary>Combines the console parse of a run with its TRX report.</summary>
internal static class ResultSources
{
    /// <summary>
    /// Finishes a console-parsed result. A TRX, when it exists and is valid, supplies the failures and, if the
    /// console printed no summary, the counts. Passing always needs a success summary (console or TRX) and a zero exit code.
    /// Console and TRX counts that disagree make the status NoSummary: never claim success on conflicting evidence.
    /// </summary>
    public static void Finish(RunResult result, int exitCode, string? trxPath)
    {
        result.ExitCode ??= exitCode != 0 ? exitCode : null;

        if (trxPath is not null && File.Exists(trxPath))
        {
            var trx = TrxReader.Read(File.ReadAllText(trxPath));
            if (trx.IsValid)
            {
                result.Trx.Add(trxPath);
                Overlay(result, trx, exitCode, trxPath);
            }
        }

        if (exitCode != 0 && result.Status == RunStatus.Pass)
        {
            result.Status = RunStatus.NoSummary; // dotnet failed although a summary said pass: never claim success
        }
    }

    private static void Overlay(RunResult result, TrxResult trx, int exitCode, string trxPath)
    {
        switch (result.Status)
        {
            case RunStatus.Pass or RunStatus.Fail:
                if ((result.Total, result.Passed, result.Failed, result.Skipped) != (trx.Total, trx.Passed, trx.Failed, trx.Skipped))
                {
                    result.Status = RunStatus.NoSummary;
                    result.StatusHelp.Add($"The console summary ({result.Total} total, {result.Failed} failed) and the TRX report ({trx.Total} total, {trx.Failed} failed) disagree; read both the log and {trxPath}");
                    return;
                }

                Replace(result.Failures, trx.Failures);
                break;

            case RunStatus.NoSummary when trx.Total > 0:
                // No console summary (output format changed, or it was lost): the TRX is the summary.
                result.Total = trx.Total;
                result.Passed = trx.Passed;
                result.Failed = trx.Failed;
                result.Skipped = trx.Skipped;
                result.Duration ??= trx.Duration;
                Replace(result.Failures, trx.Failures);
                if (trx.Failed > 0)
                {
                    result.Status = RunStatus.Fail;
                }
                else if (exitCode == 0)
                {
                    result.Status = RunStatus.Pass;
                }

                break;
        }
    }

    private static void Replace(List<TestFailure> target, IReadOnlyList<TestFailure> source)
    {
        target.Clear();
        target.AddRange(source);
    }
}
