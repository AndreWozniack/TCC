namespace AuthFix;

public interface IFixGenerator
{
    RunReport Generate(GenerationInput input);
}
