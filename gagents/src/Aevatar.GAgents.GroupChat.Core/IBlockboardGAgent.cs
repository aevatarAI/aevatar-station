using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;

public interface IBlackboardGAgent : IGAgent
{
    public Task<bool> SetTopic(string topic);
    public Task<List<ChatMessage>> GetContent();

    public Task<List<ChatMessage>> GetLastChatMessageAsync(List<Guid> talkerList);

    public Task SetMessageAsync(CoordinatorConfirmChatResponse confirmChatResponse);

    public Task ResetAsync();
}