using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AuthFix;

public static class AuthorizationAnalyzer
{
    public static string FindEndpoint(string source, int line)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var candidates = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method =>
            {
                var span = tree.GetLineSpan(method.Span);
                return span.StartLinePosition.Line + 1 <= line && span.EndLinePosition.Line + 1 >= line;
            }).ToArray();
        var minimal = root.DescendantNodes().OfType<GlobalStatementSyntax>()
            .Where(statement =>
            {
                var span = tree.GetLineSpan(statement.Span);
                return span.StartLinePosition.Line + 1 <= line && span.EndLinePosition.Line + 1 >= line
                    && statement.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
                        invocation.Expression is MemberAccessExpressionSyntax member
                        && member.Name.Identifier.ValueText is "MapGet" or "MapPost" or "MapPut" or "MapPatch" or "MapDelete");
            }).ToArray();
        if (candidates.Length + minimal.Length != 1)
            throw new ArgumentException("A localização não identifica um único endpoint.");
        return candidates.Length == 1 ? candidates[0].ToFullString().Trim() : minimal[0].ToFullString().Trim();
    }
}
