# Backlog

## In flight
## Queued
- [ ] add-trx-reporting-as-a-structured-result-source - Add TRX reporting as a structured result source with console parsing as fallback (kind: idea) (priority: 1) (since 2026-11-10)
  Use --report-trx when the Microsoft.Testing.Extensions.TrxReport extension is present. Keep the console parser for build errors, crashes and projects without the extension.
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
## Done
