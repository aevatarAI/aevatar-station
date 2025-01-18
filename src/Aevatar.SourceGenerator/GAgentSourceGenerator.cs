using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Aevatar.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class GAgentSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsClassDeclarationWithIArtifact(s),
                transform: static (ctx, _) => GetClassDeclaration(ctx))
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static bool IsClassDeclarationWithIArtifact(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax &&
               (classDeclarationSyntax.BaseList?.Types
                   .Any(t => t.ToString().StartsWith("IArtifact")) ?? false);
    }

    private static ClassDeclarationSyntax? GetClassDeclaration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        return classDeclarationSyntax;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes,
        SourceProductionContext context)
    {
        foreach (var candidateClass in classes)
        {
            var semanticModel = compilation.GetSemanticModel(candidateClass!.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(candidateClass) is not INamedTypeSymbol classSymbol)
                continue;

            var artifactInterface = classSymbol.AllInterfaces
                .FirstOrDefault(i => i.OriginalDefinition.Name == "IArtifact");

            if (artifactInterface is null || artifactInterface.TypeArguments.Length != 2)
                continue;

            // TState
            var stateType = artifactInterface.TypeArguments[0].ToDisplayString();
            // TStateLogEvent
            var stateLogEventType = artifactInterface.TypeArguments[1].ToDisplayString();

            var classNamespace = classSymbol.ContainingNamespace.ToDisplayString();
            var generatedClassName = $"{classSymbol.Name}GAgent";

            var generatedCode = GenerateGAgentClassCode(
                classNamespace,
                generatedClassName,
                classSymbol.Name,
                stateType,
                stateLogEventType
            );

            context.AddSource($"{generatedClassName}.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
        }
    }

    private static string GenerateGAgentClassCode(
        string classNamespace,
        string className,
        string artifactTypeName,
        string gAgentStateType,
        string gAgentStateLogEventType)
    {
        var generatedCode = Template
            .Replace("{ClassNamespace}", classNamespace)
            .Replace("{ClassName}", className)
            .Replace("{ArtifactTypeName}", artifactTypeName)
            .Replace("{GAgentStateType}", gAgentStateType)
            .Replace("{GAgentStateLogEventType}", gAgentStateLogEventType);

        return generatedCode;
    }

    private const string Template = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace {ClassNamespace};

public class {ClassName} : GAgentBase<{GAgentStateType}, {GAgentStateLogEventType}>
{
    private readonly {ArtifactTypeName} _artifact;

    public {ClassName}(ILogger<{ClassName}> logger, {ArtifactTypeName} artifact) : base(logger)
    {
        _artifact = artifact;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(_artifact.GetDescription());
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
        await UpdateObserverList(_artifact.GetType());
    }

    protected override void GAgentTransitionState({GAgentStateType} state, StateLogEventBase<{GAgentStateLogEventType}> @event)
    {
        base.GAgentTransitionState(state, @event);
        _artifact.TransitionState(state, @event);
    }
}
";
}