using AuthFix;
using Xunit;

namespace AuthFix.Tests;

public class AlertReaderTests
{
    [Fact]
    public void ManifestAndSarifResolveSameLocation()
    {
        const string manifest = """{"id":"1","cwe":"CWE-862","file":"Api.cs","startLine":4}""";
        const string sarif = """{"version":"2.1.0","runs":[{"results":[{"ruleId":"missing","locations":[{"physicalLocation":{"artifactLocation":{"uri":"Api.cs"},"region":{"startLine":4}}}]}]}]}""";
        var root = Path.GetTempPath();
        var a = AlertReader.Read(manifest, root);
        var b = AlertReader.Read(sarif, root, new Dictionary<string, string> { ["missing"] = "CWE-862" });
        Assert.Equal(a.File, b.File);
        Assert.Equal(a.StartLine, b.StartLine);
        Assert.Equal(a.Cwe, b.Cwe);
    }

    [Theory]
    [InlineData("""{"id":"1","cwe":"CWE-862","file":"../outside.cs","startLine":4}""")]
    [InlineData("""{"id":"1","cwe":"CWE-89","file":"Api.cs","startLine":4}""")]
    [InlineData("""{"id":"1","cwe":"CWE-862","file":"Api.cs","startLine":0}""")]
    public void RejectsInvalidInputs(string input)
    {
        Assert.ThrowsAny<Exception>(() => AlertReader.Read(input, Path.GetTempPath()));
    }

    [Fact]
    public void FindsMethodContainingReportedLine()
    {
        const string source = "class Controller {\n [Authorize]\n public void Update() {}\n}";
        var endpoint = AuthorizationAnalyzer.FindEndpoint(source, 3);
        Assert.Contains("Update", endpoint);
        Assert.Contains("Authorize", endpoint);
    }
}
