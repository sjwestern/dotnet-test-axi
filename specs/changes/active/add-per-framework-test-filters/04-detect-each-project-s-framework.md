# 04 - Detect each project's framework

Owns: src/DotnetTestAxi/Frameworks/FrameworkDetector.cs, src/DotnetTestAxi/Config.cs, tests/DotnetTestAxi.Tests/FrameworkDetectorTests.cs
Docs: none

## Goal

Identify which test framework a project uses so the right adapter is chosen.

## Context

ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and. Run dotnet msbuild <project> -getItem:PackageReference (about 0.5 s) and match package ids against the adapters (xunit.v3*, MSTest*, NUnit, TUnit*). Only called when a filter is given. Config override: .dotnet-test-axi.json gains frameworks, a map from a project path (relative to the config) to xunit3, mstest, nunit or tunit. An undetectable project yields an UnknownFramework result naming the project, with help to set frameworks or pass native options after --.

## Work

1. Add src/DotnetTestAxi/Frameworks/FrameworkDetector.cs returning (adapter or unknown) per project, honoring the override first.
2. Extend Config.cs with the frameworks map and its parsing.
3. Run detections in parallel (they are independent and short).
4. Add tests/DotnetTestAxi.Tests/FrameworkDetectorTests.cs: package-id matching as a pure function, override precedence, unknown result, and one test that shells out to dotnet msbuild against a sample project.

## Done when

FrameworkDetectorTests pass and detection of each sample project returns the expected adapter.
