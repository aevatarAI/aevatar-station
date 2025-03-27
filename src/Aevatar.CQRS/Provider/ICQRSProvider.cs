using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Aevatar.CQRS.Provider;

public interface ICQRSProvider
{
    string GetIndexName(string name);

    Task<string> QueryStateAsync(string indexName, Action<QueryDescriptor<dynamic>> query,
        int skip, int limit);

    Task<string> QueryAgentStateAsync(string stateName, Guid primaryKey);

    Task<Tuple<long, List<TargetT>>> GetUserInstanceAgent<SourceT, TargetT>(Guid userId, int pageIndex, int pageSize);
}