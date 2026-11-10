using Xunit;

namespace DotnetTestAxi.Tests;

public class ArgsAndConfigTests
{
    [Fact]
    public void Args_parse_target_filters_and_passthrough()
    {
        var a = Args.Parse(["unit", "--class", "Ns.C", "--full", "--", "--coverage"]);

        Assert.Equal("unit", a.Target);
        Assert.Equal("Ns.C", a.Class);
        Assert.True(a.Full);
        Assert.Equal(["--coverage"], a.Passthrough);
        Assert.Null(a.Error);
    }

    [Fact]
    public void Unknown_option_is_an_error()
    {
        Assert.Equal("unknown flag --nope", Args.Parse(["--nope"]).Error);
        Assert.Contains("--class", Args.ValidFlags);
    }

    [Fact]
    public void Project_target_maps_to_project_flag_and_solution_to_solution_flag()
    {
        var args = Args.Parse(["x", "--method", "M", "--no-build"]);

        var project = TestRunner.BuildArguments("/r/tests/Unit.csproj", args);
        var solution = TestRunner.BuildArguments("/r/All.slnx", args);

        Assert.Equal(["test", "--project", "/r/tests/Unit.csproj", "--filter-method", "M", "--no-build", "--no-progress"], project);
        Assert.Equal("--solution", solution[1]);
    }

    [Fact]
    public void Config_is_found_by_walking_up_and_targets_resolve_relative_to_it()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var nested = Directory.CreateDirectory(Path.Combine(root, "a", "b")).FullName;
            File.WriteAllText(
                Path.Combine(root, Config.FileName),
                "{ // comment\n \"targets\": { \"Unit\": \"tests/Unit\" }, \"logDir\": \".logs\" }");

            var config = Config.Find(nested);

            Assert.NotNull(config);
            Assert.Equal(".logs", config.LogDir);
            Assert.Equal(Path.GetFullPath("tests/Unit", root), Config.ResolveTarget(config, "unit", nested));
            Assert.Null(Config.ResolveTarget(config, "missing", nested));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
