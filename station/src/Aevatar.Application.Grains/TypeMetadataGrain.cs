// ABOUTME: This file implements the TypeMetadataGrain for Orleans cluster-wide metadata persistence
// ABOUTME: Provides distributed caching and persistence of agent type metadata across Orleans silos

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Application.Models;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Application.Grains
{
    public class TypeMetadataGrain : Grain, ITypeMetadataGrain
    {
        private readonly IPersistentState<TypeMetadataState> _state;

        public TypeMetadataGrain([PersistentState("typeMetadata", "PubSubStore")] IPersistentState<TypeMetadataState> state)
        {
            _state = state;
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
}