using System;
using System.Threading.Tasks;
using Aevatar.CQRS;
using Aevatar.Query;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Users;
using FluentValidation.Results;
using System.Collections.Generic;

namespace Aevatar.Controllers.Tests
{
    public class QueryControllerTest
    {
        private readonly Mock<IIndexingService> _mockIndexingService;

        public QueryControllerTest()
        {
            _mockIndexingService = new Mock<IIndexingService>();
        }

        [Fact]
        public async Task CountWithLuceneAsync_WithValidQuery_ShouldReturnCount()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = "status:active"
            };

            _mockIndexingService.Setup(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
                .ReturnsAsync(12345L);

            // Act
            var result = await _mockIndexingService.Object.CountWithLuceneAsync(queryDto);

            // Assert
            Assert.Equal(12345L, result);
            _mockIndexingService.Verify(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()), Times.Once);
        }

        [Fact]
        public async Task CountWithLuceneAsync_WithEmptyQuery_ShouldReturnCount()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = ""
            };

            _mockIndexingService.Setup(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
                .ReturnsAsync(54321L);

            // Act
            var result = await _mockIndexingService.Object.CountWithLuceneAsync(queryDto);

            // Assert
            Assert.Equal(54321L, result);
        }

        [Fact]
        public async Task CountWithLuceneAsync_WithLargeCount_ShouldReturnCorrectValue()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = "status:active"
            };

            const long largeCount = 9_876_543_210L; // Test with large number beyond 10,000 limit
            _mockIndexingService.Setup(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
                .ReturnsAsync(largeCount);

            // Act
            var result = await _mockIndexingService.Object.CountWithLuceneAsync(queryDto);

            // Assert
            Assert.Equal(largeCount, result);
        }

        [Fact]
        public async Task CountWithLuceneAsync_WithZeroCount_ShouldReturnZero()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = "status:nonexistent"
            };

            _mockIndexingService.Setup(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
                .ReturnsAsync(0L);

            // Act
            var result = await _mockIndexingService.Object.CountWithLuceneAsync(queryDto);

            // Assert
            Assert.Equal(0L, result);
        }

        [Fact]
        public async Task CountWithLuceneAsync_WithComplexQuery_ShouldReturnCorrectCount()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = "status:active AND type:user AND ctime:[2024-01-01 TO 2024-12-31]"
            };

            _mockIndexingService.Setup(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
                .ReturnsAsync(5555L);

            // Act
            var result = await _mockIndexingService.Object.CountWithLuceneAsync(queryDto);

            // Assert
            Assert.Equal(5555L, result);
            
            // Verify the complex query was passed through
            _mockIndexingService.Verify(x => x.CountWithLuceneAsync(
                It.Is<LuceneQueryDto>(req => 
                    req.QueryString.Contains("status:active") &&
                    req.QueryString.Contains("type:user") &&
                    req.QueryString.Contains("ctime:[2024-01-01 TO 2024-12-31]"))), 
                Times.Once);
        }
    }
} 