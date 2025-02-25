using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Nest;

namespace Aevatar.CQRS.Provider;

public interface ICQRSProvider : IEventDispatcher
{
    Task<string> QueryStateAsync(string indexName,Func<QueryContainerDescriptor<dynamic>, QueryContainer> query,int skip, int limit);
    
    Task<Tuple<long, List<AgentGEventIndex>>> QueryGEventAsync(string eventId, List<string> grainIds, int pageNumber, int pageSize);
    
    Task<Tuple<long, List<AgentGEventIndex>>> QueryAgentGEventAsync(Guid? primaryKey, string agentType, int pageNumber, int pageSize);
  
    Task<string> QueryAgentStateAsync(string stateName, Guid primaryKey);

    Task<Tuple<long, List<TargetT>>> GetUserInstanceAgent<SourceT,TargetT>(Guid userId, int pageIndex, int pageSize);
}