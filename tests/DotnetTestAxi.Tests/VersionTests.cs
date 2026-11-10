using System.Text.RegularExpressions;
using Xunit;

namespace DotnetTestAxi.Tests;

public class VersionTests
{
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DotnetTestAxi.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }

    private static string Read(params string[] path) => File.ReadAllText(Path.Combine([Root(), .. path]));

    private static string ToolVersion() =>
        Regex.Match(Read("Directory.Build.props"), "<Version>([^<]+)</Version>").Groups[1].Value;

    [Fact]
    public void Skill_version_matches_tool_version()
    {
        var skill = Regex.Match(Read("skills", "dotnet-test-axi", "SKILL.md"), @"(?m)^version:\s*""?([^""\r\n]+)""?\s*$").Groups[1].Value;
        Assert.Equal(ToolVersion(), skill);
    }

    [Fact]
    public void Changelog_has_a_section_for_the_tool_version()
    {
        Assert.Matches($@"(?m)^## \[{Regex.Escape(ToolVersion())}\]", Read("CHANGELOG.md"));
    }
}
