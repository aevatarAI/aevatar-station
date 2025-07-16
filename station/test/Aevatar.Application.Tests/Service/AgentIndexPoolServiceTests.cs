using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Aevatar.Application.Service;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Tests.Service
{
    public class AgentIndexPoolServiceTests : IDisposable
    {
        private readonly Mock<IAgentScannerService> _mockAgentScannerService;
        private readonly Mock<ILogger<AgentIndexPoolService>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private readonly AgentIndexPoolService _service;

        public AgentIndexPoolServiceTests()
        {
            _mockAgentScannerService = new Mock<IAgentScannerService>();
            _mockLogger = new Mock<ILogger<AgentIndexPoolService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            
            _service = new AgentIndexPoolService(
                _mockAgentScannerService.Object,
                _memoryCache,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllAgentsAsync_WhenCalled_ShouldReturnActiveAgents()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            // Act
            var result = await _service.GetAllAgentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Only active agents should be returned
            Assert.All(result, agent => Assert.True(agent.IsActive));
        }

        [Fact]
        public async Task GetAgentByIdAsync_WithValidId_ShouldReturnAgent()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            var targetAgentId = testAgents.First(a => a.IsActive).Id;

            // Act
            var result = await _service.GetAgentByIdAsync(targetAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetAgentId, result.Id);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetAgentByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            // Act
            var result = await _service.GetAgentByIdAsync("invalid-id");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAgentByIdAsync_WithNullOrEmptyId_ShouldReturnNull()
        {
            // Act
            var resultNull = await _service.GetAgentByIdAsync(null);
            var resultEmpty = await _service.GetAgentByIdAsync("");

            // Assert
            Assert.Null(resultNull);
            Assert.Null(resultEmpty);
        }

        [Fact]
        public async Task GetAgentByIdAsync_WithInactiveAgentId_ShouldReturnNull()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            var inactiveAgentId = testAgents.First(a => !a.IsActive).Id;

            // Act
            var result = await _service.GetAgentByIdAsync(inactiveAgentId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SearchAgentsAsync_WithoutFilters_ShouldReturnAllActiveAgents()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            // Act
            var result = await _service.SearchAgentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Only active agents
            Assert.All(result, agent => Assert.True(agent.IsActive));
        }

        [Fact]
        public async Task SearchAgentsAsync_WithSearchTerm_ShouldReturnMatchingAgents()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            // Act
            var result = await _service.SearchAgentsAsync("DataProcessor");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("DataProcessor", result.First().Name);
        }

        [Fact]
        public async Task SearchAgentsAsync_WithCategory_ShouldReturnMatchingAgents()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            // Act
            var result = await _service.SearchAgentsAsync(category: "DataProcessing");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("DataProcessing", result.First().Category);
        }

        [Fact]
        public async Task SearchAgentsAsync_WithComplexityLevel_ShouldReturnMatchingAgents()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            // Act
            var result = await _service.SearchAgentsAsync(complexityLevel: ComplexityLevel.Simple);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(ComplexityLevel.Simple, result.First().ComplexityLevel);
        }

        [Fact]
        public async Task SearchAgentsAsync_WithMultipleFilters_ShouldReturnMatchingAgents()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            // Act
            var result = await _service.SearchAgentsAsync(
                searchTerm: "TextProcessor",
                category: "TextProcessing",
                complexityLevel: ComplexityLevel.Medium);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var agent = result.First();
            Assert.Contains("TextProcessor", agent.Name);
            Assert.Equal("TextProcessing", agent.Category);
            Assert.Equal(ComplexityLevel.Medium, agent.ComplexityLevel);
        }

        [Fact]
        public async Task RefreshIndexAsync_WhenCalled_ShouldUpdateIndex()
        {
            // Arrange
            var initialAgents = CreateTestAgents().Take(1).ToList();
            var updatedAgents = CreateTestAgents();
            
            _mockAgentScannerService.SetupSequence(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(initialAgents)
                .ReturnsAsync(updatedAgents);

            // Act - Initial load
            await _service.GetAllAgentsAsync();
            var initialResult = await _service.GetAllAgentsAsync();
            
            // Act - Refresh
            await _service.RefreshIndexAsync();
            var refreshedResult = await _service.GetAllAgentsAsync();

            // Assert
            Assert.Single(initialResult);
            Assert.Equal(2, refreshedResult.Count);
        }

        [Fact]
        public async Task RefreshIndexAsync_WithScannerFailure_ShouldThrowException()
        {
            // Arrange
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ThrowsAsync(new InvalidOperationException("Scanner failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RefreshIndexAsync());
        }

        [Fact]
        public async Task GetAllAgentsAsync_MultipleCallsAfterInitialization_ShouldUseCachedData()
        {
            // Arrange
            var testAgents = CreateTestAgents();
            _mockAgentScannerService
                .Setup(x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()))
                .ReturnsAsync(testAgents);

            // Act
            var result1 = await _service.GetAllAgentsAsync();
            var result2 = await _service.GetAllAgentsAsync();
            var result3 = await _service.GetAllAgentsAsync();

            // Assert
            Assert.Equal(result1.Count, result2.Count);
            Assert.Equal(result2.Count, result3.Count);
            
            // Verify scanner was called only once for initialization
            _mockAgentScannerService.Verify(
                x => x.ScanAgentsAsync(It.IsAny<IEnumerable<System.Reflection.Assembly>>()), 
                Times.Once);
        }

        [Fact]
        public void Constructor_WithNullDependencies_ShouldThrowArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AgentIndexPoolService(null, _memoryCache, _mockLogger.Object));
            
            Assert.Throws<ArgumentNullException>(() => 
                new AgentIndexPoolService(_mockAgentScannerService.Object, null, _mockLogger.Object));
            
            Assert.Throws<ArgumentNullException>(() => 
                new AgentIndexPoolService(_mockAgentScannerService.Object, _memoryCache, null));
        }

        public void Dispose()
        {
            _memoryCache?.Dispose();
            _service?.Dispose();
        }

        private List<AgentIndexInfo> CreateTestAgents()
        {
            return new List<AgentIndexInfo>
            {
                new AgentIndexInfo
                {
                    Id = "agent1",
                    Name = "DataProcessor",
                    L1Description = "Processes various data formats and transforms them according to specified rules and requirements efficiently",
                    L2Description = "A comprehensive data processing agent that handles multiple input formats including JSON, XML, CSV, and binary data. It performs data validation, transformation, filtering, and aggregation operations. The agent supports custom transformation rules, data schema validation, and can output results in various formats. It includes error handling, logging, and progress tracking capabilities for large dataset processing.",
                    Category = "DataProcessing",
                    ComplexityLevel = ComplexityLevel.Simple,
                    EstimatedExecutionTime = 30,
                    RequiredParameters = new Dictionary<string, string> { { "inputData", "Data to process" } },
                    OutputDescription = "Processed data in specified format",
                    Dependencies = new List<string> { "DataValidator" },
                    Tags = new List<string> { "data", "processing", "transformation" },
                    Version = "1.0.0",
                    IsActive = true,
                    TypeFullName = "TestNamespace.DataProcessor",
                    AssemblyName = "TestAssembly"
                },
                new AgentIndexInfo
                {
                    Id = "agent2",
                    Name = "TextProcessor",
                    L1Description = "Advanced text processing agent that performs natural language operations including parsing and analysis tasks",
                    L2Description = "A sophisticated text processing agent designed for natural language processing tasks. It supports text tokenization, sentence parsing, named entity recognition, sentiment analysis, and language detection. The agent can handle multiple languages and text encodings, perform text summarization, keyword extraction, and content classification. It includes advanced features like semantic similarity comparison, text clustering, and automated content tagging for comprehensive text analysis workflows.",
                    Category = "TextProcessing",
                    ComplexityLevel = ComplexityLevel.Medium,
                    EstimatedExecutionTime = 45,
                    RequiredParameters = new Dictionary<string, string> { { "text", "Text to process" } },
                    OutputDescription = "Processed text results",
                    Dependencies = new List<string> { "NLPLibrary" },
                    Tags = new List<string> { "text", "nlp", "processing" },
                    Version = "2.0.0",
                    IsActive = true,
                    TypeFullName = "TestNamespace.TextProcessor",
                    AssemblyName = "TestAssembly"
                },
                new AgentIndexInfo
                {
                    Id = "agent3",
                    Name = "InactiveAgent",
                    L1Description = "This agent is currently inactive and should not appear in normal operations or search results from users",
                    L2Description = "An agent that has been marked as inactive due to maintenance, deprecation, or other operational reasons. While the agent's functionality remains intact, it is temporarily or permanently disabled from being used in workflow orchestration. This agent serves as a test case for validation of active/inactive agent filtering in the indexing and search systems.",
                    Category = "Testing",
                    ComplexityLevel = ComplexityLevel.Simple,
                    EstimatedExecutionTime = 10,
                    RequiredParameters = new Dictionary<string, string>(),
                    OutputDescription = "No output - agent is inactive",
                    Dependencies = new List<string>(),
                    Tags = new List<string> { "test", "inactive" },
                    Version = "1.0.0",
                    IsActive = false, // This agent is inactive
                    TypeFullName = "TestNamespace.InactiveAgent",
                    AssemblyName = "TestAssembly"
                }
            };
        }
    }
} 