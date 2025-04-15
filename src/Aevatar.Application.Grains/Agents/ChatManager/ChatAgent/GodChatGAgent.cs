using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Json.Schema.Generation;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

[Description("god chat agent")]
public class GodChatGAgent : ChatGAgentBase<GodChatState, GodChatEventLog, EventBase, ChatConfigDto>, IGodChat
{
    public async Task<string> GodChatAsync(string llm, string message,
        ExecutionPromptSettings? promptSettings = null)
    {
        if (State.SystemLLM != llm)
        {
            await InitializeAsync(new InitializeDto()
                { Instructions = State.PromptTemplate, LLMConfig = new LLMConfigDto() { SystemLLM = llm } });
        }

        var response = await ChatAsync(message, promptSettings);
        if (response is { Count: > 0 })
        {
            return response[0].Content!;
        }

        return string.Empty;
    }

    public async Task<string> GodStreamChatAsync(Guid sessionId,string llm, bool streamingModeEnabled,string message, String chatId,
        ExecutionPromptSettings? promptSettings = null)
    {
        if (State.SystemLLM != llm || State.StreamingModeEnabled != streamingModeEnabled)
        {
            await InitializeAsync(new InitializeDto()
            {
                Instructions = State.PromptTemplate, LLMConfig = new LLMConfigDto() { SystemLLM = llm },
                StreamingModeEnabled = true, StreamingConfig = new StreamingConfig()
                {
                    BufferingSize = 32
                }
            });
        }

        AIChatContextDto aiChatContextDto = new AIChatContextDto()
        {
            ChatId = chatId,
            RequestId = sessionId
            
        };
        await ChatAsync(message, promptSettings,aiChatContextDto);

        return string.Empty;
    }

    public async Task SetUserProfileAsync(UserProfileDto? userProfileDto)
    {
        if (userProfileDto == null)
        {
            return;
        }

        RaiseEvent(new UpdateUserProfileGodChatEventLog
        {
            Gender = userProfileDto.Gender,
            BirthDate = userProfileDto.BirthDate,
            BirthPlace = userProfileDto.BirthPlace,
            FullName = userProfileDto.FullName
        });

        await ConfirmEvents();
    }

    public async Task<UserProfileDto?> GetUserProfileAsync()
    {
        if (State.UserProfile == null)
        {
            return null;
        }

        return new UserProfileDto
        {
            Gender = State.UserProfile.Gender,
            BirthDate = State.UserProfile.BirthDate,
            BirthPlace = State.UserProfile.BirthPlace,
            FullName = State.UserProfile.FullName
        };
    }

    public Task<List<ChatMessage>> GetChatMessageAsync()
    {
        return Task.FromResult(State.ChatHistory);
    }

    protected sealed override void AIGAgentTransitionState(GodChatState state,
        StateLogEventBase<GodChatEventLog> @event)
    {
        base.AIGAgentTransitionState(state, @event);
        
        switch (@event)
        {
            case UpdateUserProfileGodChatEventLog updateUserProfileGodChatEventLog:
                if (State.UserProfile == null)
                {
                    State.UserProfile = new UserProfile();
                }
                State.UserProfile.Gender = updateUserProfileGodChatEventLog.Gender;
                State.UserProfile.BirthDate = updateUserProfileGodChatEventLog.BirthDate;
                State.UserProfile.BirthPlace = updateUserProfileGodChatEventLog.BirthPlace;
                State.UserProfile.FullName = updateUserProfileGodChatEventLog.FullName;
                break;
        }
    }
}