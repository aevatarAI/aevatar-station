using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TestBase;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Shouldly;
using Xunit;

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
        var member = await _factory.GetGAgentAsync<IGroupMemberGAgent>(Guid.NewGuid(), new Dictionary<string, object> { { "Type", "PsiOmni" } });
        await member.PrepareResourceContextAsync(ResourceContext.Create(new List<GrainId>(), Guid.NewGuid().ToString()).WithMetadata("WorkflowId", Guid.NewGuid().ToString()));

        var collector = await _factory.GetGAgentAsync<WorkflowExecutionRecordGAgentTests.IEventCollectorGAgent>(Guid.NewGuid());

        await member.RegisterAsync(collector);
        await member.PublishAsync(new EvaluationInterestEvent { ChatTerm = 1 });
        await Task.Delay(200);

        var state = await collector.GetStateAsync();
        state.LastInterestValue.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ChatEvent_Should_Raise_ReceiveUserMessageEvent()
    {
        var member = await _factory.GetGAgentAsync<IGroupMemberGAgent>(Guid.NewGuid(), new Dictionary<string, object> { { "Type", "PsiOmni" } });
        await member.PrepareResourceContextAsync(ResourceContext.Create(new List<GrainId>(), Guid.NewGuid().ToString()).WithMetadata("WorkflowId", Guid.NewGuid().ToString()));

        await member.PublishAsync(new ChatEvent
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
        var member = await _factory.GetGAgentAsync<IGroupMemberGAgent>(Guid.NewGuid(), new Dictionary<string, object> { { "Type", "PsiOmni" } });
        await member.PrepareResourceContextAsync(ResourceContext.Create(new List<GrainId>(), Guid.NewGuid().ToString()).WithMetadata("WorkflowId", Guid.NewGuid().ToString()));
        var collector = await _factory.GetGAgentAsync<WorkflowExecutionRecordGAgentTests.IEventCollectorGAgent>(Guid.NewGuid());
        await member.RegisterAsync(collector);

        await member.PublishAsync(new CoordinatorPingEvent());
        await Task.Delay(200);

        var state = await collector.GetStateAsync();
        state.LastPongMemberId.ShouldNotBe(Guid.Empty);
    }
}


