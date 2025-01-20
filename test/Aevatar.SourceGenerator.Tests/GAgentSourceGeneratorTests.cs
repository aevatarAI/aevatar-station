using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Aevatar.Core.Abstractions;
using Aevatar.SourceGenerator;
using Shouldly;
using Xunit.Abstractions;

public class GAgentSourceGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public GAgentSourceGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void GAgentSourceGeneratorTest()
    {
        // Arrange.
        const string source = @"
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents
{
    public interface IMyArtifact : IArtifact<GeneratedGAgentState, GeneratedStateLogEvent> { }
    public class MyArtifact : IMyArtifact
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
        var generatedTrees = outputCompilation.SyntaxTrees.ToList();
        generatedTrees.Count.ShouldBe(3);
        var generatedCode = generatedTrees.Last().ToString();
        _output.WriteLine(generatedTrees[1].ToString());
        _output.WriteLine(generatedTrees[2].ToString());
        generatedCode.ShouldContain("public partial class MyArtifactGAgent");
    }
}