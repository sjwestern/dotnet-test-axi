using DotnetTestAxi.Frameworks;
using DotnetTestAxi.Frameworks.Adapters;
using Xunit;

namespace DotnetTestAxi.Tests;

public class RunPlanTests
{
    private static Detection D(string project, string? id) =>
        new(project, id is null ? null : AdapterRegistry.All.Single(a => a.Id == id), "test");

    [Fact]
    public void Empty_filter_is_unfiltered_even_with_unknown_projects()
    {
        var plan = RunPlan.Choose(true, [D("A.csproj", null), D("B.csproj", "mstest")]);
        Assert.Equal(Strategy.Unfiltered, plan.Strategy);
        Assert.Null(plan.Adapter);
    }

    [Fact]
    public void One_framework_runs_once_with_its_adapter()
    {
        var plan = RunPlan.Choose(false, [D("A.csproj", "nunit"), D("B.csproj", "nunit")]);
        Assert.Equal(Strategy.Single, plan.Strategy);
        Assert.Equal("nunit", plan.Adapter!.Id);
    }

    [Fact]
    public void Several_frameworks_are_mixed()
    {
        var plan = RunPlan.Choose(false, [D("A.csproj", "nunit"), D("B.csproj", "tunit")]);
        Assert.Equal(Strategy.Mixed, plan.Strategy);
    }

    [Fact]
    public void Any_unknown_project_makes_the_plan_unknown()
    {
        Assert.Equal(Strategy.Unknown, RunPlan.Choose(false, [D("A.csproj", "nunit"), D("B.csproj", null)]).Strategy);
        Assert.Equal(Strategy.Unknown, RunPlan.Choose(false, []).Strategy);
    }

    [Fact]
    public void Unfiltered_arguments_carry_no_filter()
    {
        var args = Args.Parse(["x", "--no-build"]);
        Assert.Equal(["test", "--project", "/r/A.csproj", "--no-build", "--no-progress"], TestRunner.BuildArguments("/r/A.csproj", args));
    }

    [Fact]
    public void Filtered_arguments_use_the_adapter()
    {
        var args = Args.Parse(["x", "--class", "A.C"]);
        var tunit = AdapterRegistry.All.Single(a => a.Id == "tunit");
        Assert.Equal(["test", "--solution", "/r/All.slnx", "--treenode-filter", "/*/A/C/*", "--no-progress"], TestRunner.BuildArguments("/r/All.slnx", args, tunit));
    }
}
