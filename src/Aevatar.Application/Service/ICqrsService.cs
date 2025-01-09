using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;

namespace Aevatar.Service;

public interface ICqrsService
{
    Task<Tuple<long, List<AgentGEventIndex>>> QueryGEventAsync(string eventId, List<string> groupIds, int pageNumber, int pageSize);
}