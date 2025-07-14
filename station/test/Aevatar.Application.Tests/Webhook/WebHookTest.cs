using System;
    public WebHookTests()
    {
        _webhookService = GetRequiredService<IWebhookService>();
    }
    [Fact]
    public async Task CreateWebhookTestAsync()
    {
        string webhookId = "telegram";
        string version = "1";
    [Fact]
    public async Task DestroyWebhookTestAsync()
    {
        string webhookId = "telegram";
        string version = "1";
        await CreateWebhookTestAsync();
        await _webhookService.DestroyWebhookAsync(webhookId, version);
}