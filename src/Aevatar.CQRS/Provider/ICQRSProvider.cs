using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Aevatar.CQRS.Provider;

public interface ICQRSProvider
{
    Task<string> QueryStateAsync(string indexName, Action<QueryDescriptor<dynamic>> query,
        int skip, int limit);

    Task<string> QueryAgentStateAsync(string stateName, Guid primaryKey);
}