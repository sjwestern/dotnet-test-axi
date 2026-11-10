namespace DotnetTestAxi;

internal static class Help
{
    public const string Text = """
        usage: dotnet-test-axi <target> [--class <name>] [--method <name>] [--namespace <name>] [--no-build] [--parallel <n>] [--full] [-- <extra dotnet test args>]
               dotnet-test-axi --from-log <file> [--full]
        target: a name from .dotnet-test-axi.json, or a path to a project, solution or directory
        --class, --method, --namespace: filter which tests run; same rules on xUnit v3, MSTest, NUnit and TUnit
          values are fully qualified names; * is allowed only at the start or end of a value
          --method needs a qualified name (MyApp.Orders.OrderTests.Saves) or a leading * (*Saves)
          the framework of each project is detected from its packages (override with "frameworks" in the config)
        --no-build: skip the build step
        --parallel: projects to run at once when a filtered solution mixes test frameworks (default: half the processors, 1-8)
        --full: list every failure with complete messages (default: first 10, messages cut at 300 chars)
        --from-log: summarise a saved log instead of running tests (no tests are run)
        --                          pass everything after it to dotnet test
        output: status (pass|fail|build-error|no-tests|no-summary|unknown-framework), projects table for mixed solutions, counts, failures, log, help
        exit: 0 pass, 1 any other status, 2 usage error
        examples:
          dotnet-test-axi tests/MyApp.Tests
          dotnet-test-axi unit --class "MyApp.Orders.OrderTests"
          dotnet-test-axi --from-log results.log --full
        config: .dotnet-test-axi.json, found by walking up from the working directory
          { "targets": { "unit": "tests/MyApp.Tests", "all": "MyApp.slnx" }, "frameworks": { "tests/Odd/Odd.csproj": "mstest" }, "logDir": ".test-logs" }

        """;
}
