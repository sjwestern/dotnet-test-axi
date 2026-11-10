# 02 - Add framework adapters that translate the filter

Owns: src/DotnetTestAxi/Frameworks/Adapters/, tests/DotnetTestAxi.Tests/AdapterTests.cs
Docs: none

## Goal

Translate a TestFilter into the native dotnet test arguments of xUnit v3, MSTest, NUnit and TUnit.

## Context

ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and. Verified syntax: xUnit v3 --filter-class/--filter-method/--filter-namespace (wildcards native); MSTest and NUnit --filter with FullyQualifiedName (= exact, ~ contains; class uses a trailing dot so Ns.C does not match Ns.COther; namespace uses Ns.; combine with & and group alternatives with |); TUnit --treenode-filter /*/<ns>/<class>/<method> where the class pattern is split at its last dot. Leading or trailing * maps to ~ or to the tree wildcard.

## Work

1. Add src/DotnetTestAxi/Frameworks/Adapters/IFrameworkAdapter.cs: Id, Matches(IEnumerable<string> packageIds), BuildFilterArguments(TestFilter) returning the argument list.
2. Implement Xunit3Adapter, MSTestAdapter, NUnitAdapter (sharing a VSTestExpression helper) and TUnitAdapter.
3. Add a registry listing the adapters in a fixed order.
4. Add tests/DotnetTestAxi.Tests/AdapterTests.cs as a table: each adapter against class, method, namespace, wildcard and combined filters, asserting the exact argument lists.

## Done when

AdapterTests pass. Running the translated arguments by hand against each sample project (--no-build) selects the same tests as the table in the proposal (class 8, method 1, wildcard Th* 4).
