# Changelog

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: [SemVer](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.0] - 2026-11-17

### Added

- `scripts/install.sh` and `scripts/install.ps1` pack the tool from a clone and install or update it as a dotnet global tool.

## [0.4.0] - 2026-11-11

### Changed

- Multi-project runs print `results_dir` once and list `logs` and `trxs` relative to it, instead of repeating the full path on every entry.

## [0.3.1] - 2026-11-11

### Fixed

- Unfiltered solution runs no longer run projects with no test framework (libraries); they are listed as `skipped_projects` instead of a false `no-summary`.

## [0.3.0] - 2026-11-10

### Added

- TRX reports: each project's run writes a `.trx` and print `trx:` next to `log:`; failures come from it with source locations. Uses `--report-xunit-trx` for xUnit v3 and `--report-trx` for MSTest, NUnit and TUnit.
- A `warning:` with the `dotnet add ... package Microsoft.Testing.Extensions.TrxReport` command when a project needs the package and lacks it.

### Changed

- Solutions and other multi-project targets now run each project separately (built once, in parallel) so each gets its own log and TRX; the output lists a `projects` table.
- Each run now has its own folder, `<log dir>/<yyyy-MM-dd>/<name>-<HHmmss>/`, holding the log and TRX, so old runs are easy to delete.
- A run passes only with a success summary from the console or the TRX and exit code 0; console and TRX counts that disagree give `no-summary`.
- Framework detection now runs on every target, not only filtered ones (about 0.5 s per project, in parallel).
- With the TRX extension active, a crashed host reports exit code 7 instead of 134 in `host_exit`.

## [0.2.0] - 2026-11-10

### Added

- README install and update steps.
- Skill carries the same version as the tool.

### Changed

- Agent skill reports a missing tool instead of falling back to `dotnet test`.

## [0.1.0]

### Added

- Compact test summary for xUnit v3, MSTest, NUnit and TUnit on Microsoft Testing Platform.
- `--class`, `--method` and `--namespace` filters for all four frameworks.
- Solution support, including mixed-framework solutions.
- Agent skill.

[Unreleased]: https://github.com/sjwestern/dotnet-test-axi/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/sjwestern/dotnet-test-axi/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/sjwestern/dotnet-test-axi/compare/v0.3.1...v0.4.0
[0.3.1]: https://github.com/sjwestern/dotnet-test-axi/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/sjwestern/dotnet-test-axi/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/sjwestern/dotnet-test-axi/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/sjwestern/dotnet-test-axi/commits/v0.1.0
