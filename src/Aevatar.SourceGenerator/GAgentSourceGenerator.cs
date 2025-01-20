using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Aevatar.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class GAgentSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsTypeDeclarationWithIArtifact(s),
                transform: static (ctx, _) => GetTypeDeclaration(ctx))
            .Where(static m => m is not null);

        var compilationAndTypes = context.CompilationProvider.Combine(typeDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndTypes,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static bool IsTypeDeclarationWithIArtifact(SyntaxNode node)
    {
        return node is TypeDeclarationSyntax typeDeclarationSyntax &&
               (typeDeclarationSyntax.BaseList?.Types
                   .Any(t => t.ToString().StartsWith("IArtifact")) ?? false);
    }

    private static TypeDeclarationSyntax? GetTypeDeclaration(GeneratorSyntaxContext context)
    {
        var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;
        return typeDeclarationSyntax;
    }

    private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax?> types,
        SourceProductionContext context)
    {
        foreach (var candidateType in types)
        {
            var semanticModel = compilation.GetSemanticModel(candidateType!.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(candidateType) is not INamedTypeSymbol typeSymbol)
                continue;

            var artifactInterface = typeSymbol.AllInterfaces
                .FirstOrDefault(i => i.OriginalDefinition.Name == "IArtifact");

            if (artifactInterface is null || artifactInterface.TypeArguments.Length != 2)
                continue;

            // TState
            var stateType = artifactInterface.TypeArguments[0].ToDisplayString();
            // TStateLogEvent
            var stateLogEventType = artifactInterface.TypeArguments[1].ToDisplayString();

            var typeNamespace = typeSymbol.ContainingNamespace.ToDisplayString();
            var typeName = typeSymbol.Name;
            if (typeSymbol.TypeKind == TypeKind.Interface && typeName.StartsWith("I"))
            {
                typeName = typeName.Substring(1);
            }
            var generatedGAgentTypeName = $"{typeName}GAgent";

            var generatedCode = GenerateGAgentImplementationCode(
                typeNamespace,
                typeName,
                generatedGAgentTypeName,
                typeSymbol.Name,
                stateType,
                stateLogEventType
            );

            context.AddSource($"{generatedGAgentTypeName}.aevatar.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
        }
    }

    private static string GenerateGAgentImplementationCode(
        string typeNamespace,
        string typeName,
        string gAgentTypeName,
        string artifactTypeName,
        string gAgentStateType,
        string gAgentStateLogEventType)
    {
        var generatedCode = ImplementationTemplate
            .Replace("{ClassNamespace}", typeNamespace)
            .Replace("{GAgentNamespace}", typeNamespace.ToLower().Replace('.', '/'))
            .Replace("{TypeName}", typeName)
            .Replace("{GAgentAlias}", typeName.ToLower())
            .Replace("{ClassName}", gAgentTypeName)
            .Replace("{ArtifactTypeName}", artifactTypeName)
            .Replace("{GAgentStateType}", gAgentStateType)
            .Replace("{GAgentStateLogEventType}", gAgentStateLogEventType);

        return generatedCode;
    }

    private const string ImplementationTemplate = @"using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using {ClassNamespace};

namespace {ClassNamespace};

public interface I{ClassName} : IStateGAgent<{GAgentStateType}>;

[GAgent(""{GAgentAlias}"", ""{GAgentNamespace}"")]
public class {ClassName} : GAgentBase<{GAgentStateType}, {GAgentStateLogEventType}>, I{ClassName}, IArtifactGAgent
{
    private readonly {ArtifactTypeName} _artifact;

    public {ClassName}(ILogger<{ClassName}> logger) : base(logger)
    {
        _artifact = ActivatorUtilities.CreateInstance<{TypeName}>(ServiceProvider);
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

    public override async Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false)
    {
        var gAgentEvents = await base.GetAllSubscribedEventsAsync(includeBaseHandlers);
        var artifactEventHandlerMethods = GetEventHandlerMethods(_artifact.GetType());
        var handlingTypes = artifactEventHandlerMethods
            .Select(m => m.GetParameters().First().ParameterType);
        return handlingTypes.Concat(gAgentEvents!).ToList();
    }
}
";
}