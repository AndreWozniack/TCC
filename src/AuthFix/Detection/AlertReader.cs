using System.Text.Json;

namespace AuthFix;

public static class AlertReader
{
    public static Alert Read(string json, string repository, IReadOnlyDictionary<string, string>? rules = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("version", out _))
            return Validate(
                root.GetProperty("id").GetString() ?? "",
                root.GetProperty("cwe").GetString() ?? "",
                root.GetProperty("file").GetString() ?? "",
                root.GetProperty("startLine").GetInt32(),
                repository);
        var results = SarifResults(root);
        if (results.Length != 1)
            throw new ArgumentException("Informe exatamente um resultado SARIF.");
        return FromSarifResult(results[0], repository, rules);
    }

    public static IReadOnlyList<Alert> ReadAll(string json, string repository, IReadOnlyDictionary<string, string>? rules = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("version", out _))
            return [Read(json, repository, rules)];
        return SarifResults(root).Select(result => FromSarifResult(result, repository, rules)).ToArray();
    }

    private static JsonElement[] SarifResults(JsonElement root)
    {
        if (root.GetProperty("version").GetString() != "2.1.0")
            throw new ArgumentException("A versão SARIF deve ser 2.1.0.");
        return root.GetProperty("runs").EnumerateArray()
            .SelectMany(run => run.GetProperty("results").EnumerateArray()).ToArray();
    }

    private static Alert FromSarifResult(JsonElement result, string repository, IReadOnlyDictionary<string, string>? rules)
    {
        var id = result.GetProperty("ruleId").GetString() ?? throw new ArgumentException("Regra ausente.");
        if (rules is null || !rules.TryGetValue(id, out var cwe))
            throw new ArgumentException($"Regra SARIF sem mapeamento de CWE: {id}.");
        var locations = result.GetProperty("locations");
        if (locations.GetArrayLength() != 1)
            throw new ArgumentException("O alerta deve indicar uma localização.");
        var physical = locations[0].GetProperty("physicalLocation");
        var artifact = physical.GetProperty("artifactLocation");
        if (artifact.TryGetProperty("uriBaseId", out _))
            throw new ArgumentException("URI base SARIF não suportada.");
        var file = Uri.UnescapeDataString(artifact.GetProperty("uri").GetString() ?? "");
        var line = physical.GetProperty("region").GetProperty("startLine").GetInt32();
        return Validate(id, cwe, file, line, repository);
    }

    private static Alert Validate(string id, string cwe, string file, int line, string repository)
    {
        if (string.IsNullOrWhiteSpace(id) || cwe is not ("CWE-862" or "CWE-863") || line < 1)
            throw new ArgumentException("Identificador, CWE ou linha inválida.");
        if (string.IsNullOrWhiteSpace(file) || Path.IsPathRooted(file) || Uri.TryCreate(file, UriKind.Absolute, out _))
            throw new ArgumentException("O caminho deve ser relativo ao repositório.");
        var relative = Path.GetRelativePath(Path.GetFullPath(repository), Path.GetFullPath(file, Path.GetFullPath(repository)));
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar))
            throw new ArgumentException("O caminho está fora do repositório.");
        return new Alert(id, cwe, relative, line);
    }
}
