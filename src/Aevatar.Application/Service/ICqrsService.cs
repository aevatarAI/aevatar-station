using System;
using System.Threading.Tasks;
using Aevatar.CQRS;
using Aevatar.Query;

namespace Aevatar.Service;

public interface ICqrsService
{
    Task<AgentEventLogsDto> QueryGEventAsync(Guid? guid, string agentType, int pageIndex, int pageSize);
    Task<AgentStateDto> QueryStateAsync(string stateName, Guid guid);
}