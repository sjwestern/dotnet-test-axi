using System.Diagnostics;

namespace DotnetTestAxi;

internal static class TestRunner
{
    public static List<string> BuildArguments(string targetPath, Args args)
    {
        var isSolution = targetPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                         || targetPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
                         || targetPath.EndsWith(".slnf", StringComparison.OrdinalIgnoreCase);
        var list = new List<string> { "test", isSolution ? "--solution" : "--project", targetPath };
        if (args.Namespace is not null) { list.Add("--filter-namespace"); list.Add(args.Namespace); }
        if (args.Class is not null) { list.Add("--filter-class"); list.Add(args.Class); }
        if (args.Method is not null) { list.Add("--filter-method"); list.Add(args.Method); }
        if (args.NoBuild) { list.Add("--no-build"); }
        list.Add("--no-progress");
        list.AddRange(args.Passthrough);
        return list;
    }

    public static string NewLogPath(string targetPath, string? logDir)
    {
        var dir = logDir ?? Path.Combine(Path.GetTempPath(), "dotnet-test-axi");
        Directory.CreateDirectory(dir);
        var name = Path.GetFileNameWithoutExtension(targetPath.TrimEnd('/', '\\'));
        return Path.Combine(dir, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    /// <summary>Runs dotnet, writing all output to <paramref name="logPath"/>. Returns the exit code.</summary>
    public static async Task<int> RunAsync(IEnumerable<string> arguments, string workingDir, string logPath)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };
        foreach (var a in arguments) { psi.ArgumentList.Add(a); }
        psi.Environment["NO_COLOR"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("could not start dotnet");
        process.StandardInput.Close();
        await using var writer = new StreamWriter(logPath, append: false) { AutoFlush = true };
        var gate = new object();
        void Write(string? line)
        {
            if (line is null) { return; }
            lock (gate) { writer.WriteLine(line); }
        }

        var stdout = Pump(process.StandardOutput, Write);
        var stderr = Pump(process.StandardError, Write);
        await Task.WhenAll(stdout, stderr);
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static async Task Pump(StreamReader reader, Action<string?> write)
    {
        while (await reader.ReadLineAsync() is { } line) { write(line); }
    }
}
