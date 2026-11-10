# Backlog

## In flight
## Queued
- [ ] reuse-dotnet-axi-toon-encoder-for-spec-conformant - Reuse dotnet-axi TOON encoder for spec-conformant quoting (kind: chore) (priority: 2) (since 2026-11-10)
  Public Apache-2.0 project chuyflores-dev/dotnet-axi has ToonV41Encoder. Reuse with attribution in NOTICE only if it stays test-specific.
- [ ] add-process-timeout-and-child-cleanup-to-the - Add process timeout and child cleanup to the runner (kind: idea) (priority: 2) (since 2026-11-10)
  Add --timeout and kill the process tree on timeout or cancel. dotnet-axi ProcessRunner is a reference.
- [ ] add-a-net48-xunit-v3-exe-runner-mode - Add a net48 xunit v3 exe runner mode (kind: idea) (priority: 3) (since 2026-11-10)
  Projects not on dotnet test (exe runners) need a second runner and parser behind the same output.
- [ ] show-passed-and-skipped-test-names-on-request - Show passed and skipped test names on request (kind: idea) (priority: 3) (since 2026-11-10)
- [ ] prune-old-log-files-from-the-log-directory - Prune old log files from the log directory (kind: chore) (priority: 3) (since 2026-11-10)
- [ ] publish-to-nuget-org-with-release-automation - Publish to nuget.org with release automation (kind: chore) (priority: 2) (since 2026-11-10)
  Pack in CI, push on tag, add version bump and changelog. Make the repo public first.
- [ ] add-a-install-script-for-the-tool-and-the-skill - Add a install script for the tool and the skill link (kind: chore) (priority: 3) (since 2026-11-10)
- [ ] cross-platform-ci-build-and-test-on-linux-windows - Cross-platform CI: build and test on Linux, Windows and macOS (kind: chore) (priority: 1) (since 2026-11-10)
- [ ] generate-skill-md-from-the-home-view-and-fail-ci - Generate SKILL.md from the home view and fail CI when stale (kind: chore) (priority: 2) (since 2026-11-10)
  AXI section 7: single source of truth. Strip live state.
- [ ] add-an-update-command-wrapping-dotnet-tool-update - Add an update command wrapping dotnet tool update (kind: idea) (priority: 3) (since 2026-11-10)
  AXI built-in self-update equivalent. current vs latest from the feed, print-only when the install method is unknown.
- [ ] optional-session-start-hook-for-ambient-context - Optional session-start hook for ambient context (kind: idea) (priority: 4) (since 2026-11-10)
  AXI section 7. Probably low value for a test runner; the skill is the primary route.
- [ ] split-the-at-column-into-file-and-line-to-avoid - Split the at column into file and line to avoid quoting (kind: chore) (priority: 3) (since 2026-11-10)
- [ ] guard-the-version-fast-path-with-a-latency-test - Guard the version fast path with a latency test (kind: chore) (priority: 3) (since 2026-11-10)
- [ ] add-fields-to-widen-the-failure-columns - Add --fields to widen the failure columns (kind: idea) (priority: 4) (since 2026-11-10)
- [ ] support-classic-vstest-projects - Support classic VSTest projects (kind: idea) (priority: 2) (since 2026-11-10)
  dotnet test --project is rejected in VSTest mode, so these projects currently report unsupported-runner. Run with a positional path and parse the VSTest console output (Passed!/Failed! summary line, Failed <name> blocks). VSTest.Sample is the test project.
- [ ] report-class-qualified-test-names-for-mstest-nunit - Report class-qualified test names for MSTest, NUnit and TUnit (kind: idea) (priority: 3) (since 2026-11-10)
  These frameworks print bare names, so the rerun-one-class hint is dropped. Possible sources: TRX output or the from line with a type lookup.
- [ ] add-multi-target-sample-for-mtp - Add multi-target sample for MTP (kind: idea) (priority: 3) (since 2026-11-10)
  A project targeting several frameworks prints one module block per target. Needs a second runtime installed on the machine.
- [ ] add-a-skipped-tests-listing-and-a-list-flag-for - Add a skipped-tests listing and a --list flag for discovered tests (kind: idea) (priority: 4) (since 2026-11-10)
## Done
- [x] add-trx-reporting-as-a-structured-result-source - Add TRX reporting as a structured result source with console parsing as fallback (kind: change) (priority: 1) (done 2026-11-10)
  Use --report-trx when the Microsoft.Testing.Extensions.TrxReport extension is present. Keep the console parser for build errors, crashes and projects without the extension.
- [x] add-per-framework-test-filters - Translate --class, --method and --namespace to each test framework (kind: change) (priority: 1) (done 2026-11-10)
  Only xUnit v3 supports --filter-class/method/namespace. MSTest and NUnit use --filter, TUnit uses --treenode-filter. Detect the framework from the project package references or from the rejected option, and translate. Samples and fixtures already cover all four.
