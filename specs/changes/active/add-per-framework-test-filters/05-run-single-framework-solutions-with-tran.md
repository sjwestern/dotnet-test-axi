# 05 - Run single-framework solutions with translated flags

Owns: src/DotnetTestAxi/Program.cs, src/DotnetTestAxi/Args.cs, src/DotnetTestAxi/TestRunner.cs, src/DotnetTestAxi/Output.cs, src/DotnetTestAxi/Help.cs, tests/DotnetTestAxi.Tests/RunPlanTests.cs
Docs: none

## Goal

Apply a filter to a project or a single-framework solution by running once with the adapter's translated arguments.

## Context

ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and. No filter keeps today's behaviour exactly (no detection, no cost). With a filter: a project target detects its framework and uses the adapter; a solution target is expanded, every project detected, and when all share one adapter it runs once as dotnet test --solution with that adapter's arguments. A mixed solution is handled by the next step; until then it returns a status saying mixed frameworks need per-project runs. invalid-arguments help changes to say the options came from -- passthrough.

## Work

1. Add a RunPlan in Program/TestRunner: choose strategy (unfiltered, single-framework, mixed) from target, filter and detection.
2. Replace the hard-coded --filter-* mapping in TestRunner.BuildArguments with adapter output.
3. Add a status and output for unknown or mixed-unsupported cases with help lines.
4. Add --help text for the filter flags and their syntax.
5. Add tests/DotnetTestAxi.Tests/RunPlanTests.cs for strategy selection (pure function over project-framework pairs).

## Done when

RunPlanTests pass. dotnet-test-axi on each sample project with --class works. The existing xUnit end-to-end tests still pass and MSTest, NUnit and TUnit --class no longer report invalid-arguments.
