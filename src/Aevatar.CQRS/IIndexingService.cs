using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.Query;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Volo.Abp.Application.Dtos;

namespace Aevatar.CQRS;

public interface IIndexingService
{
    public Task CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase;
    public Task SaveOrUpdateStateIndexBatchAsync(IEnumerable<SaveStateCommand> commands);

    public Task<string> GetStateIndexDocumentsAsync(string stateName,
        Action<QueryDescriptor<dynamic>> query, int skip = 0, int limit = 1000);


    Task<PagedResultDto<Dictionary<string, object>>> QueryWithLuceneAsync(LuceneQueryDto queryDto);
}