using System.Text.Json;

namespace DotnetTestAxi;

/// <summary>
/// Optional per-repo config, found by walking up from the working directory.
/// Maps short target names to project or solution paths (relative to the config file).
/// </summary>
internal sealed class Config
{
    public const string FileName = ".dotnet-test-axi.json";

    public Dictionary<string, string> Targets { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string? LogDir { get; init; }
    public string? Directory { get; init; }
    public string? Path { get; init; }

    public static Config? Find(string startDir)
    {
        for (var dir = new DirectoryInfo(startDir); dir is not null; dir = dir.Parent)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, FileName);
            if (File.Exists(candidate))
            {
                return Load(candidate);
            }
        }

        return null;
    }

    private static Config Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (doc.RootElement.TryGetProperty("targets", out var t) && t.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in t.EnumerateObject())
            {
                targets[p.Name] = p.Value.GetString() ?? "";
            }
        }

        var logDir = doc.RootElement.TryGetProperty("logDir", out var l) ? l.GetString() : null;
        return new Config { Targets = targets, LogDir = logDir, Directory = System.IO.Path.GetDirectoryName(path), Path = path };
    }

    /// <summary>Resolves a name or path to a path usable by <c>dotnet test</c>, or null if it does not exist.</summary>
    public static string? ResolveTarget(Config? config, string target, string cwd)
    {
        if (config is not null && config.Targets.TryGetValue(target, out var mapped))
        {
            return System.IO.Path.GetFullPath(mapped, config.Directory!);
        }

        var full = System.IO.Path.GetFullPath(target, cwd);
        return File.Exists(full) || System.IO.Directory.Exists(full) ? full : null;
    }
}
