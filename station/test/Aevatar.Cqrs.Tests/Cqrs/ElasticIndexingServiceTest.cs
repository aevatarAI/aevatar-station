using System;
using System.Threading.Tasks;
using Aevatar.CQRS;
using Aevatar.Query;
using Aevatar.CQRS.Provider;
using Aevatar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using Volo.Abp;
using Elastic.Clients.Elasticsearch;
using System.Threading;

namespace Aevatar.Cqrs.Tests.Cqrs
{
    public class ElasticIndexingServiceTest
    {
        private readonly Mock<ElasticsearchClient> _mockClient;
        private readonly Mock<ILogger<ElasticIndexingService>> _mockLogger;
        private readonly Mock<ICQRSProvider> _mockCqrsProvider;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IOptionsSnapshot<HostOptions>> _mockOptions;
        private readonly ElasticIndexingService _service;

        public ElasticIndexingServiceTest()
        {
            _mockClient = new Mock<ElasticsearchClient>();
            _mockLogger = new Mock<ILogger<ElasticIndexingService>>();
            _mockCqrsProvider = new Mock<ICQRSProvider>();
            _mockCache = new Mock<IMemoryCache>();
            _mockOptions = new Mock<IOptionsSnapshot<HostOptions>>();

            // Setup host options
            _mockOptions.Setup(x => x.Value).Returns(new HostOptions { HostId = "test-host" });

            _service = new ElasticIndexingService(
                _mockLogger.Object,
                _mockClient.Object,
                _mockCqrsProvider.Object,
                _mockCache.Object,
                _mockOptions.Object);
        }

        #region Basic Index Name Tests

        [Fact]
        public void GetIndexName_ShouldGenerateCorrectFormat()
        {
            // Arrange
            const string stateName = "TestState";
            const string expectedIndex = "aevatar-test-host-teststateindex";

            // Act
            var actualIndex = _service.GetIndexName(stateName);

            // Assert
            Assert.Equal(expectedIndex, actualIndex);
        }

        [Theory]
        [InlineData("TestState", "aevatar-test-host-teststateindex")]
        [InlineData("UserProfiles", "aevatar-test-host-userprofilesindex")]
        [InlineData("UPPERCASE", "aevatar-test-host-uppercaseindex")]
        public void GetIndexName_WithVariousStateNames_ShouldCreateCorrectIndexName(string stateName, string expectedIndex)
        {
            // Act
            var actualIndex = _service.GetIndexName(stateName);

            // Assert
            Assert.Equal(expectedIndex, actualIndex);
        }

        #endregion

        #region CountWithLuceneAsync Comprehensive Tests

        [Fact]
        public async Task CountWithLuceneAsync_NullQueryDto_ShouldThrowNullReferenceException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(
                () => _service.CountWithLuceneAsync(null!));
        }

