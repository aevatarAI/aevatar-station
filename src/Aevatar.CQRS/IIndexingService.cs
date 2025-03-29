using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.Query;
using Nest;
using Volo.Abp.Application.Dtos;

namespace Aevatar.CQRS;

public interface IIndexingService
{
    public void CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase;
    public Task SaveOrUpdateStateIndexBatchAsync(IEnumerable<SaveStateCommand> commands);

    public Task<string> GetStateIndexDocumentsAsync(string indexName,
        Func<QueryContainerDescriptor<dynamic>, QueryContainer> query, int skip = 0, int limit = 1000);

    public void CheckExistOrCreateIndex<T>(T baseIndex) where T : BaseIndex;

    public Task SaveOrUpdateIndexAsync<T>(string id, T baseIndex) where T : BaseIndex;

    public Task<Tuple<long, List<TEntity>>> GetSortListAsync<TEntity>(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null
        , Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sortFunc = null, int limit = 1000, int skip = 0,
        string? index = null) where TEntity : class;

    public Task<Tuple<long, string>> GetSortDataDocumentsAsync(string indexName,
        Func<QueryContainerDescriptor<dynamic>, QueryContainer> query, int skip = 0, int limit = 1000);

    Task<PagedResultDto<Dictionary<string, object>>> QueryWithLuceneAsync(LuceneQueryDto queryDto);
}