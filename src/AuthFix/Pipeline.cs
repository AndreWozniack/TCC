using System.Diagnostics;
using System.Text;

namespace AuthFix;

public static class Pipeline
{
    public static RunReport Run(string repository, Alert alert, string condition, MockResponse response, string? revision, string? evaluator)
    {
        var path = SafePath(repository, alert.File);
        var source = File.ReadAllText(path);
        var method = AuthorizationAnalyzer.FindEndpoint(source, alert.StartLine);
        IReadOnlyList<Evidence> evidence = condition == "C" ? ContextRetriever.Find(RepositoryFiles(repository, alert.File)) : [];
        var input = Experiment.BuildInput(alert, method, source, evidence, condition);
        var report = MockGenerator.Generate(input, response);
        if (revision is not null && evaluator is not null)
        {
            var result = PatchValidator.Validate(repository, revision, alert.File, evaluator, response.Diff);
            report = report with { Validation = result.Status, Results = result };
        }
        return report;
    }

    public static string Git(string repository, params string[] arguments)
    {
        var info = new ProcessStartInfo("git") { WorkingDirectory = repository, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var git = Process.Start(info) ?? throw new InvalidOperationException("Git indisponível.");
        var output = git.StandardOutput.ReadToEndAsync();
        var error = git.StandardError.ReadToEnd();
        git.WaitForExit();
        if (git.ExitCode != 0)
            throw new InvalidOperationException(error.Trim());
        return output.GetAwaiter().GetResult();
    }

    /// <summary>Resolve o commit e exige que o checkout corresponda a ele, como na validação.</summary>
    public static string ResolveCleanCommit(string repository, string revision)
    {
        var commit = Git(repository, "rev-parse", "--verify", "--end-of-options", revision + "^{commit}").Trim();
        try { Git(repository, "diff", "--quiet", "--no-ext-diff", commit, "--"); }
        catch (InvalidOperationException)
        {
            throw new ArgumentException("O checkout deve corresponder ao commit usado na validação (há alterações não commitadas).");
        }
        return commit;
    }

    public static string Markdown(RunReport report) => new StringBuilder()
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

    private static string SafePath(string repository, string relative)
    {
        var current = repository;
        foreach (var part in relative.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new ArgumentException("Caminhos com links simbólicos não são suportados.");
        }
        return current;
    }

    private static Dictionary<string, string> RepositoryFiles(string repository, string alertFile)
    {
        var paths = Git(repository, "ls-files", "-z").Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var files = new Dictionary<string, string>();
        foreach (var path in paths.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.Equals(path, alertFile.Replace(Path.DirectorySeparatorChar, '/'), StringComparison.Ordinal))
                continue;
            if (path.Split('/').Any(part => part.Contains("test", StringComparison.OrdinalIgnoreCase) || part.Equals("avaliacao", StringComparison.OrdinalIgnoreCase)))
                continue;
            var parent = repository;
            var hasLink = false;
            foreach (var component in path.Split('/'))
            {
                parent = Path.Combine(parent, component);
                if ((!File.Exists(parent) && !Directory.Exists(parent)) || File.GetAttributes(parent).HasFlag(FileAttributes.ReparsePoint)) { hasLink = true; break; }
            }
            if (!hasLink) files[path] = File.ReadAllText(Path.Combine(repository, path));
        }
        return files;
    }
}
