namespace AuthFix;

public record Evidence(string File, string Symbol, int Line, string Reason, string Text);
public record GenerationInput(Alert Alert, string Code, IReadOnlyList<Evidence> Evidence, string Condition);

public static class Experiment
{
    public static GenerationInput BuildInput(Alert alert, string method, string file, IReadOnlyList<Evidence> evidence, string condition)
    {
        return condition switch
        {
            "A" => new(alert, method, Array.Empty<Evidence>(), condition),
            "B" => new(alert, file, Array.Empty<Evidence>(), condition),
            "C" => new(alert, file, evidence, condition),
            _ => throw new ArgumentException("Condição deve ser A, B ou C.")
        };
    }
}
