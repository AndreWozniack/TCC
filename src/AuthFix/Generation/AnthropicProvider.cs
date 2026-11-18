using System.Text;
using System.Text.Json;

namespace AuthFix;

public sealed class AnthropicProvider : ILlmProvider
{
    public const string DefaultModel = "claude-sonnet-4-5";
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(60) };

    private static readonly JsonElement StructuredOutputSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["fixedCode", "explanation", "references"],
          "properties": {
            "fixedCode": { "type": "string" },
            "explanation": { "type": "string" },
            "references": {
              "type": "array",
              "items": { "type": "string" }
            }
          }
        }
        """);

    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public AnthropicProvider(string apiKey, string model = DefaultModel, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Chave da API Anthropic ausente.");
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
        _httpClient = httpClient ?? SharedClient;
    }

    public string Name => $"anthropic:{_model}";

    public string Complete(string systemPrompt, string userPrompt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var payload = new
        {
            model = _model,
            max_tokens = 4096,
            temperature = 0,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            },
            output_config = new
            {
                format = new
                {
                    type = "json_schema",
                    schema = StructuredOutputSchema
                }
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
        var responseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Erro da API Anthropic ({(int)response.StatusCode}): {responseJson}");

        using var document = JsonDocument.Parse(responseJson);
        foreach (var block in document.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) && type.GetString() == "text"
                && block.TryGetProperty("text", out var text))
            {
                return text.GetString() ?? "";
            }
        }

        throw new InvalidOperationException("A resposta da API Anthropic não contém bloco de texto.");
    }
}
