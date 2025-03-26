using System;
using System.Collections.Generic;
using Aevatar.Common;
using Xunit;

namespace Aevatar.common;

public class ReflectionUtilTests
{
    [Fact]
    public void ConvertValue_ShouldReturnNull_WhenValueIsNull()
    {
        // Arrange
        Type targetType = typeof(string);
        object? value = null;

        // Act
        var result = ReflectionUtil.ConvertValue(targetType, value);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertValue_ShouldConvertValueToTargetType()
    {
        // Arrange
        Type targetType = typeof(int);
        object value = "123";

        // Act
        var result = ReflectionUtil.ConvertValue(targetType, value);

        // Assert
        Assert.Equal(123, result);
        Assert.IsType<int>(result);
    }

    [Fact]
    public void ConvertValue_ShouldHandleGenericListConversion()
    {
        // Arrange
        var targetType = typeof(List<int>);
        var value = new List<object> { "1", "2", "3" };

        // Act
        var result = ReflectionUtil.ConvertValue(targetType, value);

        // Assert
        Assert.IsType<List<int>>(result);

        var resultList = (List<int>)result!;
        Assert.Equal(3, resultList.Count);
        Assert.Equal(1, resultList[0]);
        Assert.Equal(2, resultList[1]);
        Assert.Equal(3, resultList[2]);
    }

    [Fact]
    public void ConvertValue_ShouldThrowException_WhenTargetTypeDoesNotMatchValue()
    {
        // Arrange
        Type targetType = typeof(int);
        object value = "abc"; // Invalid value that cannot be converted to int

        // Act & Assert
        Assert.Throws<FormatException>(() => ReflectionUtil.ConvertValue(targetType, value));
    }

    [Fact]
    public void ConvertValue_ShouldHandleNestedGenericListConversion()
    {
        // Arrange
        var targetType = typeof(List<List<int>>);
        var value = new List<object>
        {
            new List<object> { "1", "2" },
            new List<object> { "3", "4" }
        };

        // Act
        var result = ReflectionUtil.ConvertValue(targetType, value);

        // Assert
        Assert.IsType<List<List<int>>>(result);

        var resultList = (List<List<int>>)result!;
        Assert.Equal(2, resultList.Count);
        Assert.Equal(new List<int> { 1, 2 }, resultList[0]);
        Assert.Equal(new List<int> { 3, 4 }, resultList[1]);
    }

    [Fact]
    public void ConvertValue_ShouldConvertStringToDateTime()
    {
        // Arrange
        Type targetType = typeof(DateTime);
        object value = "2023-10-10";

        // Act
        var result = ReflectionUtil.ConvertValue(targetType, value);

        // Assert
        Assert.IsType<DateTime>(result);
        Assert.Equal(new DateTime(2023, 10, 10), result);
    }

    // [Fact] TODO  NullableInt is not supported
    public void ConvertValue_ShouldHandleStringToNullableInt()
    {
        // Arrange
        Type targetType = typeof(int?);
        object value = "123";

        // Act
        var result = ReflectionUtil.ConvertValue(targetType, value);

        // Assert
        Assert.IsType<int>(result);
        Assert.Equal(123, result);
    }

    [Fact]
    public void ConvertValue_ShouldHandleNullForNullableType()
    {
        // Arrange
        Type targetType = typeof(int?);
        object? value = null;

        // Act
        var result = ReflectionUtil.ConvertValue(targetType, value);

        // Assert
        Assert.Null(result);
    }
}