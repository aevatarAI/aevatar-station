// ABOUTME: This file implements the TypeMetadataGrain for Orleans cluster-wide metadata persistence
// ABOUTME: Provides distributed caching and persistence of agent type metadata across Orleans silos

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Application.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Application.Grains
{
    public class TypeMetadataGrain : Grain, ITypeMetadataGrain
    {
        private readonly IPersistentState<TypeMetadataState> _state;
        private readonly ILogger<TypeMetadataGrain> _logger;

        public TypeMetadataGrain(
            [PersistentState("typeMetadata", "PubSubStore")] IPersistentState<TypeMetadataState> state,
            ILogger<TypeMetadataGrain> logger)
        {
            _state = state;
            _logger = logger;
        }

        public async Task SetMetadataAsync(List<AgentTypeMetadata> metadata)
        {
            _state.State.Metadata = metadata ?? new List<AgentTypeMetadata>();
            await _state.WriteStateAsync();
        }

        public async Task<List<AgentTypeMetadata>> GetAllMetadataAsync()
        {
            await Task.CompletedTask;
            return _state.State.Metadata ?? new List<AgentTypeMetadata>();
        }

        public async Task<List<AgentTypeMetadata>> GetByCapabilityAsync(string capability)
        {
            await Task.CompletedTask;
            
            if (string.IsNullOrEmpty(capability))
            {
                return new List<AgentTypeMetadata>();
            }

            var metadata = _state.State.Metadata ?? new List<AgentTypeMetadata>();
            return metadata.Where(m => m.Capabilities?.Contains(capability) == true).ToList();
        }

        public async Task<AgentTypeMetadata> GetByTypeAsync(string agentType)
        {
            await Task.CompletedTask;
            
            if (string.IsNullOrEmpty(agentType))
            {
                return null;
            }

            var metadata = _state.State.Metadata ?? new List<AgentTypeMetadata>();
            return metadata.FirstOrDefault(m => m.AgentType == agentType);
        }

        public async Task ClearMetadataAsync()
        {
            _state.State.Metadata = new List<AgentTypeMetadata>();
            await _state.WriteStateAsync();
        }

        public async Task<MetadataStats> GetStatsAsync()
        {
            await Task.CompletedTask;
            
            var sizeInBytes = CalculateStateSize();
            var stats = new MetadataStats
            {
                TotalTypes = _state.State.Metadata?.Count ?? 0,
                SizeInBytes = sizeInBytes,
                PercentageOf16MB = (sizeInBytes / (16.0 * 1024 * 1024)) * 100
            };
            
            if (stats.PercentageOf16MB > 50)
            {
                _logger.LogWarning(
                    "TypeMetadata approaching capacity: {Percentage:F2}% of 16MB limit ({SizeInBytes} bytes)", 
                    stats.PercentageOf16MB, stats.SizeInBytes);
            }
            
            return stats;
        }

        private long CalculateStateSize()
        {
            try
            {
                if (_state.State.Metadata == null || _state.State.Metadata.Count == 0)
                {
                    return 0;
                }
                
                // Serialize the metadata to estimate size
                var jsonString = JsonSerializer.Serialize(_state.State.Metadata);
                return System.Text.Encoding.UTF8.GetByteCount(jsonString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate state size, returning 0");
                return 0;
            }
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await _state.ReadStateAsync();
        }
    }

    [GenerateSerializer]
    public class TypeMetadataState
    {
        [Id(0)]
        public List<AgentTypeMetadata> Metadata { get; set; } = new List<AgentTypeMetadata>();
    }
    
    [GenerateSerializer]
    public class MetadataStats
    {
        [Id(0)]
        public int TotalTypes { get; set; }
        
        [Id(1)]
        public long SizeInBytes { get; set; }
        
        [Id(2)]
        public double PercentageOf16MB { get; set; }
    }
}