using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aevatar.SourceGenerator;

public class ArtifactSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
        {
            var hasArtifactInterface = classDeclarationSyntax.BaseList?.Types
                .Any(t => t.ToString().StartsWith("IArtifact")) ?? false;

            if (hasArtifactInterface)
            {
                CandidateClasses.Add(classDeclarationSyntax);
            }
        }
    }
}