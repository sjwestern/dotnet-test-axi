# Add TRX reporting as a structured result source with console parsing as fallback

## Why

Run results come from regex-parsing console text, which breaks when a framework or runner changes its output and truncates failure detail. TRX is a stable schema with full messages, stack traces and per-test outcomes. Agents that want more than the summary need a file to open, and today the only one is the raw log.

## Context

Docs read: docs/architecture/test-filtering.md (run strategies single, mixed, unfiltered), ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and (no constraint on this change). Code read: TestRunner, Program, MtpParser, Models, FrameworkDetector, MixedRun. No active changes overlap. Verified in step 1: the MSTest and TUnit metapackages depend on Microsoft.Testing.Extensions.TrxReport (`--report-trx`; projects referencing only MSTest.TestAdapter or TUnit.Engine do not get it); xUnit v3 has its own `--report-xunit-trx` and needs no package (`--report-trx` is rejected there); NUnit needs a direct TrxReport package reference (the NUnit sample now has one). The msbuild PackageReference read lists direct references only, so transitive TrxReport is invisible to it.

## What changes

- When a project has Microsoft.Testing.Extensions.TrxReport, the run passes --report-trx, --report-trx-filename and --results-directory so the TRX is written beside the log. Each run gets its own folder, `<log dir>/<yyyy-MM-dd>/<name>-<HHmmss>/`, holding `<name>.log`, `<name>.trx` and anything else the runner writes (for example TUnit's HTML report), so a run, or a whole day, is removed by deleting a folder.
- Run output gains a trx: field (absolute path) next to log:, only when a TRX was produced. Multi-project and mixed runs get one TRX per project, matching the per-project logs.
- Counts and failures (name, message, stack, location) come from the TRX when present. Failure text keeps the existing truncation and total-size rules.
- Console parsing stays the source for build errors, crashes and rejected options, and the only source for projects without TRX.
- Each framework adapter declares its TRX option: xUnit v3 `--report-xunit-trx`, MSTest, NUnit and TUnit `--report-trx`. TRX is always preferred over console text for counts and failures when it was produced.
- When a framework needs the TrxReport package and the project lacks it (for example NUnit, or MSTest without the MSTest metapackage), the run falls back to console parsing and warns, once per run and whether or not tests fail, with a complete suggestion: add Microsoft.Testing.Extensions.TrxReport (`dotnet add <project> package Microsoft.Testing.Extensions.TrxReport`) for TRX results.
- When failures are truncated and a TRX exists, help[] gives a complete command to read the detail from the TRX.
- The tool never deletes the TRX.

## Out of scope

- Adding the TrxReport package to user projects (the tool does not modify projects).
- Classic VSTest projects and --logger trx: stays in the backlog item support-classic-vstest-projects. TrxReader is runner-agnostic so that item can reuse it.
- Pruning old logs and TRX files (backlog item prune-old-log-files-from-the-log-directory should cover both).
- Listing passed and skipped test names (backlog item show-passed-and-skipped-test-names-on-request can build on the TRX).
- Renaming or removing any existing output field.

## Approach

- TrxReader: pure function, TRX XML in, counts and failures out, no MTP knowledge. Tested against fixtures captured from the samples.
- Artifact paths: replace TestRunner.NewLogPath with a helper returning the log and TRX paths with one shared stem.
- Detection: the adapter says which TRX option it uses and which direct package ids imply TRX support (`MSTest`, `TUnit`, `Microsoft.Testing.Extensions.TrxReport`; none for xUnit v3). FrameworkDetector already reads direct ids; step 1 confirmed transitive references are not listed, which is fine because the metapackage itself is the direct id. TRX args are added only when available, so no option is rejected and no run is repeated. If detection fails, fall back to console parsing.
- Merge: console parse first (status, build errors, crash, summary), then overlay TRX counts and failures. A run only passes with a success summary (console or TRX, not tied to MTP wording), a zero exit code and no failure signal. If TRX counts and the console summary disagree, status is NoSummary and help points at both files.
- Mixed runs: per-project TRX paths feed ResultMerger like per-project logs.
- Alternatives: always pass --report-trx and retry on rejection (rejected: doubles run time on failure); check for the extension dll next to build output (rejected: needs a build first).

## Steps

- `01-add-trxreader-with-captured-trx-fixtures.md` - TrxReader and TRX fixtures from the samples.
- `02-write-trx-beside-the-log-and-merge-it-in.md` - TRX written beside the log, merged into results, trx: field and missing-package warning.
- `03-end-to-end-tests-for-trx-and-fallback.md` - end-to-end coverage of TRX, fallback and mixed solutions.
- `04-docs-adr-changelog-and-release-prep.md` - docs, ADR, README, changelog and skill.

## Acceptance

- dotnet test --solution DotnetTestAxi.slnx passes, including new TrxReader unit tests and end-to-end tests decoded with ToonDecoder.
- MSTest and TUnit sample runs print trx: beside log:, same stem, and the file exists and parses. Counts and failures match the previous console-derived result.
- xUnit v3 sample runs print trx: (via --report-xunit-trx). An NUnit run without the package has no trx: field, output is otherwise as today, and shows the TrxReport warning; with the package it prints trx:.
- A mixed solution with and without the extension reports a trx per project that has one.
- Build errors and crashes still report their statuses. A forced TRX versus summary count mismatch yields NoSummary.
- Existing fixtures still parse identically.

## Docs impact

- docs/architecture/result-sources.md - new: TRX versus console parsing, artifact naming, merge and mismatch rules.
- README.md - update: framework table gets a TRX column and the trx: field is documented.
- CHANGELOG.md - update: Added entry under Unreleased.
- skills/dotnet-test-axi/SKILL.md - update: mention trx: and the version bump.
- ADR: Prefer TRX over console parsing when the extension is present (proposed).

## Risks and open questions

- The package-id read may not list transitive packages; step 1 verifies and picks the fallback.
- Frameworks may populate TRX stack and message fields differently; fixtures from all four frameworks cover this.
- Rule intent for mismatch handling: never report success when the run failed. The mismatch to NoSummary decision is the conservative reading; revisit if it proves noisy.

- Implemented limit: a run of several projects through one `dotnet test --solution` call shares one report file name, so the files would overwrite each other. TRX is used only when a run targets one project (including each project of a mixed, filtered solution run); other runs use the console log and print no trx: or warning.
- Implemented side effect: with the TRX extension active Microsoft Testing Platform reports host exit code 7 for a crashed host instead of 134; status stays crashed.
