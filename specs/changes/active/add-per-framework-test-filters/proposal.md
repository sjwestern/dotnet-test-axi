# Translate --class, --method and --namespace to each test framework

## Why

Only xUnit v3 accepts --class, --method and --namespace (as --filter-class and friends). MSTest, NUnit and TUnit reject them, so the tool reports invalid-arguments, and a solution with mixed frameworks fails every non-xUnit module. Agents need one filter vocabulary that works on any .NET test project.

## Context

No repo docs existed before this plan. Read: README.md (framework support table), the sample projects and captured fixtures under tests/Samples and fixtures/. Applies: ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and (written with this plan). Measured on the samples: xUnit v3 takes --filter-class/method/namespace; MSTest and NUnit take --filter with VSTest syntax; TUnit takes --treenode-filter; a mixed solution run with one xUnit flag ran the xUnit project and failed the others with exit 5; dotnet msbuild -getItem:PackageReference takes about 0.5 s per project.

## What changes

- --class, --method and --namespace work on xUnit v3, MSTest, NUnit and TUnit projects with identical semantics (fully qualified names, * only at the start or end).
- The framework is detected per project from PackageReference items, with an optional frameworks override in .dotnet-test-axi.json.
- A solution whose test projects share a framework runs as one dotnet test --solution with the translated flags.
- A mixed solution builds once, then runs each project with --no-build and its own filter in parallel, capped by processor count (override with --parallel), and merges the results.
- Mixed runs print a projects table (name, framework, status, total, failed) and a project column on failure rows.
- A project whose framework cannot be detected gets a clear status naming it, and how to fix it.
- A filter value the rules do not allow is a usage error (exit 2) before anything runs.
- invalid-arguments remains for options the framework rejects that came from -- passthrough.

## Out of scope

- Classic VSTest projects (separate backlog item).
- Wildcards in the middle of a value and other globbing.
- Filtering by trait or category, and --filter-not-* flags.
- Class-qualified names for MSTest, NUnit and TUnit results.
- Frameworks beyond xUnit v3, MSTest, NUnit and TUnit.

## Approach

A TestFilter value (namespace, class, method patterns) is parsed and validated once. An IFrameworkAdapter per framework turns it into dotnet test arguments: xUnit v3 maps to --filter-class/method/namespace, MSTest and NUnit to a --filter FullyQualifiedName expression (a trailing dot on class names stops Ns.C matching Ns.COther), TUnit to a /*/ns/class/method tree filter. A FrameworkDetector reads PackageReference items through dotnet msbuild -getItem and matches package ids (xunit.v3*, MSTest*, NUnit, TUnit*). A SolutionReader lists test projects from .slnx, .sln and .slnf. The runner picks the strategy: no filter keeps today's single run; one framework runs once with translated flags; mixed builds once, then runs projects with --no-build in parallel (default max(1, ProcessorCount / 2), capped at 8) and a ResultMerger sums counts and combines statuses (no-tests only if nothing ran anywhere; any crash or failure wins). Alternatives considered: retry after a rejected flag (slow and hides errors); one run with all flags (frameworks reject unknown options); require the user to pass native filters (loses the single vocabulary).

## Steps

- 01-parse-and-validate-the-filter-flags.md - TestFilter value and validation
- 02-add-framework-adapters-that-translate-th.md - xUnit v3, MSTest, NUnit and TUnit adapters
- 03-read-test-projects-from-solution-files.md - project lists from slnx, sln and slnf
- 04-detect-each-project-s-framework.md - PackageReference detection and config override
- 05-run-single-framework-solutions-with-tran.md - filtered runs with translated flags
- 06-run-mixed-solutions-per-project-in-paral.md - build once, parallel per-project runs, merged output
- 07-add-sample-solutions-and-end-to-end-test.md - same-framework and mixed sample solutions with e2e tests
- 08-update-docs-for-per-framework-filters.md - README, architecture doc, ADR accepted

## Acceptance

- dotnet test --solution DotnetTestAxi.slnx passes, including new adapter, detector, merger and end-to-end tests.
- dotnet-test-axi <MSTest|NUnit|TUnit|Xunit3 sample> --class Ns.SampleTests --no-build reports the class tests, and --method Ns.SampleTests.Fails reports total 1.
- A single-framework sample solution with --class runs once and reports merged counts.
- A mixed sample solution with --class selects the right tests in every project and the projects table lists each one.
- --method Fails (unqualified) and a middle wildcard are rejected with exit 2 and a help line.
- docs-axi check and change-axi check pass.

## Docs impact

- README.md - update: framework table (flags now work on all four), filter syntax, --parallel, frameworks config.
- docs/architecture/test-filtering.md - create: how detection, adapters and mixed runs work.
- docs/decisions/0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and.md - create: the decision record.

## Risks and open questions

- MSBuild evaluation may miss frameworks pulled in only through a shared test project reference: the config override covers it.
- FullyQualifiedName~ is a contains match, so namespace filters can over-select on similar prefixes (Ns vs NsExtra): mitigated with a trailing dot, to be confirmed in tests.
- Parallel dotnet test runs on one machine can contend for databases or ports in some suites: --parallel 1 is the escape hatch.
- Detection runs dotnet msbuild once per project; large solutions pay roughly half a second each, only when a filter is given.
