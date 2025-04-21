using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Service;
using Shouldly;
using Xunit;
using Volo.Abp;

namespace Aevatar.Webhook;

public class WebHookTests : AevatarApplicationTestBase
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
        var codeFiles = new Dictionary<string, byte[]>
        {
            ["test.dll"] = "21323".GetBytes()
        };
        await _webhookService.CreateWebhookAsync(webhookId, version, codeFiles);
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CreateWebhook_WithMultipleFiles_ShouldSucceed()
    {
        string webhookId = "multi-file";
        string version = "1";
        var codeFiles = new Dictionary<string, byte[]>
        {
            ["main.dll"] = "main_code".GetBytes(),
            ["lib.dll"] = "library_code".GetBytes(),
            ["config.json"] = "{}".GetBytes()
        };
        await _webhookService.CreateWebhookAsync(webhookId, version, codeFiles);
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(3);
    }


    [Fact]
    public async Task GetWebhookCode_ForNonExistentWebhook_ShouldReturnEmpty()
    {
        string webhookId = "non-existent";
        string version = "1";
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DestroyWebhookTestAsync()
    {
        string webhookId = "telegram";
        string version = "1";
        await CreateWebhookTestAsync();
        await _webhookService.DestroyWebhookAsync(webhookId, version);

        // Verify webhook is destroyed
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DestroyWebhook_ForNonExistentWebhook_ShouldSucceed()
    {
        string webhookId = "non-existent";
        string version = "1";
        await _webhookService.DestroyWebhookAsync(webhookId, version);
        // Should not throw exception
    }

    [Fact]
    public async Task UpdateCode_ForExistingWebhook_ShouldSucceed()
    {
        string webhookId = "update-test";
        string version = "1";
        var initialFiles = new Dictionary<string, byte[]>
        {
            ["initial.dll"] = "initial_code".GetBytes()
        };
        await _webhookService.CreateWebhookAsync(webhookId, version, initialFiles);

        var updatedFiles = new Dictionary<string, byte[]>
        {
            ["updated.dll"] = "updated_code".GetBytes()
        };
        await _webhookService.UpdateCodeAsync(webhookId, version, updatedFiles);

        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(1);
        code.ShouldContainKey("updated.dll");
    }

    [Fact]
    public async Task UpdateCode_WithMultipleFiles_ShouldSucceed()
    {
        string webhookId = "update-multi";
        string version = "1";
        await _webhookService.CreateWebhookAsync(webhookId, version, new Dictionary<string, byte[]>
        {
            ["old.dll"] = "old_code".GetBytes()
        });

        var updatedFiles = new Dictionary<string, byte[]>
        {
            ["new1.dll"] = "new1_code".GetBytes(),
            ["new2.dll"] = "new2_code".GetBytes(),
            ["config.json"] = "{}".GetBytes()
        };
        await _webhookService.UpdateCodeAsync(webhookId, version, updatedFiles);

        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(3);
        code.ShouldContainKey("new1.dll");
        code.ShouldContainKey("new2.dll");
        code.ShouldContainKey("config.json");
    }
}