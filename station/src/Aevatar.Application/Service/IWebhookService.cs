using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

public interface IWebhookService : IApplicationService
{
    Task CreateWebhookAsync(string webhookId, string version, Dictionary<string, byte[]> codeFiles);
    Task<Dictionary<string, string>> GetWebhookCodeAsync(string webhookId, string version);
    Task DestroyWebhookAsync(string inputWebhookId, string inputVersion);
    Task UpdateCodeAsync(string webhookId, string version, Dictionary<string, byte[]> codeFiles);
} 