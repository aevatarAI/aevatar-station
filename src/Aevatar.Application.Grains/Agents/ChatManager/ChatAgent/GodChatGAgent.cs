using System.Diagnostics;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Json.Schema.Generation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

[Description("god chat agent")]
public class GodChatGAgent : ChatGAgentBase<GodChatState, GodChatEventLog, EventBase, ChatConfigDto>, IGodChat
{
    
    [EventHandler]
    public async Task HandleEventAsync(RequestStreamGodChatEvent @event)
    {
        string chatId = Guid.NewGuid().ToString();
        Logger.LogDebug($"[GodChatGAgent][RequestStreamGodChatEvent] start:{JsonConvert.SerializeObject(@event)} chatID:{chatId}");
        var title = "";
        var content = "";
        var isLastChunk = false;

        try
        {
            if (State.StreamingModeEnabled)
            {
                Logger.LogDebug("State.StreamingModeEnabled is on");
                await StreamChatWithSessionAsync(@event.SessionId, @event.SystemLLM, @event.Content,chatId);
            }
            else
            {
                var response = await ChatWithSessionAsync(@event.SessionId, @event.SystemLLM, @event.Content);
                content = response.Item1;
                title = response.Item2;
                isLastChunk = true;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"[GodChatGAgent][RequestStreamGodChatEvent] handle error:{e.ToString()}");
        }

        await PublishAsync(new ResponseStreamGodChat()
        {
            ChatId =chatId,
            Response = content,
            NewTitle = title,
            IsLastChunk = isLastChunk,
            SerialNumber = -1,
            SessionId = @event.SessionId
            
        });

        Logger.LogDebug($"[GodChatGAgent][RequestStreamGodChatEvent] end:{JsonConvert.SerializeObject(@event)}");
    }
    
    private async Task StreamChatWithSessionAsync(Guid sessionId,string sysmLLM, string content,string chatId,
        ExecutionPromptSettings promptSettings = null)
    {
        Stopwatch sw = new Stopwatch();
        Logger.LogDebug($"StreamChatWithSessionAsync - step1,time use:{sw.ElapsedMilliseconds}");

        var title = "";
        
        if (State.Title.IsNullOrEmpty())
        {
            sw.Reset();
            sw.Start();
            var titleList = await ChatWithHistory(content);
            title = titleList is { Count: > 0 }
                ? titleList[0].Content!
                : string.Join(" ", content.Split(" ").Take(4));
        
            RaiseEvent(new RenameChatTitleEventLog()
            {
                Title = title
            });
        
            await ConfirmEvents();
            sw.Stop();
            await PublishAsync(new RenameChatTitleEvent()
            {
                SessionId = sessionId,
                Title = title
            });
            Logger.LogDebug($"StreamChatWithSessionAsync - step3,time use:{sw.ElapsedMilliseconds}");
        }

        sw.Reset();
        sw.Start();
        var configuration = GetConfiguration();
        GodStreamChatAsync(sessionId,await configuration.GetSystemLLM(), await configuration.GetStreamingModeEnabled(),content, chatId,promptSettings);
        sw.Stop();
        Logger.LogDebug($"StreamChatWithSessionAsync - step4,time use:{sw.ElapsedMilliseconds}");
    }
    
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
            case RenameChatTitleEventLog renameChatTitleEventLog:
                State.Title = renameChatTitleEventLog.Title;
                break;
        }
    }
    
    private IConfigurationGAgent GetConfiguration()
    {
        return GrainFactory.GetGrain<IConfigurationGAgent>(CommonHelper.GetSessionManagerConfigurationId());
    }
    
    public async Task<Tuple<string, string>> ChatWithSessionAsync(Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null)
    {
        

        var title = "";
        if (State.Title.IsNullOrEmpty())
        {
            var titleList = await ChatWithHistory(content);
            title = titleList is { Count: > 0 }
                ? titleList[0].Content!
                : string.Join(" ", content.Split(" ").Take(4));

            RaiseEvent(new RenameChatTitleEventLog()
            {
                Title = title
            });

            await ConfirmEvents();
            
            await PublishAsync(new RenameChatTitleEvent()
            {
                SessionId = sessionId,
                Title = title
            });
        }

        var configuration = GetConfiguration();
        var response = await GodChatAsync(await configuration.GetSystemLLM(), content, promptSettings);
        return new Tuple<string, string>(response, title);
    }
}