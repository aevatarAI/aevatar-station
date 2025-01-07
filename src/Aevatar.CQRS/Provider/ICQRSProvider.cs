using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;

namespace Aevatar.CQRS.Provider;

public interface ICQRSProvider : IEventDispatcher
{
    Task<BaseStateIndex> QueryAsync(string index, string id);
    
    Task SendEventCommandAsync(EventBase eventBase);

    Task<string> QueryGEventAsync(string index, string id);


}