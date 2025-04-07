using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Agents.Creator;
using Aevatar.Agents.Creator.Models;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Aevatar.Options;
using Aevatar.Schema;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NJsonSchema;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
using Xunit;

namespace Aevatar.service;

public class SampleAgentConfig(string env) : ConfigurationBase
{
    public string Env { get; set; } = env;
    public int Replicas { get; set; }
}

public class FakeSchema : JsonSchema
{
    public new string ToJson()
    {
        return "{}";
    }
}
public class AgentServiceTests
{
    private readonly AgentService _agentService;
    private readonly Mock<IClusterClient> _mockClusterClient = new();
    private readonly Mock<ICQRSProvider> _mockCqrsProvider = new();
    private readonly Mock<ILogger<AgentService>> _mockLogger = new();
    private readonly Mock<IGAgentFactory> _mockGAgentFactory = new();
    private readonly Mock<IGAgentManager> _mockGAgentManager = new();
    private readonly Mock<IUserAppService> _mockUserAppService = new();
    private readonly Mock<IOptionsMonitor<AgentOptions>> _mockOptions = new();
    private readonly Mock<GrainTypeResolver> _mockGrainTypeResolver = new();
    private readonly Mock<ISchemaProvider> _mockSchemaProvider = new();

    public AgentServiceTests()
    {
        _mockOptions.Setup(x => x.CurrentValue).Returns(new AgentOptions
        {
            SystemAgentList = new List<string>()
        });

        _agentService = new AgentService(
            _mockClusterClient.Object,
            _mockCqrsProvider.Object,
            _mockLogger.Object,
            _mockGAgentFactory.Object,
            _mockGAgentManager.Object,
            _mockUserAppService.Object,
            _mockOptions.Object,
            null,
            _mockSchemaProvider.Object
        );
    }

    [Fact]
    public async Task GetAgentEventLogsAsync_ShouldReturnEmptyList_WhenNoEvents()
    {
        // Arrange
        var agentId = Guid.NewGuid().ToString();

        // Mock ViewGroupTreeAsync → Calls BuildGroupTreeAsync → Attempts to parse agentId as Guid
        var validGuid = Guid.Parse(agentId);

        // Mock CQRS query, directly returning empty
        _mockCqrsProvider.Setup(p => p.QueryGEventAsync("",
                It.Is<List<string>>(list => list.Contains(agentId)),
                0, 10))
            .ReturnsAsync(Tuple.Create(0L, new List<AgentGEventIndex>()));

        // Only mock cluster grain → Empty tree structure (no further child node simulation)
        var mockGrain = new Mock<ICreatorGAgent>();
        mockGrain.Setup(g => g.GetChildrenAsync())
            .ReturnsAsync(new List<GrainId>());

        _mockClusterClient.Setup(x => x.GetGrain<ICreatorGAgent>(validGuid, null))
            .Returns(mockGrain.Object);

        // Act
        var result = await _agentService.GetAgentEventLogsAsync(agentId, 0, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0L, result.Item1);
        Assert.Empty(result.Item2);
    }

    [Fact]
    public async Task GetAgentAsync_ShouldMapFromCreatorGAgentStateToAgentDto()
    {
        // Arrange
        var guid = "21597e4d-69cc-4cbd-be04-8f190048db20";
        var userId = Guid.NewGuid(); // owner id
        var agentType = "TestType";
        var name = "SampleAgent";
        var businessGrainId =
            GrainId.Parse("Aevatar.SignalR.GAgents.SignalRGAgent/21597e4d69cc4cbdbe048f190048db20");
        var createTime = DateTime.UtcNow;

        var propertiesDict = new Dictionary<string, object>
        {
            { "env", "staging" },
            { "replicas", 5 }
        };
        var propertiesJson = JsonConvert.SerializeObject(propertiesDict);

        var state = new CreatorGAgentState
        {
            Id = guid.ToGuid(),
            UserId = userId,
            AgentType = agentType,
            Name = name,
            Properties = propertiesJson,
            BusinessAgentGrainId = businessGrainId,
            EventInfoList = new List<EventDescription>(),
            CreateTime = createTime
        };

        // Mock current user
        _mockUserAppService.Setup(x => x.GetCurrentUserId()).Returns(userId);

        // Mock Creator agent
        var creatorGrainMock = new Mock<ICreatorGAgent>();
        creatorGrainMock.Setup(x => x.GetAgentAsync()).ReturnsAsync(state);
        _mockClusterClient.Setup(x => x.GetGrain<ICreatorGAgent>(guid.ToGuid(), null))
            .Returns(creatorGrainMock.Object);

        // ✅ Mock business agent GetConfigurationTypeAsync
        var mockBusinessAgent = new Mock<IGAgent>();
        mockBusinessAgent.Setup(x => x.GetConfigurationTypeAsync())
            .ReturnsAsync(typeof(Dictionary<string, object>));

        _mockGAgentFactory.Setup(x => x.GetGAgentAsync(businessGrainId, null))
            .ReturnsAsync(mockBusinessAgent.Object);

        // ✅ SchemaProvider mock to avoid null.ToJson()
        _mockSchemaProvider.Setup(x => x.GetTypeSchema(typeof(Dictionary<string, object>)))
            .Returns(new FakeSchema());

        // Act
        var result = await _agentService.GetAgentAsync(guid.ToGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(guid.ToGuid(), result.Id);
        Assert.Equal(agentType, result.AgentType);
        Assert.Equal(name, result.Name);
        Assert.Equal(businessGrainId, result.GrainId);
        Assert.Equal(guid, result.AgentGuid.ToString());
        Assert.Equal(2, result.Properties!.Count);
        Assert.Equal("staging", result.Properties["env"]);
        Assert.Equal(5L, Convert.ToInt64(result.Properties["replicas"]));
    }
    
    // [Fact]
    // public void CalculateScore_ShouldReturnCorrectValue()
    // {
    //     // Arrange: mock依赖（可选）
    //     var mockLogger = new Mock<ILogger<AgentService>>();
    //     var service = new AgentService(...);  // 传入你真实依赖或 mock 对象
    //
    //     // 使用反射获取 private 方法
    //     var method = typeof(AgentService).GetMethod("CalculateScore",
    //         BindingFlags.NonPublic | BindingFlags.Instance);
    //
    //     Assert.NotNull(method);
    //
    //     // Act
    //     var result = method.Invoke(service, new object[] { 5, 10 });
    //
    //     // Assert
    //     Assert.Equal(50, result);
    // }
}

