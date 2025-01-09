using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;

namespace Aevatar.CQRS;

public interface IIndexingService
{
    public void CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase;
    public Task SaveOrUpdateStateIndexAsync<T>(string id, T stateBase) where T : StateBase;
    
    public Task<BaseStateIndex> QueryStateIndexAsync(string id,string indexName);

    public void CheckExistOrCreateGEventIndex<T>(T gEvent) where T : GEventBase;
    
    public Task SaveOrUpdateGEventIndexAsync<T>(T gEvent) where T : GEventBase;

    public Task<string> QueryEventIndexAsync(string id, string indexName);


}