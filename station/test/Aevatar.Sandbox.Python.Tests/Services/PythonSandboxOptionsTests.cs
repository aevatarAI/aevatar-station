using Aevatar.Sandbox.Python.Services;

namespace Aevatar.Sandbox.Python.Services;

public class PythonSandboxOptionsTests
{
    [Fact]
    public void Should_Have_Default_Values()
    {
        // Arrange & Act
        var options = new PythonSandboxOptions();

        // Assert
        options.PythonImage.ShouldBe("python:3.9-slim");
        options.Namespace.ShouldBe("sandbox-python");
    }

    [Fact]
    public void Should_Set_And_Get_Values()
    {
        // Arrange
        var options = new PythonSandboxOptions();
        
        // Act
        options.PythonImage = "custom-python:3.10";
        options.Namespace = "custom-namespace";

        // Assert
        options.PythonImage.ShouldBe("custom-python:3.10");
        options.Namespace.ShouldBe("custom-namespace");
    }
}