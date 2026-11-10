using DotnetTestAxi.Frameworks;
using DotnetTestAxi.Runs;
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

    [Theory]
    [InlineData("xunit3", new string[0], "Available")] // own option, no package
    [InlineData("mstest", new[] { "MSTest" }, "Available")] // metapackage depends on the extension
    [InlineData("mstest", new[] { "MSTest.TestAdapter", "MSTest.TestFramework" }, "MissingPackage")]
    [InlineData("tunit", new[] { "TUnit" }, "Available")]
    [InlineData("tunit", new[] { "TUnit.Engine" }, "MissingPackage")]
    [InlineData("nunit", new[] { "NUnit", "NUnit3TestAdapter" }, "MissingPackage")]
    [InlineData("nunit", new[] { "NUnit", "Microsoft.Testing.Extensions.TrxReport" }, "Available")]
    [InlineData("tunit", new[] { "Microsoft.Testing.Extensions.TrxReport" }, "Available")]
    public void Trx_availability_follows_the_framework_and_package_ids(string adapter, string[] ids, string expected)
    {
        Assert.Equal(Enum.Parse<TrxAvailability>(expected), TrxSupport.Check(adapter, ids));
    }

    [Fact]
    public void Trx_is_skipped_quietly_when_nothing_is_known()
    {
        Assert.Equal(TrxAvailability.Unsupported, TrxSupport.Check(null, ["MSTest"]));
        Assert.Equal(TrxAvailability.Unsupported, TrxSupport.Check("nunit", null)); // ids not read
        Assert.Equal(TrxAvailability.Unsupported, TrxSupport.Check("nunit", [])); // lookup failed
    }

    [Theory]
    [InlineData("xunit3", "--report-xunit-trx", "--report-xunit-trx-filename")]
    [InlineData("mstest", "--report-trx", "--report-trx-filename")]
    [InlineData("nunit", "--report-trx", "--report-trx-filename")]
    [InlineData("tunit", "--report-trx", "--report-trx-filename")]
    public void Trx_arguments_write_the_given_file(string adapter, string report, string fileName)
    {
        var args = TrxSupport.Arguments(adapter, "/logs/2026-01-01/App-101010/App.trx");

        Assert.Equal([report, fileName, "App.trx", "--results-directory", "/logs/2026-01-01/App-101010"], args);
    }

    [Fact]
    public void Trx_arguments_come_before_native_passthrough()
    {
        var args = Args.Parse(["App", "--", "--my-native", "x"]);

        var list = TestRunner.BuildArguments("App.csproj", args, trxArguments: ["--report-trx", "--report-trx-filename", "a.trx"]);

        Assert.True(list.IndexOf("--report-trx") < list.IndexOf("--my-native"));
        Assert.Equal(["--my-native", "x"], list.TakeLast(2));
        Assert.DoesNotContain("--report-trx", TestRunner.BuildArguments("App.csproj", args));
    }

    [Fact]
    public async Task Package_ids_are_read_when_a_config_override_skipped_them()
    {
        var config = ConfigWith("""{ "App/App.csproj": "nunit" }""");
        var d = await FrameworkDetector.DetectAsync(Path.Combine(_dir, "App", "App.csproj"), config);
        Assert.Null(d.PackageIds);

        var filled = await FrameworkDetector.WithPackageIdsAsync(d); // project does not exist: nothing found, but not null

        Assert.NotNull(filled.PackageIds);
    }

    [Fact]
    public void Each_run_gets_a_dated_folder_holding_log_and_trx_with_one_stem()
    {
        var a = TestRunner.NewArtifacts("/x/App.csproj", _dir);
        var b = TestRunner.NewArtifacts("/x/App.csproj", _dir);

        Assert.Equal(Path.GetDirectoryName(a.Log), Path.GetDirectoryName(a.Trx));
        Assert.Equal(Path.ChangeExtension(a.Log, ".trx"), a.Trx);
        Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), Path.GetFileName(Path.GetDirectoryName(a.Directory)));
        Assert.True(Directory.Exists(a.Directory));
        Assert.NotEqual(a.Directory, b.Directory); // same second: still separate folders
    }
}
