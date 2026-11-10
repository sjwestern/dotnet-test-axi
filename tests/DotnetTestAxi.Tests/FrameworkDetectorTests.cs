using DotnetTestAxi.Frameworks;
using Xunit;

namespace DotnetTestAxi.Tests;

public sealed class FrameworkDetectorTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("detector").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Matches_package_ids_purely()
    {
        Assert.Equal("tunit", FrameworkDetector.Match(["TUnit", "Other"])?.Id);
        Assert.Null(FrameworkDetector.Match(["Microsoft.NET.Test.Sdk"]));
    }

    [Fact]
    public void Parses_msbuild_json()
    {
        const string json = """{ "Items": { "PackageReference": [ { "Identity": "NUnit" }, { "Identity": "Foo" } ] } }""";
        Assert.Equal(["NUnit", "Foo"], FrameworkDetector.ParsePackageIds(json));
        Assert.Empty(FrameworkDetector.ParsePackageIds("not json"));
        Assert.Empty(FrameworkDetector.ParsePackageIds("{}"));
    }

    private Config ConfigWith(string frameworksJson)
    {
        File.WriteAllText(Path.Combine(_dir, Config.FileName), $$"""{ "frameworks": {{frameworksJson}} }""");
        return Config.Find(_dir)!;
    }

    [Fact]
    public async Task Config_override_wins_without_running_msbuild()
    {
        var project = Path.Combine(_dir, "App", "App.csproj"); // does not exist: msbuild would find nothing
        var config = ConfigWith("""{ "App/App.csproj": "mstest" }""");
        var d = await FrameworkDetector.DetectAsync(project, config);
        Assert.Equal("mstest", d.Adapter?.Id);
        Assert.Equal("config", d.Source);
    }

    [Fact]
    public async Task Unknown_project_names_itself_and_help()
    {
        var project = Path.Combine(_dir, "Nope.csproj");
        var d = await FrameworkDetector.DetectAsync(project, null);
        Assert.True(d.IsUnknown);
        Assert.Equal(project, d.Project);
        Assert.Contains("frameworks", d.Help);
        Assert.Contains("--", d.Help);
    }

    [Fact]
    public async Task Invalid_override_is_unknown()
    {
        var config = ConfigWith("""{ "A.csproj": "jasmine" }""");
        Assert.True((await FrameworkDetector.DetectAsync(Path.Combine(_dir, "A.csproj"), config)).IsUnknown);
    }

    [Theory]
    [InlineData("Xunit3.Sample", "xunit3")]
    [InlineData("MSTest.Sample", "mstest")]
    [InlineData("NUnit.Sample", "nunit")]
    [InlineData("TUnit.Sample", "tunit")]
    public async Task Detects_sample_projects_with_msbuild(string sample, string expected)
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "DotnetTestAxi.slnx"))) { root = Path.GetDirectoryName(root); }
        var project = Path.Combine(root!, "tests", "Samples", sample, sample + ".csproj");
        var d = await FrameworkDetector.DetectAsync(project, null);
        Assert.Equal(expected, d.Adapter?.Id);
    }
}
