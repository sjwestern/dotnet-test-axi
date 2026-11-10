using DotnetTestAxi.Solutions;
using Xunit;

namespace DotnetTestAxi.Tests;

public sealed class SolutionReaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("slnreader").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Touch(string rel)
    {
        var p = Path.Combine(_dir, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, "<Project />");
        return p;
    }

    private string Write(string rel, string content)
    {
        var p = Path.Combine(_dir, rel);
        File.WriteAllText(p, content);
        return p;
    }

    [Fact]
    public void Reads_slnx_with_nested_folders_and_skips_missing()
    {
        var a = Touch("a/A.csproj");
        var b = Touch("b/B.csproj");
        var sln = Write("My.slnx", """
            <Solution>
              <Folder Name="/x/"><Folder Name="/x/y/"><Project Path="a/A.csproj" /></Folder></Folder>
              <Project Path="b/B.csproj" />
              <Project Path="gone/Gone.csproj" />
              <File Path="readme.md" />
            </Solution>
            """);
        Assert.Equal([a, b], SolutionReader.Read(sln));
    }

    [Fact]
    public void Reads_sln_skipping_solution_folders_and_missing()
    {
        var a = Touch("a/A.csproj");
        var sln = Write("My.sln", """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "A", "a\A.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Folder", "Folder", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Gone", "gone\Gone.csproj", "{33333333-3333-3333-3333-333333333333}"
            EndProject
            """);
        Assert.Equal([a], SolutionReader.Read(sln));
    }

    [Fact]
    public void Reads_slnf_as_a_filtered_view_of_its_parent()
    {
        Touch("a/A.csproj");
        var b = Touch("b/B.csproj");
        Write("My.slnx", """<Solution><Project Path="a/A.csproj" /><Project Path="b/B.csproj" /></Solution>""");
        var slnf = Write("Part.slnf", """{ "solution": { "path": "My.slnx", "projects": ["b\\B.csproj"] } }""");
        Assert.Equal([b], SolutionReader.Read(slnf));
    }

    [Fact]
    public void Reads_the_repo_solution()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "DotnetTestAxi.slnx"))) { root = Path.GetDirectoryName(root); }
        Assert.NotNull(root);
        var projects = SolutionReader.Read(Path.Combine(root!, "DotnetTestAxi.slnx"));
        Assert.Equal(7, projects.Count);
        Assert.All(projects, p => Assert.True(Path.IsPathRooted(p) && File.Exists(p)));
    }
}
