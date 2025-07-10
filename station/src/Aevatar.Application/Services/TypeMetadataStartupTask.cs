// ABOUTME: This file implements the TypeMetadataStartupTask for automatic silo initialization
// ABOUTME: Ensures metadata is refreshed on silo startup and provides background refresh capability

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aevatar.Application.Services
{
    /// <summary>
    /// Orleans startup task that ensures TypeMetadata is refreshed on silo startup.
    /// </summary>
    public class TypeMetadataStartupTask : IStartupTask
    {
        private readonly ITypeMetadataService _typeMetadataService;
        private readonly ILogger<TypeMetadataStartupTask> _logger;

        public TypeMetadataStartupTask(
            ITypeMetadataService typeMetadataService,
            ILogger<TypeMetadataStartupTask> logger)
        {
            _typeMetadataService = typeMetadataService ?? throw new ArgumentNullException(nameof(typeMetadataService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            try
            {
                // Check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogInformation("Starting TypeMetadata initialization...");
                
                // Refresh metadata on startup
                await _typeMetadataService.RefreshMetadataAsync();
                
                // Check for cancellation after refresh
                cancellationToken.ThrowIfCancellationRequested();
                
                // Get stats and log warnings if needed
                var stats = await _typeMetadataService.GetStatsAsync();
                
                // Log warning if approaching MongoDB 16MB limit
                if (stats.PercentageOf16MB > 80)
                {
                    _logger.LogWarning(
                        "TypeMetadata approaching MongoDB 16MB limit: {Percentage:F1}% ({SizeInBytes} bytes, {TotalTypes} types)",
                        stats.PercentageOf16MB, stats.SizeInBytes, stats.TotalTypes);
                }
                
                // Log completion
                _logger.LogInformation("TypeMetadata initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize TypeMetadata during startup");
                throw;
            }
        }
    }
}