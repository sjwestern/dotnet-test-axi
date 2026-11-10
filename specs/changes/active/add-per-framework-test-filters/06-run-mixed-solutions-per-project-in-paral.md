# 06 - Run mixed solutions per project in parallel and merge

Owns: src/DotnetTestAxi/Runs/, src/DotnetTestAxi/Models.cs, src/DotnetTestAxi/Program.cs, src/DotnetTestAxi/Output.cs, tests/DotnetTestAxi.Tests/ResultMergerTests.cs
Docs: none

## Goal

Run a mixed-framework solution by building once and running each project with its own filter in parallel, then merge the results.

## Context

ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and. Build once with dotnet build --solution, then dotnet test --project <p> --no-build per project with that project's adapter arguments. Parallelism default max(1, ProcessorCount / 2) capped at 8, overridable with --parallel <n>. A project where the filter matched nothing is normal: merged no-tests only when nothing ran in any project. Any crash, failure or build error wins in that order of severity (build-error, crashed, fail, then pass). Output adds projects[N]{name,framework,status,total,failed} and a project column on failure rows, only for multi-project runs. Each project keeps its own log; the merged result lists them.

## Work

1. Add src/DotnetTestAxi/Runs/MixedRun.cs (build, parallel runs with a SemaphoreSlim, per-project logs).
2. Add src/DotnetTestAxi/Runs/ResultMerger.cs summing counts and combining statuses.
3. Extend RunResult/Output with the projects table and project column.
4. Add --parallel to Args and help.
5. Add tests/DotnetTestAxi.Tests/ResultMergerTests.cs for status precedence, no-tests handling and count sums.

## Done when

ResultMergerTests pass. The mixed sample solution (added in the next step) runs with --class and returns merged counts.
