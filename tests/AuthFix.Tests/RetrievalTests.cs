using AuthFix;
using Xunit;

namespace AuthFix.Tests;

public class RetrievalTests
{
    [Fact]
    public void FindsAuthorizationMechanismWithProvenance()
    {
        var evidence = ContextRetriever.Find(new Dictionary<string, string>
        {
            ["Authorization/DocumentHandler.cs"] = "class DocumentHandler : AuthorizationHandler<DocumentRequirement> { }",
            ["Other.cs"] = "class Other { }"
        });
        Assert.Single(evidence);
        Assert.Equal("Authorization/DocumentHandler.cs", evidence[0].File);
        Assert.Contains("DocumentHandler", evidence[0].Text);
        Assert.False(string.IsNullOrWhiteSpace(evidence[0].Reason));
    }

    [Fact]
    public void ConditionsHaveExpectedContext()
    {
        var alert = new Alert("1", "CWE-863", "Endpoint.cs", 3);
        var evidence = new[] { new Evidence("Policy.cs", "DocumentHandler", 1, "handler", "handler text") };
        var a = Experiment.BuildInput(alert, "method", "file", evidence, "A");
        var b = Experiment.BuildInput(alert, "method", "file", evidence, "B");
        var c = Experiment.BuildInput(alert, "method", "file", evidence, "C");
        Assert.Equal("method", a.Code);
        Assert.Equal("file", b.Code);
        Assert.Empty(b.Evidence);
        Assert.Single(c.Evidence);
        Assert.Equal(b.Code, c.Code);
    }
}
