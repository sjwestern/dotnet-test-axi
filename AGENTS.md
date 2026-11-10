# dotnet-test-axi

Agent-friendly `dotnet test` runner and result summariser (a .NET tool). It only runs tests and reports results.

## Rules

- Keep it specific to .NET testing. Do not add general .NET analysis or project-modification features.
- Keep the repo generic. Never reference other projects, companies, products or private paths in code, tests, fixtures, docs or examples. Use generic names (`MyApp`, `Demo`) and sanitised paths (`/work/...`).
- Layout: `src/` (the tool), `tests/` (unit tests, end-to-end tests, `tests/Samples/` sample projects), solution `DotnetTestAxi.slnx` at the root.
- Parser changes are test-first against a log in `fixtures/`. Fixtures are real output from the sample projects (regenerate with `pwsh scripts/capture-fixtures.ps1`, which rewrites absolute paths to `/work/...`). When a framework or runner shows a new output shape, add a scenario to the samples and a captured fixture rather than hand-writing one.
- Repo-specific scripts are pwsh (`scripts/*.ps1`) so they run on Windows, macOS and Linux. No bash scripts.
- A new test framework needs an `IFrameworkAdapter` (registered in `AdapterRegistry`) with rows in `AdapterTests`, a sample project in `tests/Samples` (added to `Mixed.slnx`), a native-filter fixture in `scripts/capture-fixtures.ps1`, end-to-end filter cases and a README table row. See `docs/architecture/test-filtering.md`.
- Samples cover: xUnit v3, MSTest, NUnit and TUnit on Microsoft Testing Platform, and a classic VSTest project. Behaviour per framework is documented in the README framework table; keep it accurate.
- Never report success without an MTP summary and a zero exit code.
- Output must follow the AXI rules (load the `axi` skill before changing anything that prints). In short: TOON on stdout produced by the official `Toon.Format` library (never hand-roll TOON, quoting is subtle); minimal fields; truncate long text and show the total size; definitive empty states; errors on stdout as `error`, `code`, `help` with exit 2 for usage errors; reject unknown flags and list the valid ones; no prompts; no progress on stdout; `help[]` suggestions are complete commands; the no-argument home view shows `bin` and `description` first, then live state; `-v`, `-V` and `--version` answer before any other work.
- Keep field names stable (agents depend on them). Assert output in tests by decoding it with `ToonDecoder`, not by string matching.
- Exit codes: 0 pass, 1 any other status, 2 usage error.
- Parts of the public Apache-2.0 `dotnet-axi` project may be reused with attribution (add a `NOTICE` entry when doing so).
- Use `dotnet test --solution DotnetTestAxi.slnx` for tests. Commits are conventional (`feat:`, `fix:`, `docs:`, `test:`, `chore:`).

<!-- plan-axi:start -->
## plan-axi

- On a branch matching `specs/changes/active/<slug>`, use the plan-apply skill (Execute mode). Run `change-axi` with no args for the change status and next step.
- For any question about change, step, or backlog state (what's next, in progress, blocked, or whether changes can run together), run `change-axi` and `tasks-axi list` first. Never read `tasks.md` or `backlog.md` directly (cat, rg, read). Step state lives on each change's worktree branch, so `main` is stale for it: use `change-axi status <slug>` from the worktree. Read `proposal.md` and step files only for content (scope, Owns), not state.
- Capture unplanned work with `change-axi idea "<one-liner>" [--kind idea|bug|chore] [--priority 0-4]` and list it with `change-axi backlog`; turn an item into a plan with `change-axi new <slug>` (plan-propose).
- Use `change-axi` and `tasks-axi` for plan files. Never hand-edit `tasks.md` or `backlog.md`.
- To find docs, start at the index: run `docs-axi` (lists the docs index; `docs-axi config` shows the docs layout and where each kind of doc goes), then `docs-axi search <terms>` for a topic and `docs-axi show <path> --section <name>` to read only what you need. If `docs-axi` is unavailable, use `rg <terms>` in the docs dir (it covers the index and doc bodies) and read only the matching files. Don't `rg` just the index when `docs-axi` works, since it lists only titles and summaries.
- For exploration and planning tasks, look at the docs first (see above) so the idea fits existing docs and ADRs.
- Cite ADRs in full as `ADR-NNNN-slug` (bare `ADR 7` is an error). An existing repo runs `docs-axi adr fix-refs` once to rewrite bare refs; on a number clash run the printed `docs-axi adr renumber` command.
<!-- plan-axi:end -->
