namespace DotnetTestAxi;

internal static class Help
{
    public const string Text = """
        usage: dotnet-test-axi <target> [--class <name>] [--method <name>] [--namespace <name>] [--no-build] [--full] [-- <extra dotnet test args>]
               dotnet-test-axi --from-log <file> [--full]
        target: a name from .dotnet-test-axi.json, or a path to a project, solution or directory
        --class, --method, --namespace: filter which tests run (Microsoft Testing Platform filters)
        --no-build: skip the build step
        --full: list every failure with complete messages (default: first 10, messages cut at 300 chars)
        --from-log: summarise a saved log instead of running tests (no tests are run)
        --                          pass everything after it to dotnet test
        output: status (pass|fail|build-error|no-tests|no-summary), counts, failures, log, help
        exit: 0 pass, 1 any other status, 2 usage error
        examples:
          dotnet-test-axi tests/MyApp.Tests
          dotnet-test-axi unit --class "MyApp.Orders.OrderTests"
          dotnet-test-axi --from-log results.log --full
        config: .dotnet-test-axi.json, found by walking up from the working directory
          { "targets": { "unit": "tests/MyApp.Tests", "all": "MyApp.slnx" }, "logDir": ".test-logs" }

        """;
}
