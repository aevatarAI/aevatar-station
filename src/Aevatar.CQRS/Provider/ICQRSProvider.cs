using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;

namespace Aevatar.CQRS.Provider;

public interface ICQRSProvider : IEventDispatcher
{
    Task<BaseStateIndex> QueryAsync(string index, string id);
    
    Task SendEventCommandAsync(EventBase eventBase);

    Task<Tuple<long, List<AgentGEventIndex>>> QueryGEventAsync(string eventId, List<string> grainIds, int pageNumber, int pageSize);

    Task PublishAsync(Guid eventId, Guid GrainId, string GrainType, GEventBase eventBase);

}