// ABOUTME: This file contains unit tests for the TypeMetadataStartupTask implementation
// ABOUTME: Tests cover startup initialization, statistics monitoring, and error handling

using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Application.Grains;
using Aevatar.Application.Services;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Services
{
    public class TypeMetadataStartupTaskTests
    {
        private readonly Mock<ITypeMetadataService> _mockTypeMetadataService;
        private readonly Mock<ILogger<TypeMetadataStartupTask>> _mockLogger;
        private readonly TypeMetadataStartupTask _startupTask;

        public TypeMetadataStartupTaskTests()
        {
            _mockTypeMetadataService = new Mock<ITypeMetadataService>();
            _mockLogger = new Mock<ILogger<TypeMetadataStartupTask>>();
            _startupTask = new TypeMetadataStartupTask(_mockTypeMetadataService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Should_RefreshMetadata_When_ExecuteAsyncCalled()
        {
            // Arrange
            var mockStats = new MetadataStats
            {
                TotalTypes = 5,
                SizeInBytes = 2048,
                PercentageOf16MB = 0.012
            };
            
            _mockTypeMetadataService.Setup(s => s.RefreshMetadataAsync())
                .Returns(Task.CompletedTask);
            _mockTypeMetadataService.Setup(s => s.GetStatsAsync())
                .ReturnsAsync(mockStats);
            
            // Act
            await _startupTask.Execute(CancellationToken.None);
            
            // Assert
            _mockTypeMetadataService.Verify(s => s.RefreshMetadataAsync(), Times.Once);
            _mockTypeMetadataService.Verify(s => s.GetStatsAsync(), Times.Once);
        }

        [Fact]
        public async Task Should_LogInformation_When_StartupSuccessful()
        {
            // Arrange
            var mockStats = new MetadataStats
            {
                TotalTypes = 3,
                SizeInBytes = 1024,
                PercentageOf16MB = 0.006
            };
            
            _mockTypeMetadataService.Setup(s => s.RefreshMetadataAsync())
                .Returns(Task.CompletedTask);
            _mockTypeMetadataService.Setup(s => s.GetStatsAsync())
                .ReturnsAsync(mockStats);
            
            // Act
            await _startupTask.Execute(CancellationToken.None);
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting TypeMetadata initialization")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
                
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TypeMetadata initialization completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_LogWarning_When_CapacityExceeds80Percent()
        {
            // Arrange
            var mockStats = new MetadataStats
            {
                TotalTypes = 1000,
                SizeInBytes = 14 * 1024 * 1024, // 14MB
                PercentageOf16MB = 87.5 // 87.5% of 16MB
            };
            
            _mockTypeMetadataService.Setup(s => s.RefreshMetadataAsync())
                .Returns(Task.CompletedTask);
            _mockTypeMetadataService.Setup(s => s.GetStatsAsync())
                .ReturnsAsync(mockStats);
            
            // Act
            await _startupTask.Execute(CancellationToken.None);
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("approaching MongoDB 16MB limit")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_LogError_When_RefreshMetadataFails()
        {
            // Arrange
            var exception = new Exception("Metadata refresh failed");
            _mockTypeMetadataService.Setup(s => s.RefreshMetadataAsync())
                .ThrowsAsync(exception);
            
            // Act & Assert
            var thrownException = await Should.ThrowAsync<Exception>(() => _startupTask.Execute(CancellationToken.None));
            thrownException.Message.ShouldBe("Metadata refresh failed");
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to initialize TypeMetadata during startup")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_LogError_When_GetStatsFails()
        {
            // Arrange
            _mockTypeMetadataService.Setup(s => s.RefreshMetadataAsync())
                .Returns(Task.CompletedTask);
            _mockTypeMetadataService.Setup(s => s.GetStatsAsync())
                .ThrowsAsync(new Exception("Stats retrieval failed"));
            
            // Act & Assert
            var thrownException = await Should.ThrowAsync<Exception>(() => _startupTask.Execute(CancellationToken.None));
            thrownException.Message.ShouldBe("Stats retrieval failed");
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to initialize TypeMetadata during startup")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_HandleCancellation_When_CancellationRequested()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            
            _mockTypeMetadataService.Setup(s => s.RefreshMetadataAsync())
                .Returns(Task.CompletedTask);
            
            // Act & Assert
            await Should.ThrowAsync<OperationCanceledException>(() => 
                _startupTask.Execute(cancellationTokenSource.Token));
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_TypeMetadataServiceIsNull()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                new TypeMetadataStartupTask(null, _mockLogger.Object));
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                new TypeMetadataStartupTask(_mockTypeMetadataService.Object, null));
        }
    }
}