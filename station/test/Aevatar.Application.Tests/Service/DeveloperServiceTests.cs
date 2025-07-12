// ABOUTME: This file contains integration tests for DeveloperService with ABP framework
// ABOUTME: Tests validate service behavior with real dependency injection and configuration

using System;
using System.Threading.Tasks;
using Aevatar.Service;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Service;

public class DeveloperServiceTests : AevatarApplicationTestBase
{
    private readonly IDeveloperService _developerService;

    public DeveloperServiceTests()
    {
        _developerService = GetRequiredService<IDeveloperService>();
    }

    [Theory]
    [InlineData("", "new-client", "1", "http://localhost:3000")]
    [InlineData("source-client", "", "1", "http://localhost:3000")]
    [InlineData("source-client", "new-client", "", "http://localhost:3000")]
    public async Task Should_Accept_EmptyString_Parameters_With_DI_Container(string sourceClientId, string newClientId, string version, string corsUrls)
    {
        // Act & Assert - Tests with real ABP dependency injection
        await Should.NotThrowAsync(() => _developerService.CopyHostAsync(sourceClientId, newClientId, version, corsUrls));
    }

    [Fact]
    public async Task Should_Handle_Null_CorsUrls_With_DI_Container()
    {
        // Arrange
        var sourceClientId = "test-source-client";
        var newClientId = "test-new-client";
        var version = "1";
        string corsUrls = null;

        // Act & Assert - Tests with real ABP dependency injection
        await Should.NotThrowAsync(() => _developerService.CopyHostAsync(sourceClientId, newClientId, version, corsUrls));
    }

    [Fact]
    public void Should_Be_Registered_In_DI_Container()
    {
        // Assert - Verify service is properly registered in ABP DI container
        _developerService.ShouldNotBeNull();
        _developerService.ShouldBeOfType<DeveloperService>();
    }
}