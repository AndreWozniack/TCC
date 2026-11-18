namespace AuthFix;

public static class Pipeline
{
    public static RunReport Run(string repository, Alert alert, string condition, IFixGenerator generator, string? revision, string? evaluator)
    {
        var source = GitRepository.ReadSafeText(repository, alert.File);
        var method = AuthorizationAnalyzer.FindEndpoint(source, alert.StartLine);
        IReadOnlyList<Evidence> evidence = condition == "C"
            ? ContextRetriever.Find(GitRepository.ReadSourceFiles(repository, alert.File))
            : [];
        var input = Experiment.BuildInput(alert, method, source, evidence, condition);
        var report = generator.Generate(input);
        if (revision is not null && evaluator is not null)
        {
            var result = PatchValidator.Validate(repository, revision, alert.File, evaluator, report.Proposal.Diff);
            report = report with { Validation = result.Status, Results = result };
        }
        return report;
    }

    public static RunReport Run(string repository, Alert alert, string condition, MockResponse response, string? revision, string? evaluator)
        => Run(repository, alert, condition, new MockGenerator(response), revision, evaluator);
}
