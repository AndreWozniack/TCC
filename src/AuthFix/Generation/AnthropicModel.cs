namespace AuthFix;

public enum AnthropicModel
{
    ClaudeFable5_2,
    ClaudeOpus5_5,
    ClaudeSonnet5_5,
    ClaudeHaiku5_5,
    ClaudeMythos5_2,
    ClaudeFable5_1,
    ClaudeMythos5_1,
    ClaudeFable5,
    ClaudeMythos5,
    ClaudeOpus5,
    ClaudeOpus4_8,
    ClaudeOpus4_7,
    ClaudeOpus4_6,
    ClaudeOpus4_5,
    ClaudeOpus4_1,
    ClaudeOpus4_0,
    ClaudeSonnet5,
    ClaudeSonnet4_6,
    ClaudeSonnet4_5,
    ClaudeSonnet4_0,
    ClaudeHaiku4_5
}

public static class AnthropicModelExtensions
{
    public static string ToApiId(this AnthropicModel model) => model switch
    {
        AnthropicModel.ClaudeFable5_2 => "claude-fable-5-2",
        AnthropicModel.ClaudeOpus5_5 => "claude-opus-5-5",
        AnthropicModel.ClaudeSonnet5_5 => "claude-sonnet-5-5",
        AnthropicModel.ClaudeHaiku5_5 => "claude-haiku-5-5",
        AnthropicModel.ClaudeMythos5_2 => "claude-mythos-5-2",
        AnthropicModel.ClaudeFable5_1 => "claude-fable-5-1",
        AnthropicModel.ClaudeMythos5_1 => "claude-mythos-5-1",
        AnthropicModel.ClaudeFable5 => "claude-fable-5",
        AnthropicModel.ClaudeMythos5 => "claude-mythos-5",
        AnthropicModel.ClaudeOpus5 => "claude-opus-5",
        AnthropicModel.ClaudeOpus4_8 => "claude-opus-4-8",
        AnthropicModel.ClaudeOpus4_7 => "claude-opus-4-7",
        AnthropicModel.ClaudeOpus4_6 => "claude-opus-4-6",
        AnthropicModel.ClaudeOpus4_5 => "claude-opus-4-5",
        AnthropicModel.ClaudeOpus4_1 => "claude-opus-4-1",
        AnthropicModel.ClaudeOpus4_0 => "claude-opus-4-0",
        AnthropicModel.ClaudeSonnet5 => "claude-sonnet-5",
        AnthropicModel.ClaudeSonnet4_6 => "claude-sonnet-4-6",
        AnthropicModel.ClaudeSonnet4_5 => "claude-sonnet-4-5",
        AnthropicModel.ClaudeSonnet4_0 => "claude-sonnet-4-0",
        AnthropicModel.ClaudeHaiku4_5 => "claude-haiku-4-5",
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Modelo Anthropic não suportado.")
    };

    public static AnthropicModel Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return AnthropicModel.ClaudeHaiku5_5;

        var trimmed = value.Trim();
        if (Enum.TryParse<AnthropicModel>(trimmed, ignoreCase: true, out var parsedEnum))
            return parsedEnum;

        foreach (var candidate in Enum.GetValues<AnthropicModel>())
        {
            if (string.Equals(candidate.ToApiId(), trimmed, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        throw new ArgumentException($"Modelo Anthropic desconhecido: '{value}'.");
    }
}