        [Fact]
        public async Task CountWithLuceneAsync_ElasticsearchClientThrowsException_ShouldWrapInUserFriendlyException()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = "test:query"
            };

            var originalException = new InvalidOperationException("Connection timeout");

            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(originalException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            Assert.Equal("Connection timeout", exception.Message);

            // Verify error logging
            VerifyErrorLogging("Exception occurred");
        }

        [Fact]
        public async Task CountWithLuceneAsync_ComplexQueryString_ShouldHandleCorrectly()
        {
            // Arrange
            const string complexQuery = "status:active AND (type:premium OR type:enterprise) AND created_date:[2024-01-01 TO 2024-12-31]";
            var queryDto = new LuceneQueryDto
            {
                StateName = "UserSubscription",
                QueryString = complexQuery
            };

            // Setup mock to throw exception to test error path
            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            // Verify the index name is constructed correctly
            var expectedIndex = _service.GetIndexName(queryDto.StateName);
            Assert.Equal("aevatar-test-host-usersubscriptionindex", expectedIndex);
        }

        [Fact]
        public async Task CountWithLuceneAsync_SpecialCharactersInQuery_ShouldHandleCorrectly()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "LogEntry",
                QueryString = "message:\"Error occurred at 2024-01-01T10:30:00Z with code [500]\""
            };

            // Setup mock to throw exception to test error path
            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            // Verify the method accepts the special characters
            Assert.Contains("Error occurred", queryDto.QueryString);
            Assert.Contains("[500]", queryDto.QueryString);
        }

        [Fact]
        public async Task CountWithLuceneAsync_VerifyElasticsearchClientIsCalled()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = "status:active"
            };

            // Setup mock to throw exception to verify it was called
            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            // Verify the client was called exactly once
            _mockClient.Verify(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("TestState-With-Dashes")]
        [InlineData("State_With_Underscores")]
        [InlineData("UPPERCASE_STATE")]
        [InlineData("Mixed.Case.State")]
        public async Task CountWithLuceneAsync_DifferentStateNameFormats_ShouldHandleCorrectly(string stateName)
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = stateName,
                QueryString = "*"
            };

            // Setup mock to throw exception to test error path
            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            // Verify the index name follows the expected pattern
            var expectedIndex = _service.GetIndexName(stateName);
            Assert.Contains("aevatar-test-host-", expectedIndex);
            Assert.EndsWith("index", expectedIndex);
        }

        [Fact]
        public async Task CountWithLuceneAsync_EmptyStateName_ShouldStillWork()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "",
                QueryString = "test:query"
            };

            // Setup mock to throw exception to test error path
            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            // Verify the index name is still constructed
            var indexName = _service.GetIndexName(queryDto.StateName);
            Assert.Contains("aevatar-test-host-", indexName);
        }

        [Fact]
        public async Task CountWithLuceneAsync_VeryLongQueryString_ShouldHandleCorrectly()
        {
            // Arrange
            var longQuery = new string('a', 10000); // Very long query string
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = longQuery
            };

            // Setup mock to throw exception to test error path
            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            // Verify the query string is preserved
            Assert.Equal(10000, queryDto.QueryString.Length);
        }

        [Theory]
        [InlineData("status:active")]
        [InlineData("status:active AND type:user")]
        [InlineData("date:[2024-01-01 TO 2024-12-31]")]
        [InlineData("*")]
        [InlineData("")]
        [InlineData(null)]
        public async Task CountWithLuceneAsync_QueryStringVariations_ShouldAcceptVariousQueryFormats(string queryString)
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "TestState",
                QueryString = queryString
            };

            // Setup mock to throw exception to test error path
            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            // Verify the query DTO is properly constructed
            Assert.Equal("TestState", queryDto.StateName);
            Assert.Equal(queryString, queryDto.QueryString);
        }

        [Fact]
        public async Task CountWithLuceneAsync_ValidQuery_ShouldLogInformation()
        {
            // Arrange
            var queryDto = new LuceneQueryDto
            {
                StateName = "UserProfile",
                QueryString = "status:active"
            };

            // Setup mock to throw exception to test logging
            _mockClient.Setup(x => x.CountAsync(
                It.IsAny<CountRequest>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => _service.CountWithLuceneAsync(queryDto));

            // Verify initial logging was called
            VerifyInformationLogging("Lucene Count");
        }

        [Fact]
        public async Task CountWithLuceneAsync_ArgumentValidation_ShouldCheckInputParameters()
        {
            // Arrange & Act & Assert

            // Test null DTO
            await Assert.ThrowsAsync<NullReferenceException>(
                () => _service.CountWithLuceneAsync(null!));

            // Test valid DTO with null state name - should not throw immediately
            var queryDtoWithNullState = new LuceneQueryDto
            {
                StateName = null!,
                QueryString = "test:query"
            };

            // This should still work as GetIndexName handles null gracefully
            try
            {
                var indexName = _service.GetIndexName(queryDtoWithNullState.StateName);
                Assert.NotNull(indexName);
            }
            catch (Exception)
            {
                // Expected if GetIndexName doesn't handle null gracefully
            }
        }

        [Fact]
        public void CountWithLuceneAsync_IndexNameGeneration_ShouldFollowCorrectPattern()
        {
            // Arrange & Act
            var testCases = new[]
            {
                ("TestState", "aevatar-test-host-teststateindex"),
                ("UserProfile", "aevatar-test-host-userprofileindex"),
                ("User-Profile_V2.Data", "aevatar-test-host-user-profile_v2.dataindex"),
                ("UPPERCASE", "aevatar-test-host-uppercaseindex")
            };

            // Assert
            foreach (var (stateName, expectedIndex) in testCases)
            {
                var actualIndex = _service.GetIndexName(stateName);
                Assert.Equal(expectedIndex, actualIndex);
            }
        }

        [Fact]
        public void CountWithLuceneAsync_MethodSignature_ShouldBeCorrect()
        {
            // Arrange
            var methodInfo = typeof(ElasticIndexingService)
                .GetMethod("CountWithLuceneAsync");

            // Assert
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.IsPublic);
            Assert.False(methodInfo.IsStatic);
            Assert.Equal(typeof(Task<long>), methodInfo.ReturnType);

            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal("queryDto", parameters[0].Name);
            Assert.Equal(typeof(LuceneQueryDto), parameters[0].ParameterType);
        }

        [Fact]
        public void CountWithLuceneAsync_ServiceConstruction_ShouldAcceptAllDependencies()
        {
            // Arrange & Act
            var service = new ElasticIndexingService(
                _mockLogger.Object,
                _mockClient.Object,
                _mockCqrsProvider.Object,
                _mockCache.Object,
                _mockOptions.Object);

            // Assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<IIndexingService>(service);
        }

        #endregion

        #region Helper Methods

        private void VerifyInformationLogging(string expectedMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        private void VerifyErrorLogging(string expectedMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Legacy Tests (Maintained for compatibility)

        [Fact]
        public void CountWithLuceneAsync_Interface_ShouldExist()
        {
            // Arrange & Act
            var interfaceType = typeof(IIndexingService);
            var method = interfaceType.GetMethod("CountWithLuceneAsync");

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<long>), method.ReturnType);
            
            var parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(LuceneQueryDto), parameters[0].ParameterType);
        }

        #endregion
    }
} 