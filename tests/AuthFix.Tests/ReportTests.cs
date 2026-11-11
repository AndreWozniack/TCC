using AuthFix;
using Xunit;

namespace AuthFix.Tests;

public class ReportTests
{
    [Fact]
    public void SimulatedResponseIsNotMarkedAsValidated()
    {
        var input = Experiment.BuildInput(new Alert("1", "CWE-863", "Api.cs", 1), "method", "file", [], "C");
        var report = MockGenerator.Generate(input, new MockResponse("diff", "explicação", []));
        Assert.Equal("simulado", report.Provider);
        Assert.Equal("não executada", report.Validation);
        Assert.Equal("diff", report.Proposal.Diff);
    }
}
