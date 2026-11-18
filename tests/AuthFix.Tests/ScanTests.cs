using AuthFix;
using Xunit;

namespace AuthFix.Tests;

public class ScanTests
{
    [Fact]
    public void DefaultsNeedNoArguments()
    {
        var options = ScanOptions.Parse([]);
        Assert.Equal("C", options.Condition);
        Assert.Null(options.Alert);
        Assert.Equal("resposta-simulada.json", options.Response);
        Assert.Equal("avaliacao", options.Evaluator);
        Assert.False(options.Apply);
    }

    [Fact]
    public void ParsesOptions()
    {
        var options = ScanOptions.Parse(["--condicao", "b", "--alerta", "x.sarif", "--aplicar"]);
        Assert.Equal("B", options.Condition);
        Assert.Equal("x.sarif", options.Alert);
        Assert.True(options.Apply);
    }

    [Theory]
    [InlineData("--condicao", "D")]
    [InlineData("--desconhecida")]
    [InlineData("--alerta")]
    public void RejectsInvalidOptions(params string[] args)
    {
        Assert.Throws<ArgumentException>(() => ScanOptions.Parse(args));
    }

    [Fact]
    public void NormalizesSemgrepSarifAndReadsAllResults()
    {
        const string sarif = """
            {"version":"2.1.0","runs":[{"results":[
              {"ruleId":"regras.aspnet-controller-sem-autorizacao","locations":[{"physicalLocation":{"artifactLocation":{"uri":"Controllers/A.cs","uriBaseId":"%SRCROOT%"},"region":{"startLine":10}}}]},
              {"ruleId":"tmp.x.aspnet-minimal-api-sem-autorizacao","locations":[{"physicalLocation":{"artifactLocation":{"uri":"Program.cs","uriBaseId":"%SRCROOT%"},"region":{"startLine":17}}}]}
            ]}]}
            """;
        var alerts = AlertReader.ReadAll(SemgrepRunner.Normalize(sarif), Path.GetTempPath(), SemgrepRunner.Rules);
        Assert.Equal(2, alerts.Count);
        Assert.Equal("aspnet-controller-sem-autorizacao", alerts[0].Id);
        Assert.Equal("CWE-862", alerts[1].Cwe);
        Assert.Equal(17, alerts[1].StartLine);
    }

    [Fact]
    public void EmptySarifHasNoAlerts()
    {
        const string sarif = """{"version":"2.1.0","runs":[{"results":[]}]}""";
        Assert.Empty(AlertReader.ReadAll(sarif, Path.GetTempPath(), SemgrepRunner.Rules));
    }

    [Fact]
    public void RulesAreEmbeddedInTheTool()
    {
        var names = typeof(SemgrepRunner).Assembly.GetManifestResourceNames();
        foreach (var rule in SemgrepRunner.Rules.Keys)
            Assert.Contains(names, name => name.EndsWith($".Regras.{rule}.yml", StringComparison.Ordinal));
    }
}
