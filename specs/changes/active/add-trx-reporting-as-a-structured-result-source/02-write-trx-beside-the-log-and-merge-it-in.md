# 02 - Write TRX beside the log and merge it into results

Owns: src/DotnetTestAxi/TestRunner.cs, src/DotnetTestAxi/Program.cs, src/DotnetTestAxi/Models.cs, src/DotnetTestAxi/Output.cs, src/DotnetTestAxi/Runs/**, src/DotnetTestAxi/Frameworks/FrameworkDetector.cs, tests/DotnetTestAxi.Tests/OutputTests.cs, tests/DotnetTestAxi.Tests/ResultMergerTests.cs, tests/DotnetTestAxi.Tests/FrameworkDetectorTests.cs
Docs: none

## Goal

Runs write a TRX with the same stem as the log when the extension is present, merge it into the result, and print trx: next to log:.

## Context

Depends on TrxReader from step 1. Pass requires a success summary (from the console or from the TRX; not tied to Microsoft Testing Platform wording), a zero exit code and no failure signal. If both sources exist and their counts disagree, status is NoSummary. Output field names stay stable; trx: is additive and only printed when a TRX exists. Output must be produced with Toon.Format and asserted via ToonDecoder. Per-adapter TRX option: xUnit v3 `--report-xunit-trx` and `--report-xunit-trx-filename` (no package); MSTest, NUnit, TUnit `--report-trx` and `--report-trx-filename`. The package is needed everywhere except xUnit v3, but the MSTest and TUnit metapackages (`MSTest`, `TUnit`) depend on it, so referencing those is enough; a project using only MSTest.TestAdapter/MSTest.TestFramework or TUnit.Engine does not get it (verified from the nuspecs). Step 1 found the msbuild read lists direct references only, which is enough: each adapter lists the package ids that imply TRX support (`MSTest`, `TUnit`, `Microsoft.Testing.Extensions.TrxReport`; NUnit only the last) and TRX is used only when one is found. Otherwise warn as below. Warning (once per run, regardless of failures, only when the adapter needs the package and none of its implying ids is present): add Microsoft.Testing.Extensions.TrxReport, with the complete `dotnet add <project> package Microsoft.Testing.Extensions.TrxReport` command in help[]. TRX is preferred over console parsing for counts and failures whenever present. AGENTS.md AXI rules apply: help[] entries are complete commands.

## Work

1. Replace TestRunner.NewLogPath with a helper returning log and TRX paths sharing one stem in the same dir.
2. Give each IFrameworkAdapter its TRX option names and the package ids that imply TRX support (none needed for xUnit v3). Check package ids (FrameworkDetector); on lookup failure skip TRX.
3. BuildArguments adds the adapter's TRX option, its filename option (<stem>.trx) and --results-directory <dir> only when TRX is available. Native passthrough args still go last.
4. Program: after the run, parse the console log first, then overlay TRX counts and failures when the file exists; apply the mismatch rule.
5. MixedRun and ResultMerger: carry per-project TRX paths (RunResult.Trx list parallel to Logs) and merge.
6. Output: add trx: (single) or per-project trx list (multi), only when present; add the once-per-run missing-package warning with a complete dotnet add command, and a help line to read failure detail from the TRX when failures are truncated.
7. Unit tests in OutputTests, ResultMergerTests, FrameworkDetectorTests, plus tests for the argument builder and the mismatch rule.

## Done when

dotnet test --solution DotnetTestAxi.slnx passes; running dotnet-test-axi on the MSTest sample prints log: and trx: with the same stem and an existing file; on the xUnit v3 sample trx: is printed too; on NUnit without the package there is no trx: and a warning suggests adding the package.
