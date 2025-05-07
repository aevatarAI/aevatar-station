using Microsoft.Extensions.Logging;
using Orleans.Core;

namespace Aevatar.Application.Grains.Agents.ChatManager.Share;

public interface IShareLinkGrain : IGrainWithGuidKey
{
    Task SaveShareContentAsync(ShareLinkDto shareLinkDto);
    Task<ShareLinkDto> GetShareContentAsync();
}

public class ShareLinkGrain : Grain<ShareState>, IShareLinkGrain
{
    private readonly ILogger<ShareLinkGrain> _logger;
    
    public ShareLinkGrain(ILogger<ShareLinkGrain> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
    
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task SaveShareContentAsync(ShareLinkDto shareLinkDto)
    {
        State.UserId = shareLinkDto.UserId;
        State.SessionId = shareLinkDto.SessionId;
        State.Messages = shareLinkDto.Messages;
        State.CreateTime = DateTime.UtcNow;
        await WriteStateAsync();
    }

    public async Task<ShareLinkDto> GetShareContentAsync()
    {
        return new ShareLinkDto
        {
            UserId = State.UserId,
            SessionId = State.SessionId,
            Messages = State.Messages,
            CreateTime = State.CreateTime
        };
    }
} 