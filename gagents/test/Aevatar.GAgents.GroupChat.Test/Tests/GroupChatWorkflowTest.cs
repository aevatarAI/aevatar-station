using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Core.Dto;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Shouldly;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

public sealed class GroupChatWorkflowTest : AevatarGroupChatTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public GroupChatWorkflowTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task WorkflowTest()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });

        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom" });

        var jeni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await jeni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Jeni" });

        var carber = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await carber.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Carber" });

        var fread = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await fread.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Fread" });

        var moni = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await moni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Moni" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = jeni.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = carber.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = fread.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = moni.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        await groupAgent.AddWorkflowGroupChat(_agentFactory, workflows);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        // wait workflow run complate
        await Task.Delay(TimeSpan.FromSeconds(2));

        var jeniState = await jeni.GetStateAsync();
        jeniState.PreWorkUnits.Count.ShouldBe(2);
        jeniState.PreWorkUnits.ShouldContain("Toni");
        jeniState.PreWorkUnits.ShouldContain("Tom");

        var freadState = await fread.GetStateAsync();
        freadState.PreWorkUnits.Count.ShouldBe(2);
        freadState.PreWorkUnits.ShouldContain("Jeni");
        freadState.PreWorkUnits.ShouldContain("Carber");

        var moniState = await moni.GetStateAsync();
        moniState.AgentNames.Count.ShouldBe(1);
        moniState.AgentNames.ShouldContain("Fread");
    }

    [Fact]
    public async Task WorkflowBlockTest()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });

        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom" });

        var jeni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await jeni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Jeni" });

        var carber = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await carber.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Carber" });

        // set fread delay worker
        var fread = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await fread.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Fread" });
        await fread.SetDelayWorkAsync(10);

        var moni = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await moni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Moni" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = jeni.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = carber.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = fread.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = moni.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        await groupAgent.AddWorkflowGroupChat(_agentFactory, workflows);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        // wait workflow run complete
        await Task.Delay(TimeSpan.FromSeconds(2));

        var jeniState = await jeni.GetStateAsync();
        jeniState.PreWorkUnits.Count.ShouldBe(2);
        jeniState.PreWorkUnits.ShouldContain("Toni");
        jeniState.PreWorkUnits.ShouldContain("Tom");

        // can't get fread state, because it has been blocked
        // var freadState = await fread.GetStateAsync();
        // freadState.PreWorkUnits.Count.ShouldBe(2);
        // freadState.PreWorkUnits.ShouldContain("Jeni");
        // freadState.PreWorkUnits.ShouldContain("Carber");

        var moniState = await moni.GetStateAsync();
        moniState.AgentNames.Count.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateWorkflowTest()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });

        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom" });

        var jeni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await jeni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Jeni" });

        var carber = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await carber.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Carber" });

        var fread = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await fread.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Fread" });

        var moni = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await moni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Moni" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflowsFirst = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = jeni.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = carber.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = fread.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = moni.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };
        var workflowsSecond = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = jeni.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = fread.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = moni.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        var workflowCoordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto()
        {
            WorkflowUnitList = workflowsFirst
        });

        await groupAgent.RegisterAsync(workflowCoordinator);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        await Task.Delay(TimeSpan.FromSeconds(1));
        // update workflow
        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto()
        {
            WorkflowUnitList = workflowsSecond
        });

        // wait workflow run complate
        await Task.Delay(TimeSpan.FromSeconds(2));

        var jeniState = await jeni.GetStateAsync();
        jeniState.PreWorkUnits.Count.ShouldBe(2);
        jeniState.PreWorkUnits.ShouldContain("Toni");
        jeniState.PreWorkUnits.ShouldContain("Tom");

        var freadState = await fread.GetStateAsync();
        freadState.PreWorkUnits.Count.ShouldBe(2);
        freadState.PreWorkUnits.ShouldContain("Jeni");
        freadState.PreWorkUnits.ShouldContain("Carber");

        var moniState = await moni.GetStateAsync();
        moniState.AgentNames.Count.ShouldBe(1);

        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { InitContent = "this is the content" });

        // wait workflow run complate
        await Task.Delay(TimeSpan.FromSeconds(2));

        freadState = await fread.GetStateAsync();
        freadState.PreWorkUnits.Count.ShouldBe(3);

        var workflowCoordinatorGraindId = workflowCoordinator.GetGrainId();
        (await toni.GetParentAsync()).ShouldBe(workflowCoordinatorGraindId);
        (await tom.GetParentAsync()).ShouldBe(workflowCoordinatorGraindId);
        (await jeni.GetParentAsync()).ShouldBe(workflowCoordinatorGraindId);
        (await carber.GetParentAsync()).IsDefault.ShouldBeTrue();
        (await fread.GetParentAsync()).ShouldBe(workflowCoordinatorGraindId);
        (await moni.GetParentAsync()).ShouldBe(workflowCoordinatorGraindId);
    }

    [Fact]
    public async Task MulitSubWorkflowTest()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });

        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom" });

        var jeni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await jeni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Jeni" });

        var carber = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await carber.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Carber" });

        var fread = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await fread.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Fread" });

        var moni = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await moni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Moni" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = carber.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = jeni.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = fread.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = moni.GetGrainId().ToString(),
                NextGrainId = "",
            },
        };

        await groupAgent.AddWorkflowGroupChat(_agentFactory, workflows);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        // wait workflow run complate
        await Task.Delay(TimeSpan.FromSeconds(2));

        var jeniState = await jeni.GetStateAsync();
        jeniState.PreWorkUnits.Count.ShouldBe(2);
        jeniState.PreWorkUnits.ShouldContain("Toni");
        jeniState.PreWorkUnits.ShouldContain("Tom");

        var freadState = await fread.GetStateAsync();
        freadState.PreWorkUnits.Count.ShouldBe(3);
        freadState.PreWorkUnits.ShouldContain("Toni");
        freadState.PreWorkUnits.ShouldContain("Tom");
        freadState.PreWorkUnits.ShouldContain("Carber");

        var moniState = await moni.GetStateAsync();
        moniState.AgentNames.Count.ShouldBe(2);
    }

    [Fact]
    public async Task WrongWorkflowUnitTest()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });

        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom" });

        var jeni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await jeni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Jeni" });

        var carber = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await carber.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Carber" });

        var fread = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await fread.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Fread" });

        var moni = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await moni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Moni" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = jeni.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = carber.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = fread.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = moni.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            }
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await groupAgent.AddWorkflowGroupChat(_agentFactory, workflows));
        exception.Message.ShouldBe("The workflow has a loop and cannot end normally.");

        await tom.RegisterAsync(toni);
        workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await groupAgent.AddWorkflowGroupChat(_agentFactory, workflows));
        exception.Message.ShouldBe($"GAgent {toni.GetGrainId().ToString()} already has a parent GAgent.");
    }

    [Fact]
    public async Task WorkflowInitTest()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });

        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = tom.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        var workflowCoordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(workflowCoordinator);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        await Task.Delay(TimeSpan.FromSeconds(2));

        var tomState = await tom.GetStateAsync();
        tomState.PreWorkUnits.Count.ShouldBe(0);

        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto()
        {
            WorkflowUnitList = new List<WorkflowUnitDto>()
        });

        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        await Task.Delay(TimeSpan.FromSeconds(2));

        tomState = await tom.GetStateAsync();
        tomState.PreWorkUnits.Count.ShouldBe(0);

        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto()
        {
            WorkflowUnitList = workflows,
            InitContent = "Init"
        });

        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        await Task.Delay(TimeSpan.FromSeconds(2));

        tomState = await tom.GetStateAsync();
        tomState.PreWorkUnits.Count.ShouldBe(1);

        var workflowState = await workflowCoordinator.GetStateAsync();
        workflowState.CurrentWorkUnitInfos.Count.ShouldBe(2);
        workflowState.Content.ShouldBe("Init");
    }

    [Fact]
    public async Task Workflow_StartFailed_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflowCoordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(workflowCoordinator);

        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });
        await Task.Delay(TimeSpan.FromSeconds(1));

        var workflowState = await workflowCoordinator.GetStateAsync();
        workflowState.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Failed);

        await groupAgent.PublishEventAsync(new ResetWorkflowEvent() { });
        await Task.Delay(TimeSpan.FromSeconds(1));

        workflowState = await workflowCoordinator.GetStateAsync();
        workflowState.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Pending);

        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });
        await Task.Delay(TimeSpan.FromSeconds(1));

        workflowState = await workflowCoordinator.GetStateAsync();
        workflowState.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Failed);

        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });

        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom" });

        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = tom.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto()
        {
            WorkflowUnitList = workflows
        });
        workflowState = await workflowCoordinator.GetStateAsync();
        workflowState.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Pending);
    }

    [Fact]
    public async Task Workflow_WithExecutionRecord_Succeeds_ShouldCreateAndCompleteRecord()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });
        var leader = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await leader.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Leader" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto() { GrainId = toni.GetGrainId().ToString(), NextGrainId = leader.GetGrainId().ToString() },
            new WorkflowUnitDto() { GrainId = leader.GetGrainId().ToString(), NextGrainId = "" }
        };

        var coordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await coordinator.ConfigAsync(new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = workflows,
            InitContent = "init",
            EnableExecutionRecord = true
        });

        await groupAgent.RegisterAsync(coordinator);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent());

        // Wait until execution record id is assigned
        Guid recordId = Guid.Empty;
        for (int i = 0; i < 20; i++)
        {
            var s = await coordinator.GetStateAsync();
            recordId = s.CurrentExecutionRecordId;
            if (recordId != Guid.Empty) break;
            await Task.Delay(100);
        }
        recordId.ShouldNotBe(Guid.Empty);

        // Wait for workflow to finish and record to be unregistered
        await Task.Delay(TimeSpan.FromSeconds(2));
        var state = await coordinator.GetStateAsync();
        state.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Pending);
        state.CurrentExecutionRecordId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task Workflow_WithExecutionRecord_Failure_ShouldMarkFailed()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni" });
        await toni.SetFailureSummary("Exception");
        var leader = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await leader.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Leader" });

        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto() { GrainId = toni.GetGrainId().ToString(), NextGrainId = leader.GetGrainId().ToString() },
            new WorkflowUnitDto() { GrainId = leader.GetGrainId().ToString(), NextGrainId = "" }
        };

        var coordinator = await _agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await coordinator.ConfigAsync(new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = workflows,
            InitContent = "init",
            EnableExecutionRecord = true
        });

        await groupAgent.RegisterAsync(coordinator);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent());

        // Wait a bit for first unit to be activated and term to be set
        await Task.Delay(500);
        // var cstateBefore = await coordinator.GetStateAsync();
        // var currentTerm = cstateBefore.Term;
        //
        // await groupAgent.PublishEventAsync(new ChatResponseEvent
        // {
        //     BlackboardId = cstateBefore.BlackboardId,
        //     MemberId = toni.GetPrimaryKey(),
        //     MemberName = "Toni",
        //     FailureSummary = "boom",
        //     Term = currentTerm
        // });
        await Task.Delay(1000);

        var cstate = await coordinator.GetStateAsync();
        cstate.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Failed);
    }
}