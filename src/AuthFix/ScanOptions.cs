namespace AuthFix;

public sealed record ScanOptions(
    string Condition = "C",
    string? Alert = null,
    string? Rules = null,
    string Response = "resposta-simulada.json",
    string Evaluator = "avaliacao",
    string Output = ".authfix",
    bool Apply = false)
{
    public const string Usage = """
        Uso: authfix scan [opções]

        Executa na raiz do repositório Git do projeto ASP.NET Core:
          1. Semgrep (Docker) com as regras de autorização do AuthFix;
          2. localização do endpoint (Roslyn) e recuperação de contexto;
          3. proposta de correção (resposta simulada até a integração do LLM);
          4. validação do patch em contêiner sem rede, com <avaliacao>/teste.sh.

        Opções:
          --condicao A|B|C   contexto enviado ao gerador (padrão: C)
          --alerta ARQ       usa um SARIF existente em vez de rodar o Semgrep
          --regras ARQ       mapa JSON regra -> CWE para o SARIF informado
          --resposta ARQ     proposta simulada (padrão: resposta-simulada.json)
          --avaliacao DIR    pasta com teste.sh (padrão: avaliacao)
          --saida DIR        relatórios e patch (padrão: .authfix)
          --aplicar          aplica a correção se segurança e funcionalidade forem aprovadas
        """;

    public static ScanOptions Parse(IReadOnlyList<string> args)
    {
        var options = new ScanOptions();
        for (var i = 0; i < args.Count; i++)
        {
            string Value() => i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i] : throw new ArgumentException($"A opção {args[i]} exige um valor.");
            options = args[i] switch
            {
                "--condicao" => options with { Condition = Value().ToUpperInvariant() },
                "--alerta" => options with { Alert = Value() },
                "--regras" => options with { Rules = Value() },
                "--resposta" => options with { Response = Value() },
                "--avaliacao" => options with { Evaluator = Value() },
                "--saida" => options with { Output = Value() },
                "--aplicar" => options with { Apply = true },
                _ => throw new ArgumentException($"Opção desconhecida: {args[i]}")
            };
        }
        if (options.Condition is not ("A" or "B" or "C"))
            throw new ArgumentException("Condição deve ser A, B ou C.");
        return options;
    }
}
