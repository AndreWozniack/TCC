using System.Text;
using System.Text.Json;

namespace AuthFix;

public static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string ToMarkdown(RunReport report) => new StringBuilder()
        .AppendLine("# Relatório do AuthFix")
        .AppendLine()
        .AppendLine($"Condição: {report.Input.Condition}")
        .AppendLine($"Alerta: {report.Input.Alert.Id} ({report.Input.Alert.Cwe})")
        .AppendLine($"Local: {report.Input.Alert.File}:{report.Input.Alert.StartLine}")
        .AppendLine($"Gerador: {report.Provider}")
        .AppendLine($"Validação: {report.Validation}")
        .AppendLine($"Compilação: {report.Results?.Build ?? "não executada"}")
        .AppendLine($"Segurança: {report.Results?.Security ?? "não executada"}")
        .AppendLine($"Funcionalidade: {report.Results?.Functionality ?? "não executada"}")
        .AppendLine($"Aderência: {report.Results?.Architecture ?? "revisão humana pendente"}")
        .AppendLine()
        .AppendLine("## Justificativa")
        .AppendLine(report.Proposal.Explanation)
        .AppendLine()
        .AppendLine("## Diff")
        .AppendLine("```diff")
        .AppendLine(report.Proposal.Diff)
        .AppendLine("```")
        .ToString();

    public static string WriteArtifacts(string outputDirectory, string prefix, RunReport report)
    {
        var reportPath = Path.Combine(outputDirectory, prefix + "relatorio.json");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.ChangeExtension(reportPath, ".md"), ToMarkdown(report), Encoding.UTF8);
        var patchPath = Path.Combine(outputDirectory, prefix + "correcao.patch");
        File.WriteAllText(patchPath, report.Proposal.Diff, Encoding.UTF8);
        return patchPath;
    }
}
