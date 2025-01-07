using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;

namespace Aevatar.Service;

public interface ICqrsService
{
    Task<BaseStateIndex> QueryAsync(string index, string id);
    
    Task SendEventCommandAsync(EventBase eventBase);

    Task<K> QueryGEventAsync<T,K>(string index, string id) where T : GEventBase;
}