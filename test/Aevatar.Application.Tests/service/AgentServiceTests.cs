// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Aevatar.Agent;
// using Aevatar.Service;
// using Shouldly;
// using Volo.Abp;
// using Xunit;
//
// namespace Aevatar.service
// {
//     public sealed class AgentServiceTests : AevatarApplicationTestBase
//     {
//         private readonly IAgentService _agentService;
//
//         public AgentServiceTests()
//         {
//             // 从依赖注入中获取实际服务
//             _agentService = GetRequiredService<IAgentService>();
//         }
//
//         [Fact]
//         public async Task GetAgentEventLogsAsync_ShouldReturnLogs()
//         {
//             // Arrange
//             var agentId = Guid.NewGuid().ToString();
//             var pageNumber = 1;
//             var pageSize = 10;
//
//             // Act
//             var result = await _agentService.GetAgentEventLogsAsync(agentId, pageNumber, pageSize);
//
//             // Assert
//             result.ShouldNotBeNull();
//             result.Item1.ShouldBe(0); // 假设没有事件日志
//             result.Item2.ShouldBeEmpty(); // 验证返回的事件日志列表为空
//         }
//
//         [Fact]
//         public async Task CreateAgentAsync_ShouldCreateAgentSuccessfully()
//         {
//             // Arrange
//             var agentType = "TestAgentType";
//             var dto = new CreateAgentInputDto
//             {
//                 AgentType = agentType,
//                 Name = "TestAgent",
//                 Properties = new Dictionary<string, object>
//                 {
//                     { "Key", "Value" }
//                 }
//             };
//
//             // Act
//             var result = await _agentService.CreateAgentAsync(dto);
//
//             // Assert
//             result.ShouldNotBeNull();
//             result.Name.ShouldBe(dto.Name);
//             result.AgentType.ShouldBe(agentType);
//             result.Properties.ShouldContainKey("Key");
//         }
//
//         [Fact]
//         public async Task AddSubAgentAsync_ShouldAddSubAgent()
//         {
//             // Arrange
//             var parentAgentId = Guid.NewGuid();
//             var subAgentId = Guid.NewGuid();
//
//             // 先创建父代理
//             await _agentService.CreateAgentAsync(new CreateAgentInputDto
//             {
//                 AgentId = parentAgentId,
//                 AgentType = "TestParentAgent",
//                 Name = "ParentAgent",
//                 Properties = null
//             });
//
//             // 再创建子代理
//             await _agentService.CreateAgentAsync(new CreateAgentInputDto
//             {
//                 AgentId = subAgentId,
//                 AgentType = "TestSubAgent",
//                 Name = "SubAgent",
//                 Properties = null
//             });
//
//             var addSubAgentDto = new AddSubAgentDto
//             {
//                 SubAgents = new List<Guid> { subAgentId }
//             };
//
//             // Act
//             var result = await _agentService.AddSubAgentAsync(parentAgentId, addSubAgentDto);
//
//             // Assert
//             result.ShouldNotBeNull();
//             result.SubAgents.ShouldContain(subAgentId);
//         }
//
//         [Fact]
//         public async Task RemoveSubAgentAsync_ShouldRemoveSubAgent()
//         {
//             // Arrange
//             var parentAgentId = Guid.NewGuid();
//             var subAgentId = Guid.NewGuid();
//
//             // 先创建父代理和子代理
//             await _agentService.CreateAgentAsync(new CreateAgentInputDto
//             {
//                 AgentId = parentAgentId,
//                 AgentType = "TestParentAgent",
//                 Name = "ParentAgent",
//                 Properties = null
//             });
//
//             await _agentService.CreateAgentAsync(new CreateAgentInputDto
//             {
//                 AgentId = subAgentId,
//                 AgentType = "TestSubAgent",
//                 Name = "SubAgent",
//                 Properties = null
//             });
//
//             // 添加子代理
//             var addSubAgentDto = new AddSubAgentDto
//             {
//                 SubAgents = new List<Guid> { subAgentId }
//             };
//             await _agentService.AddSubAgentAsync(parentAgentId, addSubAgentDto);
//
//             // 准备删除子代理
//             var removeSubAgentDto = new RemoveSubAgentDto
//             {
//                 RemovedSubAgents = new List<Guid> { subAgentId }
//             };
//
//             // Act
//             var result = await _agentService.RemoveSubAgentAsync(parentAgentId, removeSubAgentDto);
//
//             // Assert
//             result.ShouldNotBeNull();
//             result.SubAgents.ShouldBeEmpty(); // 验证子代理被移除
//         }
//
//         [Fact]
//         public async Task DeleteAgentAsync_ShouldDeleteAgent()
//         {
//             // Arrange
//             var agentId = Guid.NewGuid();
//
//             // 创建代理
//             await _agentService.CreateAgentAsync(new CreateAgentInputDto
//             {
//                 AgentId = agentId,
//                 AgentType = "TestAgentType",
//                 Name = "TestAgent",
//                 Properties = null
//             });
//
//             // Act
//             await _agentService.DeleteAgentAsync(agentId);
//
//             // Assert
//             // 试图获取删除的代理应抛出异常
//             await Assert.ThrowsAsync<UserFriendlyException>(async () =>
//             {
//                 await _agentService.GetAgentAsync(agentId);
//             });
//         }
//
//         [Fact]
//         public async Task GetAgentAsync_ShouldReturnAgentDetails()
//         {
//             // Arrange
//             var agentId = Guid.NewGuid();
//             var agentType = "TestAgentType";
//             var name = "TestAgent";
//
//             // 创建代理
//             var createResult = await _agentService.CreateAgentAsync(new CreateAgentInputDto
//             {
//                 AgentId = agentId,
//                 AgentType = agentType,
//                 Name = name,
//                 Properties = new Dictionary<string, object>()
//             });
//
//             // Act
//             var result = await _agentService.GetAgentAsync(agentId);
//
//             // Assert
//             result.ShouldNotBeNull();
//             result.Id.ShouldBe(agentId);
//             result.AgentType.ShouldBe(agentType);
//             result.Name.ShouldBe(name);
//         }
//     }
// }