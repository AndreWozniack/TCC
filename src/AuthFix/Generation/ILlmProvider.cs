namespace AuthFix;

public interface ILlmProvider
{
    string Name { get; }
    string Complete(string systemPrompt, string userPrompt);
}
