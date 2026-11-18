namespace AuthFix;

public static class PatchValidator
{
    public static ValidationResult Validate(string repository, string revision, string allowedFile, string evaluatorDirectory, string diff)
    {
        const string pending = "não executada";
        if (string.IsNullOrWhiteSpace(diff))
            return new("patch ausente", pending, pending, pending, "revisão humana pendente", "");
        var worktree = Path.Combine(Path.GetTempPath(), "authfix-" + Guid.NewGuid().ToString("N"));
        var patch = worktree + ".patch";
        var name = "authfix-" + Guid.NewGuid().ToString("N");
        var created = false;
        try
        {
            var resolved = ProcessRunner.Run("git", repository, "rev-parse", "--verify", "--end-of-options", revision + "^{commit}");
            if (resolved.ExitCode != 0)
                throw new InvalidOperationException(resolved.Output);
            var add = ProcessRunner.Run("git", repository, "-c", "core.hooksPath=/dev/null", "worktree", "add", "--detach", worktree, resolved.Output.Trim());
            if (add.ExitCode != 0)
                throw new InvalidOperationException(add.Output);
            created = true;
            File.WriteAllText(patch, diff);
            var apply = ProcessRunner.Run("git", worktree, "apply", "--check", "--", patch);
            if (apply.ExitCode != 0)
                return new("patch inválido", pending, pending, pending, "revisão humana pendente", apply.Output);
            apply = ProcessRunner.Run("git", worktree, "apply", "--", patch);
            if (apply.ExitCode != 0)
                return new("patch inválido", pending, pending, pending, "revisão humana pendente", apply.Output);
            var changed = ProcessRunner.Run("git", worktree, "diff", "--name-only", "-z");
            var names = changed.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            if (changed.ExitCode != 0 || names.Length != 1 || names[0] != allowedFile)
                return new("arquivos não permitidos", pending, pending, pending, "revisão humana pendente", changed.Output);
            var changedPath = Path.Combine(worktree, allowedFile);
            if (!File.Exists(changedPath) || File.GetAttributes(changedPath).HasFlag(FileAttributes.ReparsePoint))
                return new("arquivo inválido", pending, pending, pending, "revisão humana pendente", "");
            var docker = ProcessRunner.Run("docker", repository, "run", "--rm", "--name", name, "--network", "none",
                "--memory", "1g", "--cpus", "2", "--pids-limit", "256", "--cap-drop", "ALL",
                "--security-opt", "no-new-privileges",
                "--mount", $"type=bind,src={worktree},dst=/work",
                "--mount", $"type=bind,src={Path.GetFullPath(evaluatorDirectory)},dst=/evaluation,readonly",
                "mcr.microsoft.com/dotnet/sdk:10.0", "sh", "/evaluation/teste.sh");
            var build = docker.Output.Contains("Build succeeded.", StringComparison.Ordinal) ? "aprovada"
                : docker.Output.Contains("Build FAILED.", StringComparison.Ordinal) ? "reprovada" : pending;
            var security = docker.Output.Contains("SEGURANCA: APROVADO", StringComparison.Ordinal) ? "aprovada"
                : docker.Output.Contains("SEGURANCA: FALHOU", StringComparison.Ordinal) ? "reprovada" : pending;
            var functionality = docker.Output.Contains("FUNCIONALIDADE: APROVADO", StringComparison.Ordinal) ? "aprovada"
                : docker.Output.Contains("FUNCIONALIDADE: FALHOU", StringComparison.Ordinal) ? "reprovada" : pending;
            return new(docker.ExitCode == 0 ? "testes executados" : "testes ou ambiente falharam", build,
                security, functionality, "revisão humana pendente", docker.Output);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            return new("erro de infraestrutura", pending, pending, pending, "revisão humana pendente", ex.Message);
        }
        finally
        {
            try { ProcessRunner.Run("docker", repository, "rm", "-f", name); } catch (Exception) { }
            if (created)
            {
                try { ProcessRunner.Run("git", repository, "worktree", "remove", "--force", worktree); } catch (Exception) { }
            }
            if (File.Exists(patch)) File.Delete(patch);
        }
    }
}
