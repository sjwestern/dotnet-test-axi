# dotnet-test-axi

Agent-friendly `dotnet test` runner and result summariser (a .NET tool). It only runs tests and reports results.

## Rules

- Keep it specific to .NET testing. Do not add general .NET analysis or project-modification features.
- Keep the repo generic. Never reference other projects, companies, products or private paths in code, tests, fixtures, docs or examples. Use generic names (`MyApp`, `Demo`) and sanitised paths (`/work/...`).
- Parser changes are test-first against a log in `fixtures/`. Fixtures come from real runs of throwaway demo projects, with absolute paths rewritten to `/work/...`.
- Never report success without an MTP summary and a zero exit code.
- Output is compact TOON-style text; keep field names stable (agents depend on them).
- Parts of the public Apache-2.0 `dotnet-axi` project may be reused with attribution (add a `NOTICE` entry when doing so).
- Use `dotnet test --solution DotnetTestAxi.slnx` for tests. Commits are conventional (`feat:`, `fix:`, `docs:`, `test:`, `chore:`).
