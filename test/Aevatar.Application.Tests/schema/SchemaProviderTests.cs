using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Aevatar.Schema;

public class SchemaProviderTests : AevatarApplicationTestBase
{
    private readonly SchemaProvider _schemaProvider;

    public SchemaProviderTests()
    {
        // 使用基类注入机制，获取实际的 SchemaProvider 实例
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
        schema1.ShouldNotBeNull(); // 验证 schema 不为空
        schema2.ShouldNotBeNull();
        schema1.ShouldBeSameAs(schema2); // 验证缓存功能：两次获取应该返回同一个对象
    }

    [Fact]
    public async Task GetTypeSchema_ShouldIgnoreBaseClassProperties()
    {
        // Arrange
        var type = typeof(DerivedClass);

        // Act
        var schema = _schemaProvider.GetTypeSchema(type);

        // Assert
        schema.Properties.ShouldContainKey("DerivedProperty"); // 子类属性应存在
        schema.Properties.ShouldNotContainKey("BaseProperty"); // 基类属性应被忽略（通过 IgnoreSpecificBaseProcessor 实现）
    }

    // [Fact]
    // public async Task ConvertValidateError_ShouldConvertErrorsToDictionary()
    // {
    //     // Arrange
    //     var errors = new List<ValidationError>
    //     {
    //         new ValidationError(
    //             ValidationErrorKind.Unknown,
    //             "Name",
    //             "Name",
    //             new JsonSchema { Description = "Name is required" },
    //             null
    //         ),
    //         new ValidationError(
    //             ValidationErrorKind.Minimum,
    //             "Age",
    //             "Age",
    //             new JsonSchema { Description = "Age must be greater than or equal to 1" },
    //             0
    //         )
    //     };
    //
    //     // Act
    //     var result = _schemaProvider.ConvertValidateError(errors);
    //
    //     // Assert
    //     result.ShouldNotBeNull();
    //     result.ShouldBeOfType<Dictionary<string, string>>(); // 返回结果是 Dictionary 类型
    //     result.ShouldContainKeyAndValue("Name", "Name is required");
    //     result.ShouldContainKeyAndValue("Age", "Age must be greater than or equal to 1");
    // }

    // [Fact]
    // public async Task ConvertValidateError_ShouldHandleEmptyDescription()
    // {
    //     // Arrange
    //     var error = new ValidationError(
    //         ValidationErrorKind.Required,
    //         "Name",
    //         "Name",
    //         new JsonSchema(), // 没有设置 Description
    //         null
    //     );
    //
    //     // Act
    //     var result = _schemaProvider.ConvertValidateError(error);
    //
    //     // Assert
    //     result.ShouldNotBeNull();
    //     result.ShouldBe("Field is incorrect"); // 使用默认描述
    // }

    // 模拟测试类
    public class BaseClass
    {
        public string BaseProperty { get; set; }
    }

    public class DerivedClass : BaseClass
    {
        public string DerivedProperty { get; set; }
    }

    public class TestClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}