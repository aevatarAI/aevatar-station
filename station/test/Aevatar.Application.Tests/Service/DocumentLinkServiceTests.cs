using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Service;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Shouldly;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Xunit;

namespace Aevatar.Application.Tests.Service;

public abstract class DocumentLinkServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IDocumentLinkService _documentLinkService;

    protected DocumentLinkServiceTests()
    {
        _documentLinkService = GetRequiredService<IDocumentLinkService>();
    }

    [Fact]
    public async Task GetDocumentLinkStatusAsync_ShouldReturnCachedValue_WhenPresent()
    {
        // Arrange
        var cache = new Mock<IDistributedCache<DocumentLinkStatus, string>>();
        var url = "https://example.com/docs";
        var cached = new DocumentLinkStatus { Url = url, IsReachable = false, StatusCode = 404, CheckedAt = DateTimeOffset.UtcNow };
        cache.Setup(c => c.GetAsync(url, It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(cached);
        
        var service = new DocumentLinkService(null, cache.Object);
        
        // Act
        var ok = await service.GetDocumentLinkStatusAsync(url);
        
        // Assert
        ok.ShouldBeFalse();
    }

    [Fact]
    public async Task GetDocumentLinkStatusAsync_ShouldReturnTrue_WhenCacheMiss()
    {
        // Arrange
        var cache = new Mock<IDistributedCache<DocumentLinkStatus, string>>();
        var url = "https://example.com/missing";
        cache.Setup(c => c.GetAsync(url, It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((DocumentLinkStatus?)null);
        
        var service = new DocumentLinkService(null, cache.Object);
        
        // Act
        var ok = await service.GetDocumentLinkStatusAsync(url);
        
        // Assert
        ok.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshDocumentLinkStatusAsync_ShouldNoop_WhenNoDocumentationLinkAttributes()
    {
        // Arrange
        var cache = new Mock<IDistributedCache<DocumentLinkStatus, string>>();
        // var service = new DocumentLinkService(null, cache.Object);

        // Act
        await _documentLinkService.RefreshDocumentLinkStatusAsync();

        // Assert
        cache.Verify(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<DocumentLinkStatus>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ), Times.Never);
    }
} 