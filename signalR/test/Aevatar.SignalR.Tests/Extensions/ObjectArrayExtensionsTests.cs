using Aevatar.SignalR.Extensions;
using Newtonsoft.Json;
using Shouldly;

namespace Aevatar.SignalR.Tests.Extensions;

public class ObjectArrayExtensionsTests
{
    [Fact]
    public void ToStrings_WithEmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        object[] emptyArray = [];

        // Act
        var result = emptyArray.ToStrings();

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(0);
    }

    [Fact]
    public void ToStrings_WithSimpleTypes_ShouldSerializeCorrectly()
    {
        // Arrange
        object[] objects = [123, "test", true];

        // Act
        var result = objects.ToStrings();

        // Assert
        result.Length.ShouldBe(3);
        result[0].ShouldBe("123");
        result[1].ShouldBe("\"test\"");
        result[2].ShouldBe("true");
    }

    [Fact]
    public void ToStrings_WithComplexObjects_ShouldSerializeToJson()
    {
        // Arrange
        var testObject = new { Name = "TestName", Value = 42 };
        object[] objects = [testObject];
        var expectedJson = JsonConvert.SerializeObject(testObject);

        // Act
        var result = objects.ToStrings();

        // Assert
        result.Length.ShouldBe(1);
        result[0].ShouldBe(expectedJson);
    }

    [Fact]
    public void ToStrings_WithNullObjects_ShouldHandleNullValues()
    {
        // Arrange - Use explicit cast to match the method signature
        object[] objects = [(object)null!];

        // Act
        var result = objects.ToStrings();

        // Assert
        result.Length.ShouldBe(1);
        result[0].ShouldBe("null");
    }
} 