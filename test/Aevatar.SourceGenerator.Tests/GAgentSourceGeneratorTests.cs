using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Aevatar.Core.Abstractions;
using Aevatar.SourceGenerator;
using Shouldly;

public class GAgentSourceGeneratorTests
{
    [Fact]
    public void TestGAgentSourceGenerator()
    {
        // Arrange.
        const string source = @"
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents
{
    public class MyArtifact : IArtifact<GeneratedGAgentState, GeneratedStateLogEvent>
    {
        public string GetDescription() => ""MyArtifact Description"";
        public void SetState(GeneratedGAgentState state) { /* custom logic */ }
        public void ApplyEvent(StateLogEventBase<GeneratedStateLogEvent> stateLogEvent) { /* custom logic */ }
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8));
        var compilation = CSharpCompilation.Create("TestAssembly",
            [syntaxTree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IArtifact<,>).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new GAgentSourceGenerator();

        // Act.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Assert.
        diagnostics.ShouldBeEmpty();
        var generatedTrees = outputCompilation.SyntaxTrees.ToList();
        generatedTrees.Count.ShouldBe(2);// Original + Generated
        var generatedCode = generatedTrees.Last().ToString();
        generatedCode.ShouldContain("public class MyArtifactGAgent");
    }
}