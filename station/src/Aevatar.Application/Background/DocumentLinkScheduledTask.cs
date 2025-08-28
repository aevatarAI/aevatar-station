using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aevatar.Background
{
    public class DocumentLinkScheduledTask : BackgroundService
    {
        private readonly ILogger<DocumentLinkScheduledTask> _logger;
        private readonly IDocumentLinkService _documentLinkService;
        private readonly TimeSpan _delay = TimeSpan.FromMinutes(30);

        public DocumentLinkScheduledTask(ILogger<DocumentLinkScheduledTask> logger, IDocumentLinkService documentLinkService)
        {
            _logger = logger;
            _documentLinkService = documentLinkService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DocumentLinkScheduledTask started at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal during shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in DocumentLinkScheduledTask loop");
                }

                try
                {
                    await Task.Delay(_delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("DocumentLinkScheduledTask stopping at: {time}", DateTimeOffset.Now);
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DocumentLinkScheduledTask tick at: {time}", DateTimeOffset.Now);
            await _documentLinkService.RefreshDocumentLinkStatusAsync();
            await Task.CompletedTask;
        }
    }
} 