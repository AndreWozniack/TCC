using System.Text;
using System.Text.Json;
using AuthFix;

Console.OutputEncoding = Encoding.UTF8;
if (args.Length == 0 || args[0] is "-h" or "--help" or "ajuda")
{
    Console.WriteLine(ScanOptions.Usage);
    return args.Length == 0 ? 2 : 0;
}
if (args[0] != "scan")
{
    Console.Error.WriteLine($"Comando desconhecido: {args[0]}\n\n{ScanOptions.Usage}");
    return 2;
}

try
{
    var options = ScanOptions.Parse(args[1..]);
    var repository = GitRepository.Execute(Directory.GetCurrentDirectory(), "rev-parse", "--show-toplevel").Trim();
    var output = Path.GetFullPath(options.Output, repository);
    Directory.CreateDirectory(output);

    Title($"AuthFix · {repository}");
    var evaluator = Path.GetFullPath(options.Evaluator, repository);
    string sarif;
    IReadOnlyDictionary<string, string> rules;
    if (options.Alert is null)
    {
        Step("1. Análise estática (Semgrep em contêiner, regras do AuthFix)");
        sarif = SemgrepRunner.Scan(repository, output);
        rules = SemgrepRunner.Rules;
    }
    else
    {
        Step($"1. Alerta informado: {options.Alert}");
        sarif = File.ReadAllText(Path.GetFullPath(options.Alert));
        rules = options.Rules is null ? SemgrepRunner.Rules
            : JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.GetFullPath(options.Rules)))
              ?? throw new ArgumentException("Mapa de regras vazio.");
    }
    File.WriteAllText(Path.Combine(output, "alerta.sarif"), sarif, Encoding.UTF8);
    var alerts = AlertReader.ReadAll(sarif, repository, rules);
    if (alerts.Count == 0)
    {
        Console.WriteLine("Nenhuma falha de autorização encontrada.");
        return 0;
    }
    foreach (var alert in alerts)
        Console.WriteLine($"  {alert.Id} ({alert.Cwe})  {alert.File}:{alert.StartLine}");

    string? commit = null;
    if (File.Exists(Path.Combine(evaluator, "teste.sh")))
        commit = GitRepository.ResolveCleanCommit(repository, "HEAD");
    else
        Console.WriteLine($"Sem {Path.GetRelativePath(repository, evaluator)}/teste.sh: a validação não será executada.");

    var generator = GeneratorFactory.Create(repository, options);

    var failures = 0;
    for (var i = 0; i < alerts.Count; i++)
    {
        var alert = alerts[i];
        var prefix = alerts.Count == 1 ? "" : $"{i + 1}-";
        Step($"2. Correção de {alert.File}:{alert.StartLine} (condição {options.Condition})");
        var report = Pipeline.Run(repository, alert, options.Condition, generator, commit, commit is null ? null : evaluator);

        if (report.Input.Evidence.Count > 0)
        {
            Console.WriteLine("Contexto recuperado:");
            foreach (var evidence in report.Input.Evidence)
                Console.WriteLine($"  - {evidence.File}:{evidence.Line} {evidence.Symbol} — {evidence.Reason}");
        }
        Console.WriteLine($"Gerador: {report.Provider}");
        Console.WriteLine($"\nJustificativa: {report.Proposal.Explanation}\n");
        Console.WriteLine(report.Proposal.Diff.TrimEnd());

        Step("3. Validação (contêiner sem rede)");
        Console.WriteLine($"Validação:      {report.Validation}");
        Console.WriteLine($"Compilação:     {report.Results?.Build ?? "não executada"}");
        Console.WriteLine($"Segurança:      {report.Results?.Security ?? "não executada"}");
        Console.WriteLine($"Funcionalidade: {report.Results?.Functionality ?? "não executada"}");
        Console.WriteLine($"Aderência:      {report.Results?.Architecture ?? "revisão humana pendente"}");

        var patch = ReportWriter.WriteArtifacts(output, prefix, report);

        var approved = report.Results is { Security: "aprovada", Functionality: "aprovada" };
        if (!approved) failures++;
        if (options.Apply)
        {
            if (!approved)
                Console.WriteLine("\nCorreção não aplicada: a validação não aprovou segurança e funcionalidade.");
            else
            {
                GitRepository.Execute(repository, "apply", "--", patch);
                Console.WriteLine($"\nCorreção aplicada em {alert.File}. Revise com: git diff");
            }
        }
        else if (approved)
            Console.WriteLine($"\nPara aplicar: authfix scan --aplicar   (ou: git apply {Path.GetRelativePath(repository, patch)})");
    }
    Console.WriteLine($"\nRelatórios em {Path.GetRelativePath(repository, output)}/");
    return failures == 0 ? 0 : 3;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or JsonException or KeyNotFoundException or InvalidOperationException or System.ComponentModel.Win32Exception)
{
    Console.Error.WriteLine("Erro: " + ex.Message);
    return 1;
}

static void Title(string text) => Console.WriteLine($"\u001b[1m{text}\u001b[0m");
static void Step(string text) => Console.WriteLine($"\n\u001b[1;36m==== {text} ====\u001b[0m");
