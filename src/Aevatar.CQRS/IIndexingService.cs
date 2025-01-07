using System.Threading.Tasks;
using Aevatar.Agents;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;

namespace Aevatar.CQRS;

public interface IIndexingService
{
    public void CheckExistOrCreateStateIndex(string typeName);
    public Task SaveOrUpdateStateIndexAsync(string typeName,BaseStateIndex baseStateIndex);
    
    public Task<BaseStateIndex> QueryStateIndexAsync(string id,string indexName);

    public void CheckExistOrCreateGEventIndex<T>(T gEvent) where T : GEventBase;
    
    public Task SaveOrUpdateGEventIndexAsync<T>(T gEvent) where T : GEventBase;

    public Task<string> QueryEventIndexAsync(string id, string indexName);


}