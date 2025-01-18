using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Aevatar.SourceGenerator;

[Generator]
public class GAgentSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ArtifactSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not ArtifactSyntaxReceiver receiver)
            return;

        foreach (var candidateClass in receiver.CandidateClasses)
        {
            var semanticModel = context.Compilation.GetSemanticModel(candidateClass.SyntaxTree);

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

            var generatedClassName = $"{classSymbol.Name}GAgent";

            var generatedCode = GenerateGAgentClassCode(
                generatedClassName,
                classSymbol.Name,
                stateType,
                stateLogEventType
            );

            context.AddSource($"{generatedClassName}.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
        }
    }

    private string GenerateGAgentClassCode(
        string className,
        string artifactTypeName,
        string gAgentStateType,
        string gAgentStateLogEventType)
    {
        var generatedCode = Template
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
        _artifact.ApplyEvent(@event);
    }
}
";
}