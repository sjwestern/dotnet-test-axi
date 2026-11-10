---
title: Test filtering
summary: How filter flags are validated, detected per framework, translated by adapters and run (single, mixed)
---

# Test filtering

How `--class`, `--method` and `--namespace` reach each test framework. Decision: ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and.

## Flow

1. `Args` parses the flags and `TestFilter.TryCreate` validates them (`src/DotnetTestAxi/Filtering/`). Values are fully qualified names; `*` is allowed only at the start or end; a method is qualified (`Ns.C.M`) or starts with `*`. A violation is a usage error (`VALIDATION_ERROR`, exit 2) before anything runs.
2. With no filter nothing else happens: no detection, one `dotnet test` run as before.
3. With a filter, `TestRunner.ProjectsOf` lists the target's projects (`SolutionReader` reads `.slnx`, `.sln` and `.slnf`) and `FrameworkDetector` finds each project's framework in parallel.
4. `RunPlan.Choose` picks a strategy from the detections.

## Detection

`dotnet msbuild <project> -getItem:PackageReference` (about 0.5 s per project) and package ids matched by the adapters: `xunit.v3*`, `MSTest*`, `NUnit`, `TUnit*`. The `frameworks` map in `.dotnet-test-axi.json` (project path relative to the config, value `xunit3|mstest|nunit|tunit`) wins over detection. A project with no match is `unknown-framework`; the status names it and suggests the config key or native options after `--`.

## Adapters

`IFrameworkAdapter` (`src/DotnetTestAxi/Frameworks/Adapters/`) has `Id`, `Matches(packageIds)` and `BuildFilterArguments(TestFilter)`. `AdapterRegistry.All` lists them in a fixed order.

| Adapter | Native arguments |
|---|---|
| `xunit3` | `--filter-namespace`, `--filter-class`, `--filter-method` (wildcards native) |
| `mstest`, `nunit` | `--filter` with `FullyQualifiedName` terms joined by `&`: `=` for an exact method, `~` (contains) otherwise; a class or namespace without a trailing `*` gets a trailing dot so `Ns.C` does not match `Ns.COther` |
| `tunit` | `--treenode-filter /*/<namespace>/<class>/<method>`; a class or method value is split at its last dot |

Limit: `~` is a contains match, so a leading-wildcard-free namespace or class can still over-select on an unusual name. When two TUnit flags constrain the same segment, the most specific wins (method, then class, then namespace).

## Run strategies

| Strategy | When | What runs |
|---|---|---|
| Unfiltered | no filter | one `dotnet test` as before |
| Single | all projects share one adapter | one `dotnet test` with the adapter's arguments (`--solution` or `--project`) |
| Mixed | several adapters | `dotnet build` once, then `dotnet test --project <p> --no-build` per project with its own arguments, in parallel (`--parallel`, default half the processors, 1 to 8); `ResultMerger` combines them |
| Unknown | a project's framework is not found, or no projects | status `unknown-framework`, nothing runs |

`ResultMerger` sums counts. Status severity is build-error, crashed, unsupported-runner, invalid-arguments, no-summary, fail, pass, no-tests: `no-tests` only when nothing ran anywhere, because a filter matching nothing in one project is normal. Mixed output adds `projects[N]{name,framework,status,total,failed}`, a `project` column on failure rows and a `logs` list (one per project).

## Add a framework

1. Add an adapter and register it in `AdapterRegistry`.
2. Add rows to `AdapterTests` and a sample project (plus `Mixed.slnx` entry) under `tests/Samples`.
3. Add its native filter to `scripts/capture-fixtures.ps1`, regenerate fixtures and add end-to-end cases.
4. Update the README framework table.
