# 03 - End-to-end tests for TRX and fallback

Owns: tests/DotnetTestAxi.EndToEndTests/**
Docs: none

## Goal

End-to-end tests prove the TRX path, the fallback path and mixed solutions against the real sample projects.

## Context

Step 2 behavior is the subject. Assert output by decoding with ToonDecoder, not string matching. Samples: MSTest and TUnit bundle TrxReport, xUnit v3 uses --report-xunit-trx, NUnit sample references the package; a fallback case needs a project without it (copy or build the NUnit sample with the reference removed, or the VSTest/Other sample); Mixed.slnx mixes them.

## Work

1. Add e2e cases: MSTest and TUnit pass and fail print trx: in the same run folder as log: (`<name>.log` and `<name>.trx`, under a date folder) and the TRX exists and parses.
2. xUnit v3 and NUnit with the package: trx: printed, counts equal the console. NUnit without the package: no trx: field, missing-package warning with the dotnet add command, counts as before.
3. Mixed.slnx with a filter: trx per project that has one. A multi-project solution without a filter prints no trx: (one report file name would be shared).
4. Forced count mismatch yields NoSummary (use a hand-edited copy of a captured TRX or a test seam from step 2).
5. Build error and crash cases still report their statuses with and without the extension.

## Done when

dotnet test --solution DotnetTestAxi.slnx passes including the new end-to-end cases.
