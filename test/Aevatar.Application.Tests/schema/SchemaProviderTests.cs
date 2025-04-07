using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Aevatar.Schema;

public class SchemaProviderTests : AevatarApplicationTestBase
{
    private readonly SchemaProvider _schemaProvider;

    public SchemaProviderTests()
    {
        // Use the base class dependency injection to get the real SchemaProvider instance
        _schemaProvider = GetRequiredService<SchemaProvider>();
    }

    [Fact]
    public async Task GetTypeSchema_ShouldGenerateSchemaAndCache()
    {
        // Arrange
        var type = typeof(TestClass);

        // Act
        var schema1 = _schemaProvider.GetTypeSchema(type);
        var schema2 = _schemaProvider.GetTypeSchema(type);

        // Assert
        schema1.ShouldNotBeNull(); // The schema should not be null
        schema2.ShouldNotBeNull();
        schema1.ShouldBeSameAs(schema2); // The two schemas should be the same instance (cached)
    }

    [Fact]
    public async Task GetTypeSchema_ShouldIgnoreBaseClassProperties()
    {
        // Arrange
        var type = typeof(DerivedClass);

        // Act
        var schema = _schemaProvider.GetTypeSchema(type);

        // Assert
        schema.Properties.ShouldContainKey("DerivedProperty"); // Derived class property should be present
        schema.Properties.ShouldNotContainKey("BaseProperty"); // Base class property should be ignored (due to IgnoreSpecificBaseProcessor)
    }

    /*
    [Fact]
    public async Task ConvertValidateError_ShouldConvertErrorsToDictionary()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError(
                ValidationErrorKind.Unknown,
                "Name",
                "Name",
                new JsonSchema { Description = "Name is required" },
                null
            ),
            new ValidationError(
                ValidationErrorKind.Minimum,
                "Age",
                "Age",
                new JsonSchema { Description = "Age must be greater than or equal to 1" },
                0
            )
        };

        // Act
        var result = _schemaProvider.ConvertValidateError(errors);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Dictionary<string, string>>(); // Result should be a Dictionary
        result.ShouldContainKeyAndValue("Name", "Name is required");
        result.ShouldContainKeyAndValue("Age", "Age must be greater than or equal to 1");
    }

    [Fact]
    public async Task ConvertValidateError_ShouldHandleEmptyDescription()
    {
        // Arrange
        var error = new ValidationError(
            ValidationErrorKind.Required,
            "Name",
            "Name",
            new JsonSchema(), // No description is set
            null
        );

        // Act
        var result = _schemaProvider.ConvertValidateError(error);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("Field is incorrect"); // Should use default message
    }
    */

    // Mock test class: base class
    public class BaseClass
    {
        public string BaseProperty { get; set; }
    }

    // Mock test class: derived class
    public class DerivedClass : BaseClass
    {
        public string DerivedProperty { get; set; }
    }

    // Mock test class: regular test object
    public class TestClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}