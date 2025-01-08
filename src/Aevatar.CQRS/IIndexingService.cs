using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Nest;

namespace Aevatar.CQRS;

public interface IIndexingService
{
    public void CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase;
    public Task SaveOrUpdateStateIndexAsync<T>(string id, T stateBase) where T : StateBase;
    
    public Task<BaseStateIndex> QueryStateIndexAsync(string id,string indexName);

    public void CheckExistOrCreateIndex<T>(T baseIndex) where T : BaseIndex;
    
    public Task SaveOrUpdateGEventIndexAsync<T>(string id, T baseIndex) where T : BaseIndex;

    public Task<string> QueryEventIndexAsync(string id, string indexName);
    public Task<string> QueryAsync<T>(Func<QueryContainerDescriptor<T>, QueryContainer> query, int pageNumber = 1, int pageSize = 10, Func<SortDescriptor<T>, IPromise<IList<ISort>>> sort = null) where T : BaseIndex;


}