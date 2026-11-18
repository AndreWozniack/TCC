using AuthFix;
using Xunit;

namespace AuthFix.Tests;

public class ReportTests
{
    [Fact]
    public void SimulatedResponseIsNotMarkedAsValidated()
    {
        var input = Experiment.BuildInput(new Alert("1", "CWE-863", "Api.cs", 1), "method", "file", [], "C");
        IFixGenerator generator = new MockGenerator(new MockResponse("diff", "explicação", []));
        var report = generator.Generate(input);
        Assert.Equal("simulado", report.Provider);
        Assert.Equal("não executada", report.Validation);
        Assert.Equal("diff", report.Proposal.Diff);
    }

    [Fact]
    public void ReportWriterFormatsMarkdownSummary()
    {
        var input = Experiment.BuildInput(new Alert("aspnet-controller-sem-autorizacao", "CWE-862", "Api.cs", 10), "method", "file", [], "C");
        var report = new RunReport(input, new MockResponse("diff --git a/Api.cs b/Api.cs", "Falta policy", ["Program.cs"]), "simulado", "não executada");
        var markdown = ReportWriter.ToMarkdown(report);
        Assert.Contains("# Relatório do AuthFix", markdown);
        Assert.Contains("Alerta: aspnet-controller-sem-autorizacao (CWE-862)", markdown);
        Assert.Contains("Falta policy", markdown);
    }
}
