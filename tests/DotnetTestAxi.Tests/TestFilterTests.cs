using Xunit;
using DotnetTestAxi.Filtering;

namespace DotnetTestAxi.Tests;

public class TestFilterTests
{
    [Fact]
    public void Empty_filter_when_no_flags()
    {
        var a = Args.Parse(["x"]);
        Assert.Null(a.Error);
        Assert.True(a.Filter.IsEmpty);
    }

    [Fact]
    public void Qualified_values_parse()
    {
        var a = Args.Parse(["x", "--namespace", "MyApp.Tests", "--class", "MyApp.Tests.C", "--method", "MyApp.Tests.C.M"]);
        Assert.Null(a.Error);
        Assert.False(a.Filter.IsEmpty);
        Assert.Equal("MyApp.Tests.C.M", a.Filter.Method!.Value.Text);
    }

    [Fact]
    public void Wildcards_at_ends_are_allowed()
    {
        var a = Args.Parse(["x", "--method", "*Fails", "--class", "MyApp.*", "--namespace", "*Tests*"]);
        Assert.Null(a.Error);
        Assert.True(a.Filter.Method!.Value.LeadingWildcard);
        Assert.True(a.Filter.Class!.Value.TrailingWildcard);
        Assert.True(a.Filter.Namespace!.Value is { LeadingWildcard: true, TrailingWildcard: true, Text: "Tests" });
    }

    [Theory]
    [InlineData("--class", "My*Class")]
    [InlineData("--namespace", "A.*.B")]
    [InlineData("--method", "A.*.M")]
    [InlineData("--class", "*")]
    [InlineData("--class", "")]
    public void Bad_wildcards_are_rejected(string flag, string value)
    {
        var a = Args.Parse(["x", flag, value]);
        Assert.NotNull(a.Error);
        Assert.NotNull(a.ErrorHelp);
    }

    [Fact]
    public void Unqualified_method_is_rejected()
    {
        var a = Args.Parse(["x", "--method", "Fails"]);
        Assert.Contains("qualified", a.Error);
        Assert.Contains("--method", a.ErrorHelp);
    }
}
