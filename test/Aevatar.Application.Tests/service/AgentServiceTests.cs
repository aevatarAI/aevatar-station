// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Aevatar.Agent;
// using Aevatar.Service;
// using Shouldly;
// using Volo.Abp;
// using Xunit;
//
// namespace Aevatar.Service
// {
//     public sealed class AgentServiceTests : AevatarApplicationTestBase
//     {
//         private readonly IAgentService _agentService;
//
//         public AgentServiceTests()
//         {
//             _agentService = GetRequiredService<IAgentService>();
//         }
//
//         [Fact]
//         public async Task GetAgentEventLogsAsync_ShouldReturnLogs()
//         {
//             var agentId = Guid.NewGuid().ToString();
//             var pageNumber = 1;
//             var pageSize = 10;
//
//             var result = await _agentService.GetAgentEventLogsAsync(agentId, pageNumber, pageSize);
//
//             result.ShouldNotBeNull();
//             result.Item1.ShouldBe(0);
//             result.Item2.ShouldBeEmpty();
//         }
//
//         [Fact]
//         public async Task CreateAgentAsync_ShouldCreateAgentSuccessfully()
//         {
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
//             var result = await _agentService.CreateAgentAsync(dto);
//
//             result.ShouldNotBeNull();
//             result.Name.ShouldBe(dto.Name);
//             result.AgentType.ShouldBe(agentType);
//             result.Properties.ShouldContainKey("Key");
//         }
//
//         [Fact]
//         public async Task AddSubAgentAsync_ShouldAddSubAgent()
//         {
//             var parentAgentId = Guid.NewGuid();
//             var subAgentId = Guid.NewGuid();
//
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
//             var addSubAgentDto = new AddSubAgentDto
//             {
//                 SubAgents = new List<Guid> { subAgentId }
//             };
//
//             var result = await _agentService.AddSubAgentAsync(parentAgentId, addSubAgentDto);
//
//             result.ShouldNotBeNull();
//             result.SubAgents.ShouldContain(subAgentId);
//         }
//
//         [Fact]
//         public async Task RemoveSubAgentAsync_ShouldRemoveSubAgent()
//         {
//             var parentAgentId = Guid.NewGuid();
//             var subAgentId = Guid.NewGuid();
//
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
//             var addSubAgentDto = new AddSubAgentDto
//             {
//                 SubAgents = new List<Guid> { subAgentId }
//             };
//             await _agentService.AddSubAgentAsync(parentAgentId, addSubAgentDto);
//
//             var removeSubAgentDto = new RemoveSubAgentDto
//             {
//                 RemovedSubAgents = new List<Guid> { subAgentId }
//             };
//
//             var result = await _agentService.RemoveSubAgentAsync(parentAgentId, removeSubAgentDto);
//
//             result.ShouldNotBeNull();
//             result.SubAgents.ShouldBeEmpty();
//         }
//
//         [Fact]
//         public async Task DeleteAgentAsync_ShouldDeleteAgent()
//         {
//             var agentId = Guid.NewGuid();
//
//             await _agentService.CreateAgentAsync(new CreateAgentInputDto
//             {
//                 AgentId = agentId,
//                 AgentType = "TestAgentType",
//                 Name = "TestAgent",
//                 Properties = null
//             });
//
//             await _agentService.DeleteAgentAsync(agentId);
//
//             await Assert.ThrowsAsync<UserFriendlyException>(async () =>
//             {
//                 await _agentService.GetAgentAsync(agentId);
//             });
//         }
//
//         [Fact]
//         public async Task GetAgentAsync_ShouldReturnAgentDetails()
//         {
//             var agentId = Guid.NewGuid();
//             var agentType = "TestAgentType";
//             var name = "TestAgent";
//
//             var createResult = await _agentService.CreateAgentAsync(new CreateAgentInputDto
//             {
//                 AgentId = agentId,
//                 AgentType = agentType,
//                 Name = name,
//                 Properties = new Dictionary<string, object>()
//             });
//
//             var result = await _agentService.GetAgentAsync(agentId);
//
//             result.ShouldNotBeNull();
//             result.Id.ShouldBe(agentId);
//             result.AgentType.ShouldBe(agentType);
//             result.Name.ShouldBe(name);
//         }
//     }
// }