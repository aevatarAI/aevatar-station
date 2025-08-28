using Aevatar.Schema;
using NJsonSchema.Generation;
using Shouldly;
using Xunit;

namespace Aevatar.Schema;

/// <summary>
/// Independent unit tests for DocumentationLinkProcessor that don't rely on test framework setup
/// </summary>
public class DocumentationLinkProcessorTests
{
    [Fact]
    public void Constructor_ShouldCreateProcessor()
    {
        // Arrange & Act
        var processor = new DocumentationLinkProcessor();
        
        // Assert
        processor.ShouldNotBeNull();
    }

    [Fact]
    public void ProcessorImplementsISchemaProcessor()
    {
        // Arrange & Act
        var processor = new DocumentationLinkProcessor();
        
        // Assert
        processor.ShouldBeAssignableTo<ISchemaProcessor>();
    }
    
    [Fact]
    public void Process_WithNullContext_ShouldNotThrow()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        
        // Act & Assert
        Should.NotThrow(() => processor.Process(null));
    }
}