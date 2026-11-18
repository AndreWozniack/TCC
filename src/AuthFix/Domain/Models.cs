namespace AuthFix;

public record Alert(string Id, string Cwe, string File, int StartLine);

public record Evidence(string File, string Symbol, int Line, string Reason, string Text);

public record GenerationInput(Alert Alert, string Code, IReadOnlyList<Evidence> Evidence, string Condition);

public record MockResponse(string Diff, string Explanation, IReadOnlyList<string> References);

public record ValidationResult(string Status, string Build, string Security, string Functionality, string Architecture, string Log);

public record RunReport(GenerationInput Input, MockResponse Proposal, string Provider, string Validation, ValidationResult? Results = null);
