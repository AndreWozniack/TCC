using AuthFix;
using Xunit;

namespace AuthFix.Tests;

public class ContextEdgeTests
{
    [Fact]
    public void FindsTopLevelPolicyRegistration()
    {
        var result = ContextRetriever.Find(new Dictionary<string, string>
        {
            ["Program.cs"] = "builder.Services.AddAuthorization(options => options.FallbackPolicy = null);"
        });
        Assert.Contains(result, e => e.File == "Program.cs" && e.Text.Contains("AddAuthorization"));
    }

    [Fact]
    public void RejectsInvalidCondition()
    {
        Assert.Throws<ArgumentException>(() => Experiment.BuildInput(new("x", "CWE-862", "x.cs", 1), "", "", [], "D"));
    }
}

public class MinimalApiTests
{
    [Fact]
    public void LocatesMinimalApiHandler()
    {
        const string source = "var app = builder.Build();\napp.MapGet(\"/items\", () => Results.Ok());\napp.Run();";
        Assert.Contains("MapGet", AuthorizationAnalyzer.FindEndpoint(source, 2));
    }
}
