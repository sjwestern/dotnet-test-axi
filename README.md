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
  MyApp.Orders.OrderTests.Rejects_empty,"Assert.Equal() Failure: Values differ | Expected: 3 | Actual:   2","OrderTests.cs:6"
  ...
exit: 2
log: /tmp/dotnet-test-axi/MyApp.Tests-20261110-152838.log
help[2]: "Run `dotnet-test-axi tests/MyApp.Tests --class \"MyApp.Orders.OrderTests\"` to rerun one failing class",Read /tmp/dotnet-test-axi/MyApp.Tests-20261110-152838.log for the full output instead of rerunning the suite
```

Output follows the [AXI](https://github.com/kunchenguid/axi) agent-CLI principles and is encoded as [TOON](https://toonformat.dev/) with the official `Toon.Format` library.

## Why

Agents rerun `dotnet test` with different `tail`/`grep` filters to see failures, and a failing MTP run buries the failures in progress and application logs.
This runs once, saves everything, and prints only what is needed. A run that crashes or prints no summary is reported as `no-summary`, never as a pass.

## Install (from a clone, before it is published)

Requires the .NET 10 SDK.

```sh
git clone <this repo> && cd dotnet-test-axi
dotnet pack src/DotnetTestAxi -c Release -o nupkg
dotnet tool install -g dotnet-test-axi --add-source ./nupkg      # first time
dotnet tool uninstall -g dotnet-test-axi && dotnet tool install -g dotnet-test-axi --add-source ./nupkg   # after changes (same version number)
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
- Exit codes: 0 for `pass`, 1 for any other status, 2 for a usage error. The underlying `dotnet test` code is reported as `exit:`.
- Errors are printed to stdout as `error`, `code` and `help`. Unknown flags are rejected with the list of valid flags.
- Run with no arguments to see the config found and the available targets.
- `--full` and `--from-log` read the saved log, so you never need to rerun the suite to see more.

### Status values

| Status | Meaning |
|---|---|
| `pass` | Summary printed and `dotnet test` exited 0 |
| `fail` | Tests failed (failures listed) |
| `build-error` | The build failed (compile errors listed) |
| `no-tests` | Zero tests ran, which MTP treats as an error. Usually a filter that matched nothing |
| `crashed` | The test host died mid-run (`host_exit`, `detail`); counts are incomplete, so only `completed` is shown |
| `invalid-arguments` | The test application rejected an option, for example a filter flag it does not have (`options` lists them) |
| `unsupported-runner` | The project uses VSTest, not Microsoft Testing Platform |
| `no-summary` | No summary was printed and no cause was recognised; read the log |

### Framework support

Verified against the sample projects in `tests/Samples` (all on Microsoft Testing Platform):

| Framework | Run, summary and failures | `--class`, `--method`, `--namespace` |
|---|---|---|
| xUnit v3 | yes | yes |
| MSTest | yes | no, reported as `invalid-arguments` |
| NUnit | yes | no, reported as `invalid-arguments` |
| TUnit | yes | no, reported as `invalid-arguments` |
| xUnit v2 / classic VSTest | not supported, reported as `unsupported-runner` | n/a |

`--class`, `--method` and `--namespace` are xUnit v3 options. For other frameworks pass the framework's own filter after `--`.
Test names from MSTest, NUnit and TUnit are reported without the class, so no rerun hint is offered for them.

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

Layout: `src/` holds the tool; `tests/` holds the unit tests (`DotnetTestAxi.Tests`, fast), the end-to-end tests (`DotnetTestAxi.EndToEndTests`, which run the real tool against the samples and take about 20 seconds) and `tests/Samples/`.

`tests/Samples/` has one small test project per framework setup: `Xunit3.Sample`, `MSTest.Sample`, `NUnit.Sample`, `TUnit.Sample` (all Microsoft Testing Platform) and `VSTest.Sample` (xUnit v2 on the classic VSTest runner, with its own `global.json`). They pass by default. Set `SAMPLE_SCENARIO=fail` or `crash` to make them fail or kill the test host, and build with `-p:SampleBuildError=true` for a compile error.

Parser fixtures in `fixtures/` are real output. Regenerate the per-framework ones from the samples with `pwsh scripts/capture-fixtures.ps1` and review the diff: a change means a framework or Microsoft Testing Platform output changed. Add a fixture whenever a new output shape is found.

```sh
dotnet test --project tests/DotnetTestAxi.Tests     # fast
dotnet test --solution DotnetTestAxi.slnx           # everything, including end to end
```

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/), enforced by commitlint through a husky hook. Run `npm install` once after cloning to enable it (Node is only needed for this, not for the tool).

## Licence

MIT
