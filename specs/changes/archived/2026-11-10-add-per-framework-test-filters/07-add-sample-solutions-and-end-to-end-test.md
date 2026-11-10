# 07 - Add sample solutions and end-to-end tests for filters

Owns: tests/Samples/, tests/DotnetTestAxi.EndToEndTests/, scripts/capture-fixtures.ps1, fixtures/, DotnetTestAxi.slnx
Docs: none

## Goal

Prove the whole feature against real projects: single-framework and mixed sample solutions, and end-to-end tests.

## Context

Samples already exist per framework (tests/Samples). Add a second xUnit v3 sample (Xunit3.Other.Sample) so a same-framework solution has two projects, and two solution files under tests/Samples: SameFramework.slnx (both xUnit projects) and Mixed.slnx (xUnit, MSTest, NUnit, TUnit). They must not be part of DotnetTestAxi.slnx test runs unless green by default (they are, via SAMPLE_SCENARIO). Fixtures come from scripts/capture-fixtures.ps1; add the new cases there.

## Work

1. Add Xunit3.Other.Sample, SameFramework.slnx and Mixed.slnx.
2. Add end-to-end tests: --class, --method and --namespace on every framework sample; a same-framework solution with a filter (single dotnet test run); the mixed solution with a filter (per-project runs, merged output); unqualified --method and a middle wildcard rejected with exit 2; --parallel 1.
3. Extend capture-fixtures.ps1 with the filter cases and regenerate fixtures; review the diff.

## Done when

dotnet test --solution DotnetTestAxi.slnx passes including the new end-to-end tests, and dotnet-test-axi tests/Samples/Mixed.slnx --class Samples.Xunit3.SampleTests reports the expected merged result.
