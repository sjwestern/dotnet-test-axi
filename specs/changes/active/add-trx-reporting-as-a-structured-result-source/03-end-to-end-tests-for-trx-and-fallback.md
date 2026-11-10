# 03 - End-to-end tests for TRX and fallback

Owns: tests/DotnetTestAxi.EndToEndTests/**
Docs: none

## Goal

End-to-end tests prove the TRX path, the fallback path and mixed solutions against the real sample projects.

## Context

Step 2 behavior is the subject. Assert output by decoding with ToonDecoder, not string matching. Samples: MSTest and TUnit have TrxReport, xUnit v3 and NUnit do not; Mixed.slnx mixes them.

## Work

1. Add e2e cases: MSTest and TUnit pass and fail print trx: with the same stem as log: and the TRX exists and parses.
2. xUnit v3 and NUnit: no trx: field, hint present on failure only, counts as before.
3. Mixed.slnx: trx per project that has one.
4. Forced count mismatch yields NoSummary (use a hand-edited copy of a captured TRX or a test seam from step 2).
5. Build error and crash cases still report their statuses with and without the extension.

## Done when

dotnet test --solution DotnetTestAxi.slnx passes including the new end-to-end cases.
