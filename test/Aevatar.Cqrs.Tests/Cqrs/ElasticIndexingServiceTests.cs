using Xunit;
using Moq;
using Nest;
using Aevatar;
using Aevatar.CQRS;
using Aevatar.CQRS.Provider;
using Aevatar.Query;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Volo.Abp;


public class ElasticServiceTests : AevatarApplicationTestBase
{
    private readonly Mock<IElasticClient> _mockElasticClient;
    private readonly Mock<ILogger<ElasticIndexingService>> _mockLogger;
    private readonly IIndexingService _elasticService;
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IMemoryCache _cache;

    public ElasticServiceTests()
    {
        _mockElasticClient = new Mock<IElasticClient>();
        _mockLogger = new Mock<ILogger<ElasticIndexingService>>();
        _cqrsProvider = GetRequiredService<ICQRSProvider>();
        _cache = GetRequiredService<IMemoryCache>();
        _elasticService = new ElasticIndexingService(
            _mockLogger.Object,
            _mockElasticClient.Object,
            _cqrsProvider,
            _cache
        );
    }

    [Fact]
    public async Task QueryWithLuceneAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var queryDto = new LuceneQueryDto
        {
            Index = "test-index",
            QueryString = "level:ERROR",
            PageIndex = 0,
            PageSize = 10,
            SortFields = new List<string> { "timestamp:desc" }
        };

        var mockResponse = new Mock<ISearchResponse<Dictionary<string, object>>>();
        mockResponse.Setup(r => r.IsValid).Returns(true);
        mockResponse.Setup(r => r.Total).Returns(1L);
        mockResponse.Setup(r => r.Documents)
            .Returns(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["level"] = "ERROR" }
            });

        _mockElasticClient.Setup(x => x.SearchAsync<Dictionary<string, object>>(
                It.IsAny<SearchDescriptor<Dictionary<string, object>>>(),
                default
            ))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await _elasticService.QueryWithLuceneAsync(queryDto);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("ERROR", result.Items[0]["level"]);
    }


    [Fact]
    public async Task ShouldHandleInvalidResponse()
    {
        // Arrange
        var queryDto = new LuceneQueryDto
        {
            Index = "test-index",
            QueryString = "invalid_field:value"
        };

        var mockResponse = new Mock<ISearchResponse<Dictionary<string, object>>>();
        mockResponse.Setup(r => r.IsValid).Returns(false);

        _mockElasticClient.Setup(x => x.SearchAsync<Dictionary<string, object>>(
                It.IsAny<SearchDescriptor<Dictionary<string, object>>>(),
                default
            ))
            .ReturnsAsync(mockResponse.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _elasticService.QueryWithLuceneAsync(queryDto));

        Assert.Contains("Elasticsearch query failed", ex.Message);
    }
}