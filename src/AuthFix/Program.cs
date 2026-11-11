using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AuthFix;

if (args.Length < 5)
{
    Console.Error.WriteLine("Uso: AuthFix <repositório> <alerta.json> <A|B|C> <resposta-simulada.json> <relatório.json> [--rules regras.json] [--validate commit pasta-avaliacao]");
    return 2;
}

try
{
    if (!string.Equals(Path.GetExtension(args[4]), ".json", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("O relatório deve ter extensão .json.");
    var repository = Path.GetFullPath(args[0]);
    Dictionary<string, string>? rules = null;
    string? revision = null;
    string? evaluator = null;
    for (var i = 5; i < args.Length; i++)
    {
        if (args[i] == "--rules" && i + 1 < args.Length)
            rules = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(args[++i]));
        else if (args[i] == "--validate" && i + 2 < args.Length)
        {
            revision = args[++i];
            evaluator = args[++i];
        }
        else throw new ArgumentException("Opção inválida ou incompleta.");
    }
    if (revision is not null)
    {
        var resolve = new ProcessStartInfo("git") { WorkingDirectory = repository, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "rev-parse", "--verify", "--end-of-options", revision + "^{commit}" }) resolve.ArgumentList.Add(argument);
        using var resolver = Process.Start(resolve) ?? throw new InvalidOperationException("Git indisponível.");
        var commit = resolver.StandardOutput.ReadToEnd().Trim();
        var resolveError = resolver.StandardError.ReadToEnd();
        resolver.WaitForExit();
        if (resolver.ExitCode != 0)
            throw new ArgumentException("Commit inválido: " + resolveError);
        var verify = new ProcessStartInfo("git") { WorkingDirectory = repository };
        foreach (var argument in new[] { "diff", "--quiet", "--no-ext-diff", commit, "--" }) verify.ArgumentList.Add(argument);
        using var check = Process.Start(verify) ?? throw new InvalidOperationException("Git indisponível.");
        check.WaitForExit();
        if (check.ExitCode != 0)
            throw new ArgumentException("O checkout deve corresponder ao commit usado na validação.");
        revision = commit;
    }
    var alert = AlertReader.Read(File.ReadAllText(args[1]), repository, rules);
    var current = repository;
    foreach (var part in alert.File.Split(Path.DirectorySeparatorChar))
    {
        current = Path.Combine(current, part);
        if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            throw new ArgumentException("Caminhos com links simbólicos não são suportados.");
    }

    var source = File.ReadAllText(current);
    var method = AuthorizationAnalyzer.FindEndpoint(source, alert.StartLine);
    IReadOnlyList<Evidence> evidence = Array.Empty<Evidence>();
    if (args[2] == "C")
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = repository,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("ls-files");
        start.ArgumentList.Add("-z");
        using var git = Process.Start(start) ?? throw new InvalidOperationException("Git indisponível.");
        var paths = git.StandardOutput.ReadToEnd().Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var error = git.StandardError.ReadToEnd();
        git.WaitForExit();
        if (git.ExitCode != 0)
            throw new InvalidOperationException(error);
        var files = new Dictionary<string, string>();
        foreach (var path in paths.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.Equals(path, alert.File.Replace(Path.DirectorySeparatorChar, '/'), StringComparison.Ordinal))
                continue;
            if (path.Split('/').Any(part => (part.Contains("test", StringComparison.OrdinalIgnoreCase) || part.Equals("avaliacao", StringComparison.OrdinalIgnoreCase))))
                continue;
            var full = Path.Combine(repository, path);
            var components = path.Split('/');
            var parent = repository;
            var hasLink = false;
            foreach (var component in components)
            {
                parent = Path.Combine(parent, component);
                if (!File.Exists(parent) && !Directory.Exists(parent)) { hasLink = true; break; }
                if (File.GetAttributes(parent).HasFlag(FileAttributes.ReparsePoint)) { hasLink = true; break; }
            }
            if (!hasLink) files[path] = File.ReadAllText(full);
        }
        evidence = ContextRetriever.Find(files);
    }

    var input = Experiment.BuildInput(alert, method, source, evidence, args[2]);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
    var response = JsonSerializer.Deserialize<MockResponse>(File.ReadAllText(args[3]), options)
        ?? throw new ArgumentException("Resposta simulada vazia.");
    var report = MockGenerator.Generate(input, response);
    if (revision is not null && evaluator is not null)
    {
        var result = PatchValidator.Validate(repository, revision, alert.File, evaluator, response.Diff);
        report = report with { Validation = result.Status, Results = result };
    }
    File.WriteAllText(args[4], JsonSerializer.Serialize(report, options), Encoding.UTF8);
    var markdown = new StringBuilder()
        .AppendLine("# Relatório da execução simulada")
        .AppendLine()
        .AppendLine($"Condição: {report.Input.Condition}")
        .AppendLine($"Alerta: {report.Input.Alert.Id} ({report.Input.Alert.Cwe})")
        .AppendLine($"Local: {report.Input.Alert.File}:{report.Input.Alert.StartLine}")
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
        .AppendLine("```");
    File.WriteAllText(Path.ChangeExtension(args[4], ".md"), markdown.ToString(), Encoding.UTF8);
    Console.WriteLine($"Relatório gerado: {args[4]}");
    return 0;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or JsonException or KeyNotFoundException or InvalidOperationException or System.ComponentModel.Win32Exception)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
