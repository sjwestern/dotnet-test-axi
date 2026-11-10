# 01 - Parse and validate the filter flags

Owns: src/DotnetTestAxi/Filtering/, tests/DotnetTestAxi.Tests/TestFilterTests.cs
Docs: none

## Goal

Parse --class, --method and --namespace into a TestFilter value and reject values the xUnit v3 rules do not allow, before any test run starts.

## Context

ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and. A value is a fully qualified name; * is allowed only at the start or end. A method must be qualified (Ns.C.M) or start with * (*Fails). Violations are usage errors (VALIDATION_ERROR, exit 2) with a help line showing a valid form. Later steps consume TestFilter, so keep it a small immutable record with Namespace, Class and Method patterns (each optional).

## Work

1. Add src/DotnetTestAxi/Filtering/TestFilter.cs: record TestFilter(string? Namespace, string? Class, string? Method) with IsEmpty, and a Pattern value type splitting a value into Text, LeadingWildcard, TrailingWildcard.
2. Add TestFilter.TryCreate(Args, out TestFilter, out string error) enforcing the rules above.
3. Make Args produce the filter and surface the error through the existing Args.Error path with valid-form help.
4. Add tests/DotnetTestAxi.Tests/TestFilterTests.cs covering qualified, wildcard-at-ends, middle-wildcard rejection, unqualified method rejection, empty filter.

## Done when

dotnet test --project tests/DotnetTestAxi.Tests passes with the new tests, and dotnet-test-axi x --method Fails prints error, code VALIDATION_ERROR and exits 2.
