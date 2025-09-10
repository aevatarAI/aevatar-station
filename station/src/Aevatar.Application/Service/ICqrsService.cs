using System;
using System.Threading.Tasks;
using Aevatar.CQRS;
using Aevatar.Query;

namespace Aevatar.Service;

public interface ICqrsService
{
    Task<AgentStateDto> QueryStateAsync(string stateName, Guid guid);
}