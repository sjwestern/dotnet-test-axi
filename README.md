# dotnet-test-axi

Runs `dotnet test` and prints a compact summary built for AI coding agents (and humans in a hurry).
It parses [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) output (xUnit v3, MSTest, NUnit and TUnit on MTP) and always saves the full output to a log file.

It does one thing: run tests and report. It is not a general .NET tool.

```
$ dotnet-test-axi tests/MyApp.Tests --class "MyApp.Orders.OrderTests"
status: fail
total: 6
passed: 2
failed: 3
skipped: 1
duration: 1s 068ms
failures[3]{name,message,at}:
  MyApp.Orders.OrderTests.Rejects_empty,Assert.Equal() Failure: Values differ | Expected: 3 | Actual:   2,OrderTests.cs:6
  ...
exit: 2
log: /tmp/dotnet-test-axi/MyApp.Tests-20261110-152838.log
next: rerun one class: dotnet-test-axi tests/MyApp.Tests --class "MyApp.Orders.OrderTests". full output: read the log, do not rerun the whole suite
```

## Why

Agents rerun `dotnet test` with different `tail`/`grep` filters to see failures, and a failing MTP run buries the failures in progress and application logs.
This runs once, saves everything, and prints only what is needed. A run that crashes or prints no summary is reported as `no-summary`, never as a pass.

## Install (from a clone, before it is published)

Requires the .NET 10 SDK.

```sh
git clone <this repo> && cd dotnet-test-axi
dotnet pack src/DotnetTestAxi -c Release -o nupkg
dotnet tool install -g dotnet-test-axi --add-source ./nupkg      # first time
dotnet tool update  -g dotnet-test-axi --add-source ./nupkg      # after changes
```

To pin it per repository instead, use a local tool manifest in that repo:

```sh
dotnet new tool-manifest
dotnet tool install dotnet-test-axi --add-source <path-to>/nupkg
dotnet tool restore
dotnet tool run dotnet-test-axi -- <args>      # or: dotnet dotnet-test-axi <args>
```

Install the agent skill by linking `skills/dotnet-test-axi` into your agent's skills folder (for example `~/.agents/skills/`).

## Use

```
dotnet-test-axi <target> [--class <name>] [--method <name>] [--namespace <name>] [--no-build] [--full] [-- <extra dotnet test args>]
dotnet-test-axi --from-log <file> [--full]
```

- `<target>`: a name from `.dotnet-test-axi.json`, or a path to a project, solution (`.sln`, `.slnx`, `.slnf`) or directory.
- `--class`, `--method`, `--namespace` map to the MTP `--filter-class`, `--filter-method` and `--filter-namespace` options.
- `--full` lists every failure (the default shows the first 10).
- `--from-log` summarises an existing log instead of running tests.
- Exit code is 0 only when the status is `pass`.
- Run with no arguments to see the config found and the available targets.

### Status values

`pass`, `fail`, `build-error`, `no-tests` (zero tests ran, which MTP treats as an error), `no-summary` (no summary was printed, the run likely crashed).

### Config

Optional `.dotnet-test-axi.json`, found by walking up from the working directory. Paths are relative to the file.

```json
{
  "targets": { "unit": "tests/MyApp.Tests", "all": "MyApp.slnx" },
  "logDir": ".test-logs"
}
```

Logs go to the system temp folder (`dotnet-test-axi/`) unless `logDir` is set.

## Develop

```sh
dotnet test --solution DotnetTestAxi.slnx
```

Parser fixtures in `fixtures/` are real MTP output from generic demo projects. Add a fixture whenever a new output shape is found.

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/), enforced by commitlint through a husky hook. Run `npm install` once after cloning to enable it (Node is only needed for this, not for the tool).

## Licence

MIT
