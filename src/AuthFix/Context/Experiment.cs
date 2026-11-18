namespace AuthFix;

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
