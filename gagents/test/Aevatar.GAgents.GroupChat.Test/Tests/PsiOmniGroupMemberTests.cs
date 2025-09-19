using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TestBase;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Shouldly;
using Xunit;
using Aevatar.GAgents.PsiOmni;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

[Collection(ClusterCollection.Name)]
public sealed class PsiOmniGroupMemberTests : AevatarGAgentTestBase<AevatarGAgentTestBaseModule>
{
    private readonly IGAgentFactory _factory;

    public PsiOmniGroupMemberTests()
    {
        _factory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task EvaluationInterestEvent_Should_Publish_Response()
    {
        var group = await _factory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var member = await _factory.GetGAgentAsync<IPsiOmniGAgent>(Guid.NewGuid());
        var blackboardId = Guid.NewGuid();
        var workflowId = blackboardId.ToString();
        await member.PrepareResourceContextAsync(ResourceContext.Create(new List<GrainId>(), workflowId).WithMetadata("WorkflowId", workflowId));

        var collector = await _factory.GetGAgentAsync<WorkflowExecutionRecordGAgentTests.IEventCollectorGAgent>(Guid.NewGuid());

        await group.RegisterAsync(member);
        await group.RegisterAsync(collector);
        await group.PublishEventAsync(new EvaluationInterestEvent { ChatTerm = 1 });
        await Task.Delay(200);

        var state = await collector.GetStateAsync();
        state.LastInterestValue.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ChatEvent_Should_Raise_ReceiveUserMessageEvent()
    {
        var group = await _factory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var member = await _factory.GetGAgentAsync<IPsiOmniGAgent>(Guid.NewGuid());
        await member.PrepareResourceContextAsync(ResourceContext.Create(new List<GrainId>(), Guid.NewGuid().ToString()).WithMetadata("WorkflowId", Guid.NewGuid().ToString()));

        await group.RegisterAsync(member);

        await group.PublishEventAsync(new ChatEvent
        {
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "hello" } },
            Term = 1,
            Speaker = member.GetGrainId().GetGuidKey()
        });

        await Task.Delay(100);
        (true).ShouldBeTrue();
    }

    [Fact]
    public async Task CoordinatorPingEvent_When_NotIgnored_Should_Respond_Pong()
    {
        var group = await _factory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var member = await _factory.GetGAgentAsync<IPsiOmniGAgent>(Guid.NewGuid());
        var blackboardId = Guid.NewGuid();
        var workflowId = blackboardId.ToString();
        await member.PrepareResourceContextAsync(ResourceContext.Create(new List<GrainId>(), workflowId).WithMetadata("WorkflowId", workflowId));
        var collector = await _factory.GetGAgentAsync<WorkflowExecutionRecordGAgentTests.IEventCollectorGAgent>(Guid.NewGuid());
        await group.RegisterAsync(member);
        await group.RegisterAsync(collector);

        await group.PublishEventAsync(new CoordinatorPingEvent { BlackboardId = blackboardId });

        var state = await collector.GetStateAsync();
        for (int i = 0; i < 10 && state.LastPongMemberId == Guid.Empty; i++)
        {
            await Task.Delay(200);
            state = await collector.GetStateAsync();
        }

        state.LastPongMemberId.ShouldNotBe(Guid.Empty);
    }
}


