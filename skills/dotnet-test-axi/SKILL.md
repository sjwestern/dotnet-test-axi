---
name: dotnet-test-axi
description: "Run .NET tests through the dotnet-test-axi CLI and read a compact pass/fail summary instead of raw dotnet test output. Use whenever you run dotnet test, check which tests fail, or need to rerun a failing test class."
user-invocable: false
---

# dotnet-test-axi

Runs `dotnet test` (Microsoft Testing Platform) and prints a short summary. The full output is always saved to the `log:` path it prints.

Get usage from the CLI, it is the source of truth:

- `dotnet-test-axi` - config found and the available targets
- `dotnet-test-axi --help` - all options

Rules:

- Use it instead of calling `dotnet test` directly.
- Never rerun the suite to see more output. Read the `log:` file, or use `--full` for every failure.
- Rerun only what failed, using the `next:` hint.
- A `no-summary` or `build-error` status is not a pass.

If the command is not found, it is a .NET tool: install it with `dotnet tool install -g dotnet-test-axi` (or `dotnet tool restore` in a repo with a tool manifest). Until it is published, install from a clone as described in its README.
