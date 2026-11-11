using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AuthFix;

public static class ContextRetriever
{
    private static readonly string[] Markers =
    [
        "Authorize", "AllowAnonymous", "AuthorizationHandler", "IAuthorizationHandler",
        "IAuthorizationRequirement", "IAuthorizationService", "AddAuthorization",
        "RequireAuthorization", "FallbackPolicy", "DefaultPolicy"
    ];

    public static IReadOnlyList<Evidence> Find(IReadOnlyDictionary<string, string> files)
    {
        var evidence = new List<Evidence>();
        foreach (var (file, content) in files.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            var root = CSharpSyntaxTree.ParseText(content).GetRoot();
            foreach (var statement in root.DescendantNodes().OfType<GlobalStatementSyntax>())
            {
                var marker = Markers.FirstOrDefault(statement.ToString().Contains);
                if (marker is null)
                    continue;
                var line = statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                evidence.Add(new(file, "(global)", line, $"Referência a {marker}", statement.ToString()));
            }
            foreach (var declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                if (declaration is not (BaseTypeDeclarationSyntax or MethodDeclarationSyntax))
                    continue;
                var header = declaration switch
                {
                    TypeDeclarationSyntax type => string.Join(" ", type.AttributeLists) + " " + type.BaseList,
                    MethodDeclarationSyntax method => string.Join(" ", method.AttributeLists) + " " + method.Body,
                    _ => ""
                };
                var marker = Markers.FirstOrDefault(header.Contains);
                if (marker is null)
                    continue;
                var symbol = declaration switch
                {
                    TypeDeclarationSyntax type => type.Identifier.ValueText,
                    MethodDeclarationSyntax method => method.Identifier.ValueText,
                    _ => ""
                };
                var line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                evidence.Add(new(file, symbol, line, $"Referência a {marker}", declaration.ToString()));
            }
        }
        return evidence.Take(20).ToArray();
    }
}
