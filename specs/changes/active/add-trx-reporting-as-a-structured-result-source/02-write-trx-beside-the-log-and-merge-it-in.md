# 02 - Write TRX beside the log and merge it into results

Owns: src/DotnetTestAxi/TestRunner.cs, src/DotnetTestAxi/Program.cs, src/DotnetTestAxi/Models.cs, src/DotnetTestAxi/Output.cs, src/DotnetTestAxi/Runs/**, src/DotnetTestAxi/Frameworks/FrameworkDetector.cs, tests/DotnetTestAxi.Tests/OutputTests.cs, tests/DotnetTestAxi.Tests/ResultMergerTests.cs, tests/DotnetTestAxi.Tests/FrameworkDetectorTests.cs
Docs: none

## Goal

Runs write a TRX with the same stem as the log when the extension is present, merge it into the result, and print trx: next to log:.

## Context

Depends on TrxReader from step 1. Pass requires an MTP summary, a zero exit code and no failure signal; TRX alone never yields pass. TRX and console count mismatch gives NoSummary. Output field names stay stable; trx: is additive and only printed when a TRX exists. Output must be produced with Toon.Format and asserted via ToonDecoder. Hint (once per run, only when failures exist and the extension is absent): add Microsoft.Testing.Extensions.TrxReport for per-test detail. AGENTS.md AXI rules apply: help[] entries are complete commands.

## Work

1. Replace TestRunner.NewLogPath with a helper returning log and TRX paths sharing one stem in the same dir.
2. Detect TrxReport per project via package ids (FrameworkDetector); on lookup failure skip TRX.
3. BuildArguments adds --report-trx, --report-trx-filename <stem>.trx and --results-directory <dir> only when detected. Native passthrough args still go last.
4. Program: after the run, parse the console log first, then overlay TRX counts and failures when the file exists; apply the mismatch rule.
5. MixedRun and ResultMerger: carry per-project TRX paths (RunResult.Trx list parallel to Logs) and merge.
6. Output: add trx: (single) or per-project trx list (multi), only when present; add the once-per-run hint and a help line to read failure detail from the TRX when failures are truncated.
7. Unit tests in OutputTests, ResultMergerTests, FrameworkDetectorTests, plus tests for the argument builder and the mismatch rule.

## Done when

dotnet test --solution DotnetTestAxi.slnx passes; running dotnet-test-axi on the MSTest sample prints log: and trx: with the same stem and an existing file; on the xUnit v3 sample there is no trx: and output is unchanged except the hint on failures.
