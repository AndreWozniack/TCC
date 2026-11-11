using System.Text.Json;

namespace AuthFix;

public record Alert(string Id, string Cwe, string File, int StartLine);

public static class AlertReader
{
    public static Alert Read(string json, string repository, IReadOnlyDictionary<string, string>? rules = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        string id;
        string cwe;
        string file;
        int line;

        if (root.TryGetProperty("version", out var version))
        {
            if (version.GetString() != "2.1.0")
                throw new ArgumentException("A versão SARIF deve ser 2.1.0.");
            var results = root.GetProperty("runs").EnumerateArray()
                .SelectMany(run => run.GetProperty("results").EnumerateArray()).ToArray();
            if (results.Length != 1)
                throw new ArgumentException("Informe exatamente um resultado SARIF.");
            var result = results[0];
            id = result.GetProperty("ruleId").GetString() ?? throw new ArgumentException("Regra ausente.");
            if (rules is null || !rules.TryGetValue(id, out cwe!))
                throw new ArgumentException("Regra SARIF sem mapeamento de CWE.");
            var locations = result.GetProperty("locations");
            if (locations.GetArrayLength() != 1)
                throw new ArgumentException("O alerta deve indicar uma localização.");
            var physical = locations[0].GetProperty("physicalLocation");
            var artifact = physical.GetProperty("artifactLocation");
            if (artifact.TryGetProperty("uriBaseId", out _))
                throw new ArgumentException("URI base SARIF não suportada.");
            file = Uri.UnescapeDataString(artifact.GetProperty("uri").GetString() ?? "");
            line = physical.GetProperty("region").GetProperty("startLine").GetInt32();
        }
        else
        {
            id = root.GetProperty("id").GetString() ?? "";
            cwe = root.GetProperty("cwe").GetString() ?? "";
            file = root.GetProperty("file").GetString() ?? "";
            line = root.GetProperty("startLine").GetInt32();
        }

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
