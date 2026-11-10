using System.Diagnostics;
using DotnetTestAxi.Frameworks;
using DotnetTestAxi.Frameworks.Adapters;
using DotnetTestAxi.Solutions;

namespace DotnetTestAxi;

internal static class TestRunner
{
    public static bool IsSolution(string path) =>
        path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".slnf", StringComparison.OrdinalIgnoreCase);

    /// <summary>The test projects behind a target: the project itself, a solution's projects, or the one solution or project in a directory.</summary>
    public static IReadOnlyList<string> ProjectsOf(string target)
    {
        if (Directory.Exists(target))
        {
            var solutions = Directory.EnumerateFiles(target).Where(IsSolution).ToList();
            if (solutions.Count == 1) { return SolutionReader.Read(solutions[0]); }
            return Directory.EnumerateFiles(target, "*proj").ToList() is { Count: 1 } one ? one : [];
        }

        return IsSolution(target) ? SolutionReader.Read(target) : [target];
    }

    /// <param name="adapter">Translates the filter; needed only when the args carry one.</param>
    public static List<string> BuildArguments(string targetPath, Args args, IFrameworkAdapter? adapter = null)
    {
        var list = new List<string> { "test", IsSolution(targetPath) ? "--solution" : "--project", targetPath };
        if (adapter is not null) { list.AddRange(adapter.BuildFilterArguments(args.Filter)); }
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

internal enum Strategy
{
    Unfiltered,
    Single,
    Mixed,
    Unknown,
}

/// <summary>How a run is carried out, chosen from the filter and the detected frameworks.</summary>
internal sealed record RunPlan(Strategy Strategy, IFrameworkAdapter? Adapter, IReadOnlyList<Detection> Detections)
{
    /// <summary>Pure strategy choice over project-framework pairs.</summary>
    public static RunPlan Choose(bool filterIsEmpty, IReadOnlyList<Detection> detections)
    {
        if (filterIsEmpty) { return new RunPlan(Strategy.Unfiltered, null, detections); }
        if (detections.Count == 0 || detections.Any(d => d.IsUnknown)) { return new RunPlan(Strategy.Unknown, null, detections); }
        var ids = detections.Select(d => d.Adapter!.Id).Distinct().ToList();
        return ids.Count == 1
            ? new RunPlan(Strategy.Single, detections[0].Adapter, detections)
            : new RunPlan(Strategy.Mixed, null, detections);
    }
}
