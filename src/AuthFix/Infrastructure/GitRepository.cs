namespace AuthFix;

public static class GitRepository
{
    public static string Execute(string repository, params string[] arguments)
    {
        var result = ProcessRunner.Run("git", repository, arguments);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(result.Stderr.Trim());
        return result.Stdout;
    }

    /// <summary>Resolve o commit e exige que o checkout corresponda a ele, como na validação.</summary>
    public static string ResolveCleanCommit(string repository, string revision)
    {
        var commit = Execute(repository, "rev-parse", "--verify", "--end-of-options", revision + "^{commit}").Trim();
        try
        {
            Execute(repository, "diff", "--quiet", "--no-ext-diff", commit, "--");
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException("O checkout deve corresponder ao commit usado na validação (há alterações não commitadas).");
        }
        return commit;
    }

    public static string ReadSafeText(string repository, string relativePath)
    {
        var current = repository;
        foreach (var part in relativePath.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new ArgumentException("Caminhos com links simbólicos não são suportados.");
        }
        return File.ReadAllText(current);
    }

    public static Dictionary<string, string> ReadSourceFiles(string repository, string alertFile)
    {
        var paths = Execute(repository, "ls-files", "-z").Split('\0', StringSplitOptions.RemoveEmptyEntries);
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
                if ((!File.Exists(parent) && !Directory.Exists(parent)) || File.GetAttributes(parent).HasFlag(FileAttributes.ReparsePoint))
                {
                    hasLink = true;
                    break;
                }
            }
            if (!hasLink)
                files[path] = File.ReadAllText(Path.Combine(repository, path));
        }
        return files;
    }
}
