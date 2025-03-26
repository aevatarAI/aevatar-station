using System;
using Aevatar.Common;
using Xunit;

namespace Aevatar.common;

public class GuidUtilTests
{
    [Fact]
    public void StringToGuid_ShouldGenerateConsistentGuid()
    {
        // Arrange
        string input = "test-string";

        // Act
        Guid guid1 = GuidUtil.StringToGuid(input);
        Guid guid2 = GuidUtil.StringToGuid(input);

        // Assert
        Assert.Equal(guid1, guid2); // Ensure deterministic output
    }

    [Fact]
    public void StringToGuid_ShouldProduceValidGuid()
    {
        // Arrange
        string input = "another-test";

        // Act
        Guid guid = GuidUtil.StringToGuid(input);

        // Assert
        Assert.NotEqual(Guid.Empty, guid); // Ensure it is not an empty GUID
    }

    [Fact]
    public void GuidToGrainKey_ShouldReturnCorrectFormat()
    {
        // Arrange
        Guid guid = Guid.NewGuid();

        // Act
        string grainKey = GuidUtil.GuidToGrainKey(guid);

        // Assert
        Assert.Equal(32, grainKey.Length); // Ensure it has no dashes and is 32 chars long
        Assert.DoesNotContain("-", grainKey); // Ensure no hyphens
    }
}