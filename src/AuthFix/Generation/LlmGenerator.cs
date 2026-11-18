using System.Text;
using System.Text.Json;

namespace AuthFix;

public sealed class LlmGenerator(
    ILlmProvider provider,
    Func<string, string> readOriginalFile,
    IFixGenerator? fallback = null) : IFixGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string SystemPrompt = """
        Você é um especialista em segurança de aplicações ASP.NET Core focado na correção de falhas de autorização (CWE-862: Missing Authorization e CWE-863: Incorrect Authorization).
        Sua tarefa é corrigir o código fornecido aplicando a autorização adequada ao endpoint sinalizado pelo alerta.

        Regras obrigatórias:
        1. Reutilize políticas ([Authorize(Policy = "...")], RequireAuthorization("...")), roles ou handlers já existentes nas evidências do projeto quando disponíveis.
        2. Caso não haja evidências de policy específica, aplique [Authorize] (em Controllers) ou .RequireAuthorization() (em Minimal APIs).
        3. Devolva o código corrigido correspondente EXATAMENTE ao escopo enviado em <codigo_alvo> (se recebeu apenas um método, devolva apenas o método corrigido; se recebeu o arquivo completo, devolva o arquivo completo corrigido), removendo comentários que indiquem falha proposital.
        4. Responda EXCLUSIVAMENTE com um objeto JSON válido (sem markdown ao redor), no formato:
        {
          "fixedCode": "código corrigido correspondente a <codigo_alvo>",
          "explanation": "justificativa técnica concisa em português explicando a falha e a correção aplicada",
          "references": ["lista de arquivos das evidências utilizadas, ou vazia"]
        }
        """;

    public RunReport Generate(GenerationInput input)
    {
        try
        {
            var userPrompt = BuildUserPrompt(input);
            var rawResponse = provider.Complete(SystemPrompt, userPrompt);
            var jsonText = ExtractJson(rawResponse);
            var parsed = JsonSerializer.Deserialize<LlmPayload>(jsonText, JsonOptions)
                ?? throw new InvalidOperationException("Resposta JSON do LLM vazia.");

            string diff;
            if (!string.IsNullOrWhiteSpace(parsed.FixedCode))
            {
                var fullFile = readOriginalFile(input.Alert.File);
                diff = DiffBuilder.Build(input.Alert.File, fullFile, input.Code, parsed.FixedCode);
            }
            else if (!string.IsNullOrWhiteSpace(parsed.Diff))
            {
                diff = parsed.Diff;
            }
            else
            {
                throw new InvalidOperationException("O LLM não retornou fixedCode nem diff.");
            }

            var proposal = new MockResponse(
                diff,
                parsed.Explanation ?? "Correção gerada via LLM.",
                parsed.References ?? []);

            return new(input, proposal, provider.Name, "não executada");
        }
        catch (Exception) when (fallback is not null)
        {
            return fallback.Generate(input);
        }
    }

    public static string BuildUserPrompt(GenerationInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Alerta: {input.Alert.Id} ({input.Alert.Cwe})");
        sb.AppendLine($"Arquivo: {input.Alert.File}:{input.Alert.StartLine}");
        sb.AppendLine($"Condição experimental: {input.Condition}");
        sb.AppendLine();
        sb.AppendLine("<codigo_alvo>");
        sb.AppendLine(input.Code);
        sb.AppendLine("</codigo_alvo>");

        if (input.Evidence.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<evidencias_projeto>");
            foreach (var evidence in input.Evidence)
            {
                sb.AppendLine($"--- {evidence.File}:{evidence.Line} ({evidence.Symbol} - {evidence.Reason}) ---");
                sb.AppendLine(evidence.Text);
            }
            sb.AppendLine("</evidencias_projeto>");
        }

        return sb.ToString();
    }

    public static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            return trimmed[start..(end + 1)];
        return trimmed;
    }

    private sealed record LlmPayload(string? FixedCode, string? Diff, string? Explanation, IReadOnlyList<string>? References);
}
