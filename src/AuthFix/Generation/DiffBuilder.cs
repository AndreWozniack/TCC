namespace AuthFix;

public static class DiffBuilder
{
    public static string Build(string relativeFile, string fullOriginalFile, string targetSnippet, string fixedSnippet)
    {
        var normalizedFull = fullOriginalFile.Replace("\r\n", "\n", StringComparison.Ordinal);
        var normalizedTarget = targetSnippet.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var normalizedFixed = fixedSnippet.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        string updatedFull;
        if (string.Equals(normalizedFull.Trim(), normalizedTarget, StringComparison.Ordinal))
        {
            updatedFull = normalizedFixed + (normalizedFull.EndsWith('\n') ? "\n" : "");
        }
        else
        {
            var index = normalizedFull.IndexOf(normalizedTarget, StringComparison.Ordinal);
            if (index < 0)
                throw new InvalidOperationException("Não foi possível localizar o trecho original no arquivo para gerar o diff.");

            // Preserva a indentação da primeira linha caso o snippet original tenha vindo sem os espaços iniciais
            var lineStart = normalizedFull.LastIndexOf('\n', index) + 1;
            var indent = normalizedFull[lineStart..index];
            if (indent.All(char.IsWhiteSpace) && indent.Length > 0 && normalizedFixed.Contains('\n'))
            {
                var lines = normalizedFixed.Split('\n');
                if (lines.Length > 1 && !lines[0].StartsWith(indent, StringComparison.Ordinal))
                {
                    for (var i = 1; i < lines.Length; i++)
                    {
                        if (lines[i].Length > 0 && !lines[i].StartsWith(indent, StringComparison.Ordinal))
                            lines[i] = indent + lines[i];
                    }
                    normalizedFixed = string.Join('\n', lines);
                }
            }

            updatedFull = string.Concat(normalizedFull.AsSpan(0, index), normalizedFixed, normalizedFull.AsSpan(index + normalizedTarget.Length));
        }

        return CreateUnifiedDiff(relativeFile, normalizedFull, updatedFull);
    }

    public static string CreateUnifiedDiff(string relativeFile, string originalContent, string updatedContent)
    {
        var normalizedPath = relativeFile.Replace('\\', '/');
        var tempRoot = Path.Combine(Path.GetTempPath(), "authfix-diff-" + Guid.NewGuid().ToString("N"));
        try
        {
            var fileA = Path.Combine(tempRoot, "a", normalizedPath);
            var fileB = Path.Combine(tempRoot, "b", normalizedPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fileA)!);
            Directory.CreateDirectory(Path.GetDirectoryName(fileB)!);
            File.WriteAllText(fileA, originalContent);
            File.WriteAllText(fileB, updatedContent);

            var result = ProcessRunner.Run("git", tempRoot, "diff", "--no-index", "--src-prefix=", "--dst-prefix=", "--", $"a/{normalizedPath}", $"b/{normalizedPath}");
            // git diff --no-index retorna 1 quando há diferenças e 0 quando os arquivos são iguais
            if (result.ExitCode is not (0 or 1))
                throw new InvalidOperationException("Falha ao calcular diff unificado: " + result.Stderr.Trim());

            return result.Stdout;
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
    }
}
