# 01 - Add TrxReader with captured TRX fixtures

Owns: src/DotnetTestAxi/Trx/**, fixtures/*.trx, scripts/capture-fixtures.ps1, tests/DotnetTestAxi.Tests/TrxReaderTests.cs
Docs: none

## Goal

A runner-agnostic TrxReader turns TRX XML into counts and failures, proven against TRX captured from every sample framework.

## Context

Parser changes are test-first against fixtures (AGENTS.md); capture with scripts/capture-fixtures.ps1, which must rewrite absolute paths to /work/.... MSTest and TUnit samples have TrxReport; xUnit v3 and NUnit samples do not, so capture their TRX by adding --report-trx only if their runners support it, otherwise cover them in step 3. Also verify here that FrameworkDetector's msbuild package-id read lists the transitive TrxReport reference, and record the finding in step 2's input (write it in the PR or session memory). Tier: none configured. Interface step 2 relies on: TrxReader.Read(string xml) returning total, passed, failed, skipped, duration and a list of TestFailure.

## Work

1. Extend scripts/capture-fixtures.ps1 to also save the TRX for the MSTest and TUnit pass and fail scenarios as fixtures/*.trx (paths rewritten to /work/...).
2. Add src/DotnetTestAxi/Trx/TrxReader.cs: parse UnitTestResult outcome, testName, duration, Output/ErrorInfo/Message and StackTrace using System.Xml.Linq; map outcomes to passed, failed, skipped. No MTP knowledge, tolerate missing elements, return an error result on invalid XML instead of throwing.
3. Parse the first stack frame into TestFailure.Location using the same shape MtpParser produces.
4. Add tests/DotnetTestAxi.Tests/TrxReaderTests.cs: pass, fail, skipped, empty, malformed XML.
5. Check whether the msbuild package-id read includes transitive packages; note the result.

## Done when

dotnet test --solution DotnetTestAxi.slnx passes with new TrxReaderTests over the captured fixtures; TrxReader counts equal the counts in the matching .log fixtures; the transitive-package finding is recorded.
