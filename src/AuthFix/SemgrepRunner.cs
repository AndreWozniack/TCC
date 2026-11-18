using System.Diagnostics;
using System.Text.Json.Nodes;

namespace AuthFix;

public static class SemgrepRunner
{
    public const string Image = "semgrep/semgrep";

    /// <summary>Regras embutidas na ferramenta e a CWE associada a cada uma.</summary>
    public static readonly IReadOnlyDictionary<string, string> Rules = new Dictionary<string, string>
    {
        ["aspnet-controller-sem-autorizacao"] = "CWE-862",
        ["aspnet-minimal-api-sem-autorizacao"] = "CWE-862",
    };

    /// <summary>Executa o Semgrep em contêiner sobre o repositório e devolve o SARIF já normalizado.</summary>
    public static string Scan(string repository, string outputDirectory)
    {
        var rules = Path.Combine(Path.GetTempPath(), "authfix-regras-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rules);
        try
        {
            var assembly = typeof(SemgrepRunner).Assembly;
            foreach (var name in assembly.GetManifestResourceNames().Where(name => name.EndsWith(".yml", StringComparison.Ordinal)))
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var file = File.Create(Path.Combine(rules, name[(name.LastIndexOf(".Regras.", StringComparison.Ordinal) + 8)..]));
                stream.CopyTo(file);
            }
            var output = Path.GetFullPath(outputDirectory);
            var info = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[]
            {
                "run", "--rm",
                "--mount", $"type=bind,src={repository},dst=/src,readonly",
                "--mount", $"type=bind,src={rules},dst=/regras,readonly",
                "--mount", $"type=bind,src={output},dst=/saida",
                "-w", "/src", Image,
                "semgrep", "scan", "--metrics=off", "--quiet", "--config", "/regras", "--sarif-output=/saida/semgrep.sarif"
            }) info.ArgumentList.Add(argument);
            using var process = Process.Start(info) ?? throw new InvalidOperationException("Docker indisponível.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            stdout.GetAwaiter().GetResult();
            var sarif = Path.Combine(output, "semgrep.sarif");
            if (!File.Exists(sarif))
                throw new InvalidOperationException("O Semgrep não gerou SARIF. Verifique se o Docker está em execução.\n" + stderr.Trim());
            return Normalize(File.ReadAllText(sarif));
        }
        finally
        {
            Directory.Delete(rules, true);
        }
    }

    /// <summary>Remove o uriBaseId e o prefixo de pasta que o Semgrep coloca no ruleId.</summary>
    public static string Normalize(string sarif)
    {
        var root = JsonNode.Parse(sarif) ?? throw new ArgumentException("SARIF vazio.");
        foreach (var run in root["runs"]?.AsArray() ?? [])
        foreach (var result in run?["results"]?.AsArray() ?? [])
        {
            if (result?["ruleId"]?.GetValue<string>() is { } id)
                result["ruleId"] = id[(id.LastIndexOf('.') + 1)..];
            foreach (var location in result?["locations"]?.AsArray() ?? [])
                (location?["physicalLocation"]?["artifactLocation"] as JsonObject)?.Remove("uriBaseId");
        }
        return root.ToJsonString(new() { WriteIndented = true });
    }
}
