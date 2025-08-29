using Aevatar.Core;
using GroupChat.GAgent.Feature.Coordinator.LogEvent;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Coordinator.GEvent;

namespace GroupChat.GAgent.Feature.Coordinator;

public abstract class CoordinatorGAgentBase<TState, TStateLogEvent> :
    GAgentBase<TState, TStateLogEvent>,
    ICoordinatorGAgent where TState : CoordinatorStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>, new()
{
    private IDisposable? _timer;
    private List<InterestInfo> _interestInfoList = new List<InterestInfo>();
    private List<GroupMember> _groupMembers = new List<GroupMember>();
    private DateTime _latestSendInterestTime = DateTime.Now;

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "CoordinatorGAgentBase - Base class for coordination agents that manage group chat sessions. " +
            "Handles member interest evaluation, speaker selection, turn management, and session lifecycle. " +
            "Monitors member participation and coordinates the flow of conversation between multiple agents."
        );
    }

    public async Task StartAsync(Guid blackboardId)
    {
        _interestInfoList = new List<InterestInfo>();
        _groupMembers = new List<GroupMember>();
        _latestSendInterestTime = DateTime.Now;

        RaiseEvent(new SetBlackboardLogEvent { BlackboardId = blackboardId });
        await ConfirmEvents();

        TryStartTimer();
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        if (@event.BlackboardId != State.BlackboardId || @event.Term != State.ChatTerm &&
            @event.MemberId != State.CoordinatorSpeaker)
        {
            return;
        }

        await PublishAsync(new CoordinatorConfirmChatResponse
        {
            BlackboardId = @event.BlackboardId, MemberId = @event.MemberId, MemberName = @event.MemberName,
            ChatResponse = @event.ChatResponse
        });

        RaiseEvent(new AddChatTermLogEvent { IfComplete = @event.ChatResponse.Continue });
        await ConfirmEvents();

        _interestInfoList.Clear();

        // group chat finished
        if (@event.ChatResponse.Continue == false)
        {
            await PublishAsync(new GroupChatFinishEvent { BlackboardId = State.BlackboardId });
            _timer?.Dispose();

            return;
        }

        // next round
        if (await NeedCheckMemberInterestValue(_groupMembers, State.BlackboardId))
        {
            await PublishAsync(new EvaluationInterestEvent
            {
                BlackboardId = State.BlackboardId,
                ChatTerm = State.ChatTerm
            });
            _latestSendInterestTime = DateTime.Now;
        }
        else
        {
            await Coordinator();
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestResponseEvent @event)
    {
        if (@event.BlackboardId != State.BlackboardId || @event.ChatTerm != State.ChatTerm)
        {
            return;
        }

        var member = _interestInfoList.Find(f => f.MemberId == @event.MemberId);
        if (member == null)
        {
            _interestInfoList.Add(new InterestInfo
            {
                MemberId = @event.MemberId,
                InterestValue = @event.InterestValue
            });
        }
        else
        {
            member.InterestValue = @event.InterestValue;
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(CoordinatorPongEvent @event)
    {
        if (@event.BlackboardId != State.BlackboardId)
        {
            return;
        }

        var member = _groupMembers.Find(f => f.Id == @event.MemberId);
        if (member == null)
        {
            member = new GroupMember
            {
                Id = @event.MemberId,
                Name = @event.MemberName
            };
            _groupMembers.Add(member);
        }

        member.UpdateTime = DateTime.Now;
    }

    protected async virtual Task<Guid> CoordinatorToSpeak(List<InterestInfo> interestInfos, List<GroupMember> members)
    {
        var randList = new List<Guid>();
        if (interestInfos.Count > 0)
        {
            var interestInfo = interestInfos.OrderByDescending(o => o.InterestValue).ToList();
            if (interestInfo[0].InterestValue == 100)
            {
                return interestInfo[0].MemberId;
            }

            randList = interestInfos.Take(5).Select(s => s.MemberId).ToList();
        }

        if (randList.Count == 0)
        {
            randList = members.Select(s => s.Id).ToList();
        }

        if (members.Count == 0)
        {
            return Guid.Empty;
        }

        var random = new Random();
        var randomNum = random.Next(0, randList.Count);

        return randList[randomNum];
    }

    protected virtual Task<bool> NeedCheckMemberInterestValue(List<GroupMember> members, Guid blackboardId)
    {
        return Task.FromResult(false);
    }

    protected virtual Task<bool> CheckSendCoordinatorPingEventAsync(DateTime dateTime)
    {
        return Task.FromResult(dateTime.Second % 10 == 0);
    }

    protected virtual Task<bool> CheckMemberOutOfGroupAsync(GroupMember groupMember)
    {
        return Task.FromResult((DateTime.Now - groupMember.UpdateTime).Seconds < 20);
    }

    protected virtual Task<bool> NeedSelectSpeakerAsync(List<InterestInfo> interestInfos, List<GroupMember> members)
    {
        return Task.FromResult(interestInfos.Count >= (members.Count * 2) / 3);
    }

    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        TryStartTimer();
        return Task.CompletedTask;
    }

    private void TryStartTimer()
    {
        _timer ??= this.RegisterGrainTimer(BackgroundWorkAsync, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private async Task BackgroundWorkAsync(CancellationToken token)
    {
        await TryDriveProgressAsync();
        await TryPingMember();
    }

    private async Task TryDriveProgressAsync()
    {
        if (await NeedCheckMemberInterestValue(_groupMembers, State.BlackboardId) == false)
        {
            return;
        }

        if (State.IfTriggerCoordinate == false)
        {
            var ifCoordinator = false;
            if (_groupMembers.Count > 0)
            {
                var ifSelectSpeaker = await NeedSelectSpeakerAsync(_interestInfoList, _groupMembers);
                if (ifSelectSpeaker)
                {
                    ifCoordinator = await Coordinator();
                }
            }

            if (ifCoordinator == false &&
                (DateTime.Now - _latestSendInterestTime).Seconds > 5 &&
                _interestInfoList.Count <= (_groupMembers.Count * 2) / 3)
            {
                await PublishAsync(new EvaluationInterestEvent
                {
                    BlackboardId = State.BlackboardId,
                    ChatTerm = State.ChatTerm
                });
                _latestSendInterestTime = DateTime.Now;
            }

            return;
        }

        // member not send message, should reschedule
        if ((DateTime.Now - State.CoordinatorTime).TotalSeconds > 10)
        {
            await Coordinator();
        }
    }

    private async Task TryPingMember()
    {
        var ifSendPingMsg = await CheckSendCoordinatorPingEventAsync(DateTime.Now);
        if (ifSendPingMsg)
        {
            await PublishAsync(new CoordinatorPingEvent() { BlackboardId = State.BlackboardId });
            var leaveMember = new List<Guid>();
            foreach (var member in _groupMembers)
            {
                var ifInGroup = await CheckMemberOutOfGroupAsync(member);
                if (ifInGroup == false)
                {
                    leaveMember.Add(member.Id);
                }
            }

            if (leaveMember.Count > 0)
            {
                _groupMembers.RemoveAll(f => leaveMember.Contains(f.Id));
            }
        }
    }

    private async Task<bool> Coordinator()
    {
        var speaker = await CoordinatorToSpeak(_interestInfoList, _groupMembers);
        if (speaker == Guid.Empty)
        {
            return false;
        }

        await PublishAsync(new ChatEvent()
            { BlackboardId = State.BlackboardId, Speaker = speaker, Term = State.ChatTerm });

        RaiseEvent(new TriggerCoordinator() { MemberId = speaker, CreateTime = DateTime.Now });
        await ConfirmEvents();

        return true;
    }

    #region Log Event Define

    public class SetBlackboardLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Guid BlackboardId { get; set; }
    }

    [GenerateSerializer]
    public class AddChatTermLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public bool IfComplete { get; set; }
    }

    [GenerateSerializer]
    public class TriggerCoordinator : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Guid MemberId { get; set; }
        [Id(1)] public DateTime CreateTime { get; set; }
    }



    #endregion

    #region Log Event Handler

    protected sealed override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> eventObj)
    {
        switch (eventObj)
        {
            case AddChatTermLogEvent @event:
                state.ChatTerm += 1;
                state.IfTriggerCoordinate = false;
                state.IfComplete = @event.IfComplete;
                break;
            case TriggerCoordinator @event:
                state.IfTriggerCoordinate = true;
                state.CoordinatorSpeaker = @event.MemberId;
                state.CoordinatorTime = @event.CreateTime;
                break;
            case SetBlackboardLogEvent @event:
                state.BlackboardId = @event.BlackboardId;
                state.IfTriggerCoordinate = false;
                state.IfComplete = false;
                state.CoordinatorSpeaker = Guid.Empty;
                state.ChatTerm = 0;
                break;
        }

        CoordinatorTransitionState(state, eventObj);
    }

    protected virtual void CoordinatorTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        // Derived classes can override this method.
    }

    #endregion
}