using System.Diagnostics;
using System.Text.Json;
using DotnetTestAxi.Frameworks.Adapters;

namespace DotnetTestAxi.Frameworks;

/// <summary>The framework found for one project. <see cref="Adapter"/> is null when it is unknown.</summary>
internal sealed record Detection(string Project, IFrameworkAdapter? Adapter, string Source)
{
    public bool IsUnknown => Adapter is null;

    public string Help => $"Set \"frameworks\" in {Config.FileName} (\"{Project}\": \"xunit3|mstest|nunit|tunit\") or pass native options after --";
}

internal static class FrameworkDetector
{
    /// <summary>Detects every project in parallel. The config override wins over package detection.</summary>
    public static async Task<IReadOnlyList<Detection>> DetectAsync(IEnumerable<string> projects, Config? config)
    {
        var tasks = projects.Select(p => DetectAsync(p, config)).ToList();
        return await Task.WhenAll(tasks);
    }

    public static async Task<Detection> DetectAsync(string project, Config? config)
    {
        if (config?.FrameworkFor(project) is { } name)
        {
            var adapter = AdapterRegistry.All.FirstOrDefault(a => a.Id.Equals(name, StringComparison.OrdinalIgnoreCase));
            return new Detection(project, adapter, "config");
        }

        var ids = await ReadPackageIdsAsync(project);
        return new Detection(project, AdapterRegistry.Find(ids), "packages");
    }

    /// <summary>Pure matching of package ids to an adapter.</summary>
    public static IFrameworkAdapter? Match(IEnumerable<string> packageIds) => AdapterRegistry.Find(packageIds);

    public static IReadOnlyList<string> ParsePackageIds(string msbuildJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(msbuildJson);
            if (doc.RootElement.TryGetProperty("Items", out var items)
                && items.TryGetProperty("PackageReference", out var refs))
            {
                return refs.EnumerateArray()
                    .Select(r => r.TryGetProperty("Identity", out var id) ? id.GetString() : null)
                    .OfType<string>()
                    .ToList();
            }
        }
        catch (JsonException)
        {
        }

        return [];
    }

    private static async Task<IReadOnlyList<string>> ReadPackageIdsAsync(string project)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };
        foreach (var a in new[] { "msbuild", project, "-getItem:PackageReference", "-nologo" }) { psi.ArgumentList.Add(a); }
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        try
        {
            using var p = Process.Start(psi);
            if (p is null) { return []; }
            p.StandardInput.Close();
            var err = p.StandardError.ReadToEndAsync();
            var output = await p.StandardOutput.ReadToEndAsync();
            await err;
            await p.WaitForExitAsync();
            return p.ExitCode == 0 ? ParsePackageIds(output) : [];
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return [];
        }
    }
}
