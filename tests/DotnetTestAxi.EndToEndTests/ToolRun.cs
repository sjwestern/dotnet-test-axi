using System.Diagnostics;
using System.Text.Json.Nodes;
using Toon.Format;

namespace DotnetTestAxi.EndToEndTests;

/// <summary>Runs the built dotnet-test-axi against a sample project, as an agent would.</summary>
internal static class ToolRun
{
    private static readonly string ToolDll = typeof(Args).Assembly.Location;

    public static string SamplePath(string name) =>
        Path.GetFullPath(Path.Combine(FindRoot(), "tests", "Samples", name));

    public static async Task<(int Exit, string Text, JsonNode Output)> RunAsync(
        string sample, string scenario, params string[] extra)
    {
        var psi = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.ArgumentList.Add(ToolDll);
        psi.ArgumentList.Add(SamplePath(sample));
        foreach (var e in extra) { psi.ArgumentList.Add(e); }
        psi.Environment["SAMPLE_SCENARIO"] = scenario;

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var text = await stdout;
        _ = await stderr;
        return (process.ExitCode, text, ToonDecoder.Decode(text)!);
    }

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DotnetTestAxi.slnx"))) { return dir.FullName; }
        }

        throw new InvalidOperationException("repo root not found");
    }
}
