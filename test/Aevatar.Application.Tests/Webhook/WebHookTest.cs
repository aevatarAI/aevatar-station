using System;
using System.Threading.Tasks;
using Aevatar.Service;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;

namespace Aevatar.Webhook;

public abstract class WebHookTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IWebhookService _webhookService;
    public WebHookTests()
    {
        _webhookService = GetRequiredService<IWebhookService>();
    }
    
    [Fact]
    public async Task CreateWebhookTestAsync()
    {
        string webhookId = "telegram";
        string version = "1";
        byte[] codeBytes = "21323".GetBytes();
        await _webhookService.CreateWebhookAsync(webhookId,version,codeBytes);
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task DestroyWebhookTestAsync()
    {
        string webhookId = "telegram";
        string version = "1";
        await CreateWebhookTestAsync();
        await _webhookService.DestroyWebhookAsync(webhookId, version);
    }
}