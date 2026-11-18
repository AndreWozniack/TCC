namespace AuthFix;

public sealed class MockGenerator(MockResponse response) : IFixGenerator
{
    private readonly MockResponse _response = Validate(response);

    public RunReport Generate(GenerationInput input) => Generate(input, _response);

    public static RunReport Generate(GenerationInput input, MockResponse response)
    {
        var validated = Validate(response);
        return new(input, validated, "simulado", "não executada");
    }

    private static MockResponse Validate(MockResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.Diff is null || response.Explanation is null || response.References is null)
            throw new ArgumentException("Resposta simulada incompleta.");
        return response;
    }
}
