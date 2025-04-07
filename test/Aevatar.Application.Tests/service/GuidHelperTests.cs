using System;
using Aevatar.Service;
using Xunit;

namespace Aevatar.service
{
    public class GuidHelperTests
    {
        [Fact]
        public void GenerateId_ShouldJoinWithUnderscore()
        {
            // Act
            var result = GuidHelper.GenerateId("abc", "123");

            // Assert
            Assert.Equal("abc_123", result);
        }

        [Fact]
        public void GenerateGrainId_ShouldJoinWithDash()
        {
            // Act
            var result = GuidHelper.GenerateGrainId("order", 1001, "done");

            // Assert
            Assert.Equal("order-1001-done", result);
        }

        [Fact]
        public void UniqGuid_SameInput_ShouldReturnSameGuid()
        {
            // Arrange
            var input1 = new[] { "user", "42" };
            var input2 = new[] { "user", "42" };

            // Act
            var guid1 = GuidHelper.UniqGuid(input1);
            var guid2 = GuidHelper.UniqGuid(input2);

            // Assert
            Assert.Equal(guid1, guid2);
        }

        [Fact]
        public void UniqGuid_DifferentInput_ShouldReturnDifferentGuid()
        {
            // Act
            var guid1 = GuidHelper.UniqGuid("a", "b");
            var guid2 = GuidHelper.UniqGuid("x", "y");

            // Assert
            Assert.NotEqual(guid1, guid2);
        }

        [Fact]
        public void UniqGuid_EmptyInput_ShouldStillReturnValidGuid()
        {
            // Act
            var guid = GuidHelper.UniqGuid();

            // Assert
            Assert.NotEqual(Guid.Empty, guid);
        }
    }
}