namespace AuthFix;

public record MockResponse(string Diff, string Explanation, IReadOnlyList<string> References);
public record RunReport(GenerationInput Input, MockResponse Proposal, string Provider, string Validation, ValidationResult? Results = null);

public static class MockGenerator
{
    public static RunReport Generate(GenerationInput input, MockResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.Diff is null || response.Explanation is null || response.References is null)
            throw new ArgumentException("Resposta simulada incompleta.");
        return new(input, response, "simulado", "não executada");
    }
}
