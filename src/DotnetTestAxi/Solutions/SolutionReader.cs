using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DotnetTestAxi.Solutions;

/// <summary>Lists the project files of a .slnx, .sln or .slnf solution as absolute paths.</summary>
internal static partial class SolutionReader
{
    [GeneratedRegex("""^\s*Project\("\{[^}]+\}"\)\s*=\s*"[^"]*"\s*,\s*"([^"]+)"\s*,""", RegexOptions.Multiline)]
    private static partial Regex SlnProject();

    /// <summary>Reads the solution. Missing files and non-project entries are skipped; they are not errors.</summary>
    public static IReadOnlyList<string> Read(string solutionPath)
    {
        var full = Path.GetFullPath(solutionPath);
        var dir = Path.GetDirectoryName(full)!;
        var ext = Path.GetExtension(full).ToLowerInvariant();
        var relative = ext switch
        {
            ".slnx" => ReadSlnx(full),
            ".sln" => ReadSln(full),
            ".slnf" => ReadSlnf(full, out dir),
            _ => throw new ArgumentException($"not a solution file: {solutionPath}"),
        };

        var result = new List<string>();
        foreach (var rel in relative)
        {
            var path = Path.GetFullPath(rel.Replace('\\', Path.DirectorySeparatorChar), dir);
            if (IsProject(path) && File.Exists(path) && !result.Contains(path))
            {
                result.Add(path);
            }
        }

        return result;
    }

    private static bool IsProject(string path) =>
        path.EndsWith("proj", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> ReadSlnx(string path) =>
        XDocument.Load(path).Descendants()
            .Where(e => e.Name.LocalName == "Project")
            .Select(e => (string?)e.Attribute("Path"))
            .OfType<string>();

    private static IEnumerable<string> ReadSln(string path) =>
        SlnProject().Matches(File.ReadAllText(path)).Select(m => m.Groups[1].Value);

    private static IEnumerable<string> ReadSlnf(string path, out string baseDir)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        var solution = doc.RootElement.GetProperty("solution");
        var parent = Path.GetFullPath(solution.GetProperty("path").GetString()!.Replace('\\', Path.DirectorySeparatorChar), Path.GetDirectoryName(path)!);
        baseDir = Path.GetDirectoryName(parent)!;
        var listed = solution.TryGetProperty("projects", out var p)
            ? p.EnumerateArray().Select(x => x.GetString()!).ToList()
            : [];
        var all = Path.GetExtension(parent).Equals(".slnx", StringComparison.OrdinalIgnoreCase) ? ReadSlnx(parent) : ReadSln(parent);
        var set = listed.Select(x => x.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return all.Where(x => set.Contains(x.Replace('\\', '/'))).ToList();
    }
}
