using System.Text.Json;

namespace AuthFix;

public static class GeneratorFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IFixGenerator Create(string repository, ScanOptions options, string? apiKey = null)
    {
        MockGenerator? fallback = null;
        var responsePath = Path.GetFullPath(options.Response, repository);
        if (File.Exists(responsePath))
        {
            var response = JsonSerializer.Deserialize<MockResponse>(File.ReadAllText(responsePath), JsonOptions)
                ?? throw new ArgumentException("Resposta simulada vazia.");
            fallback = new MockGenerator(response);
        }

        var resolvedKey = apiKey ?? ResolveApiKey(repository);
        if (!options.ForceMock && !string.IsNullOrWhiteSpace(resolvedKey))
        {
            var model = options.Model
                ?? ResolveEnvValue(repository, "ANTHROPIC_MODEL")
                ?? AnthropicProvider.DefaultModel;
            var provider = new AnthropicProvider(resolvedKey, model);
            return new LlmGenerator(provider, file => GitRepository.ReadSafeText(repository, file), fallback);
        }

        if (fallback is not null)
            return fallback;

        throw new ArgumentException($"Chave ANTHROPIC_API_KEY ausente e proposta simulada não encontrada ({options.Response}).");
    }

    public static string? ResolveApiKey(string repository)
        => ResolveEnvValue(repository, "ANTHROPIC_API_KEY");

    public static string? ResolveEnvValue(string repository, string keyName)
    {
        var envValue = Environment.GetEnvironmentVariable(keyName);
        if (!string.IsNullOrWhiteSpace(envValue))
            return envValue.Trim();

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Path.Combine(repository, ".env"),
            Path.Combine(home, "Documents", "TCC", ".env"),
            Path.Combine(home, "Documents", "TCC", "src", "AuthFix", ".env"),
            Path.Combine(home, ".env")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;
                if (line.StartsWith("export ", StringComparison.Ordinal))
                    line = line["export ".Length..].Trim();
                if (line.StartsWith(keyName + "=", StringComparison.Ordinal))
                {
                    var value = line[(keyName.Length + 1)..].Trim().Trim('"', '\'');
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
        }

        return null;
    }
}
