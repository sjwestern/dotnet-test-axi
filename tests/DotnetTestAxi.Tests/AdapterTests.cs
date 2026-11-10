using DotnetTestAxi.Filtering;
using DotnetTestAxi.Frameworks.Adapters;
using Xunit;

namespace DotnetTestAxi.Tests;

public class AdapterTests
{
    private static TestFilter Filter(params string[] argv) => Args.Parse(["x", .. argv]).Filter;

    public static TheoryData<string, string[], string[]> Cases() => new()
    {
        { "xunit3", ["--class", "A.B.C"], ["--filter-class", "A.B.C"] },
        { "xunit3", ["--method", "A.B.C.M"], ["--filter-method", "A.B.C.M"] },
        { "xunit3", ["--namespace", "A.*"], ["--filter-namespace", "A.*"] },
        { "xunit3", ["--method", "*Fails"], ["--filter-method", "*Fails"] },
        { "xunit3", ["--namespace", "A", "--class", "A.C"], ["--filter-namespace", "A", "--filter-class", "A.C"] },
        { "xunit3", [], [] },

        { "mstest", ["--class", "A.B.C"], ["--filter", "FullyQualifiedName~A.B.C."] },
        { "mstest", ["--class", "A.B.C*"], ["--filter", "FullyQualifiedName~A.B.C"] },
        { "mstest", ["--method", "A.B.C.M"], ["--filter", "FullyQualifiedName=A.B.C.M"] },
        { "mstest", ["--method", "A.B.C.Th*"], ["--filter", "FullyQualifiedName~A.B.C.Th"] },
        { "mstest", ["--method", "*Fails"], ["--filter", "FullyQualifiedName~Fails"] },
        { "mstest", ["--namespace", "A.B"], ["--filter", "FullyQualifiedName~A.B."] },
        { "mstest", ["--namespace", "A*", "--class", "*C"], ["--filter", "FullyQualifiedName~A&FullyQualifiedName~C."] },
        { "mstest", [], [] },

        { "nunit", ["--class", "A.B.C"], ["--filter", "FullyQualifiedName~A.B.C."] },
        { "nunit", ["--method", "A.B.C.M"], ["--filter", "FullyQualifiedName=A.B.C.M"] },
        { "nunit", ["--namespace", "A.B", "--method", "*M"], ["--filter", "FullyQualifiedName~A.B.&FullyQualifiedName~M"] },

        { "tunit", ["--class", "A.B.C"], ["--treenode-filter", "/*/A.B/C/*"] },
        { "tunit", ["--class", "A.B.C*"], ["--treenode-filter", "/*/A.B/C*/*"] },
        { "tunit", ["--method", "A.B.C.M"], ["--treenode-filter", "/*/A.B/C/M"] },
        { "tunit", ["--method", "A.B.C.Th*"], ["--treenode-filter", "/*/A.B/C/Th*"] },
        { "tunit", ["--method", "*Fails"], ["--treenode-filter", "/*/*/*/*Fails"] },
        { "tunit", ["--namespace", "A.B"], ["--treenode-filter", "/*/A.B/*/*"] },
        { "tunit", ["--namespace", "A.*", "--method", "*M"], ["--treenode-filter", "/*/A.*/*/*M"] },
        { "tunit", [], [] },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Translates_filter(string id, string[] flags, string[] expected) =>
        Assert.Equal(expected, AdapterRegistry.All.Single(a => a.Id == id).BuildFilterArguments(Filter(flags)));

    [Theory]
    [InlineData("xunit3", "xunit.v3")]
    [InlineData("xunit3", "xunit.v3.mtp-v2")]
    [InlineData("mstest", "MSTest")]
    [InlineData("mstest", "MSTest.TestFramework")]
    [InlineData("nunit", "NUnit")]
    [InlineData("tunit", "TUnit")]
    public void Matches_package_ids(string id, string package) =>
        Assert.Equal(id, AdapterRegistry.Find(["Other", package])?.Id);

    [Fact]
    public void Unknown_packages_match_nothing()
    {
        Assert.Null(AdapterRegistry.Find(["Newtonsoft.Json", "NUnit.Analyzers"]));
        Assert.Equal(["xunit3", "mstest", "nunit", "tunit"], AdapterRegistry.All.Select(a => a.Id));
    }
}
