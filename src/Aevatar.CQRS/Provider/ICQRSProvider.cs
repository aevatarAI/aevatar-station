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
    
    Task SendEventCommandAsync(EventBase eventBase);

    Task<Tuple<long, List<AgentGEventIndex>>> QueryGEventAsync(string eventId, List<string> grainIds, int pageNumber, int pageSize);
    
    Task<ChatLogPageResultDto> QueryDataListAsync(GetLogQuery command);
    
    Task SendSaveDataCommandAsync(BaseIndex index, string id);

}