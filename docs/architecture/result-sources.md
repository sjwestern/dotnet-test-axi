---
title: Result sources
summary: "How console parsing and TRX combine into a run result: artifact layout, per-framework options, merge and mismatch rules"
---

# Result sources

How a run's result is built from the console log and the TRX report. Decision: ADR-0002-prefer-trx-over-console-parsing-when-the-extension-is.

## Sources

- The console log is always written and always parsed first (`MtpParser`). It is the only source for build errors, crashes, rejected options and the unsupported-runner case, and the fallback source for everything else.
- A TRX report is requested when the run targets one project and the framework can write one. It supplies the failures (name, message, first source location) through `TrxReader`, a runner-agnostic reader of the Visual Studio TRX schema in `src/DotnetTestAxi/Trx/`.

## Artifact layout

Each run gets its own folder, `<log dir>/<yyyy-MM-dd>/<name>-<HHmmss>/` (the log dir is `logDir` from the config, else `<temp>/dotnet-test-axi`), holding `<name>.log`, `<name>.trx` when produced, and anything else the runner writes into `--results-directory` (TUnit adds an HTML report). A second run in the same second gets a `-2` suffix. Delete a run folder to remove a run, or a date folder to remove a day. The tool never deletes them.

## Which framework writes TRX

`TrxSupport` holds the options per framework:

| Framework | Option | Package needed |
|---|---|---|
| xUnit v3 | `--report-xunit-trx`, `--report-xunit-trx-filename` | none |
| MSTest | `--report-trx`, `--report-trx-filename` | `MSTest` (metapackage) or `Microsoft.Testing.Extensions.TrxReport` |
| NUnit | `--report-trx`, `--report-trx-filename` | `Microsoft.Testing.Extensions.TrxReport` |
| TUnit | `--report-trx`, `--report-trx-filename` | `TUnit` (metapackage) or `Microsoft.Testing.Extensions.TrxReport` |

The package check reads direct package references, the same read the framework detection uses. It does not list transitive packages, which is why the metapackages `MSTest` and `TUnit` count: they depend on the extension. A project referencing only `MSTest.TestAdapter` or `TUnit.Engine` does not get it. When the package ids cannot be read, TRX is skipped without a warning.

The options are added ahead of native passthrough arguments (after `--`), together with `--results-directory <run folder>`.

## One project per report

Test applications write the report file name they are given, and several projects run by one `dotnet test --solution` call would overwrite each other. So TRX is used when the run targets one project: a project, a solution with one test project, or each project of a mixed solution run (those run one project at a time). A multi-project run of one framework, or any unfiltered multi-project run, uses the console log only and prints no `trx:` or warning. Framework detection runs for a single-project target even without a filter (about 0.5 s) so the right option is chosen.

## Merge rules (`ResultSources.Finish`)

- The console result is parsed first. A valid TRX is read when the file exists; a missing or invalid file leaves the console result unchanged.
- Console status `pass` or `fail`: TRX and console counts (total, passed, failed, skipped) must agree. If they do, the failures come from the TRX. If they differ, the status is `no-summary` and help names the TRX path. This is the conservative reading of never reporting success on conflicting evidence.
- Console status `no-summary` and a TRX with tests: the TRX is the summary. Counts and failures come from it; failures give `fail`, no failures plus exit code 0 gives `pass`.
- Build errors, crashes, rejected options and unsupported runners keep their console status.
- A pass always needs a success summary (console or TRX) and a zero exit code. A non-zero exit turns a pass into `no-summary`.

A crashed host still writes a partial TRX when the extension is active; the status stays `crashed`. The extension also changes the reported host exit code (7 instead of 134 on Linux).

## Output

`trx:` (one project) or `trxs` (several, mixed runs) is printed next to `log:` only when a report was read. When a framework needs the package and the project lacks it, the run uses the console log and prints `warning:` once, with a complete `dotnet add <project> package Microsoft.Testing.Extensions.TrxReport` command in `help`. When failures are truncated and a TRX exists, `help` points at it for complete messages and stack traces.
