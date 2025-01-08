using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Nest;

namespace Aevatar.CQRS;

public interface IIndexingService
{
    public Task CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase;
    public Task SaveOrUpdateStateIndexAsync<T>(string id, T stateBase) where T : StateBase;
    
    public Task<BaseStateIndex> QueryStateIndexAsync(string id,string indexName);

    public Task CheckExistOrCreateIndex<T>(T baseIndex) where T : BaseIndex;
    
    public Task SaveOrUpdateGEventIndexAsync<T>(string id, T baseIndex) where T : BaseIndex;

    public Task<string> QueryEventIndexAsync(string id, string indexName);
    public Task<Tuple<long, List<TEntity>>> GetSortListAsync<TEntity>(Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null
        ,Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sortFunc = null, int limit = 1000, int skip = 0, string? index = null) where TEntity : class;

}