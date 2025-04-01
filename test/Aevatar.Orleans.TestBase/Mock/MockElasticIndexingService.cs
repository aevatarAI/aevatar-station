using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Aevatar.Query;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Mock;

public class MockElasticIndexingService : IIndexingService, ISingletonDependency
{
    private readonly ILogger<MockElasticIndexingService> _logger;
    private readonly IMemoryCache _cache;

    private readonly ConcurrentDictionary<string, List<Dictionary<string, object>>> _indexStorage = new();

    public MockElasticIndexingService(
        ILogger<MockElasticIndexingService> logger,
        IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase
    {
        var indexName = stateBase.GetType().Name;
        if (_cache.TryGetValue(indexName, out bool? _))
        {
            return;
        }

        if (!_indexStorage.ContainsKey(indexName))
        {
            _indexStorage[indexName] = new List<Dictionary<string, object>>();
            _logger.LogInformation("Successfully created state index. indexName:{indexName}", indexName);
        }

        _cache.Set(indexName, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });

        await Task.CompletedTask;
    }

    public async Task SaveOrUpdateStateIndexBatchAsync(IEnumerable<SaveStateCommand> commands)
    {
        lock (_indexStorage)
        {
            foreach (var command in commands)
            {
                var (stateBase, id) = (command.State, command.GuidKey);
                var indexName = stateBase.GetType().Name;

                if (!_indexStorage.ContainsKey(indexName))
                {
                    _indexStorage[indexName] = new List<Dictionary<string, object>>();
                }

                var document = new Dictionary<string, object>();
                foreach (var property in stateBase.GetType().GetProperties())
                {
                    var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                    var value = property.GetValue(stateBase);
                    if (value == null)
                    {
                        continue;
                    }

                    if (!IsBasicType(property.PropertyType))
                    {
                        document[propertyName] = JsonConvert.SerializeObject(value, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        });
                    }
                    else
                    {
                        document.Add(propertyName, value);
                    }
                }

                document["ctime"] = DateTime.UtcNow;

                var existingDoc = _indexStorage[indexName]
                    .FirstOrDefault(d => d.ContainsKey("id") && d["id"].ToString().Equals(id.ToString()));
                if (existingDoc != null)
                {
                    _indexStorage[indexName].Remove(existingDoc);
                }

                document["id"] = id;
                _indexStorage[indexName].Add(document);
            }
        }

        await Task.CompletedTask;
    }

    private static bool IsBasicType(Type type)
    {
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsPrimitive)
            return true;

        if (underlyingType == typeof(string) ||
            underlyingType == typeof(DateTime) ||
            underlyingType == typeof(decimal) ||
            underlyingType == typeof(Guid))
            return true;
        return false;
    }

    public async Task<string> GetStateIndexDocumentsAsync(string indexName,
        Action<QueryDescriptor<dynamic>> query, int skip = 0, int limit = 1000)
    {
        if (!_indexStorage.ContainsKey(indexName))
        {
            _logger.LogError("Index not found: {IndexName}", indexName);
            return string.Empty;
        }

        var documents = _indexStorage[indexName]
            .Skip(skip)
            .Take(limit)
            .ToList();

        var json = JsonConvert.SerializeObject(documents);
        await Task.CompletedTask;
        return json;
    }

    public async Task<PagedResultDto<Dictionary<string, object>>> QueryWithLuceneAsync(LuceneQueryDto queryDto)
    {
        var indexName = queryDto.StateName;

        if (!_indexStorage.ContainsKey(indexName))
        {
            _logger.LogWarning("[Lucene Query] Index not found: {Index}", indexName);
            return new PagedResultDto<Dictionary<string, object>>(0, new List<Dictionary<string, object>>());
        }

        var documents = _indexStorage[indexName]
            .Where(doc => doc.Values.Any(value => value?.ToString()?.Contains(queryDto.QueryString) ?? false))
            .ToList();

        var total = documents.Count;

        documents = documents
            .Skip(queryDto.PageIndex * queryDto.PageSize)
            .Take(queryDto.PageSize)
            .ToList();

        return new PagedResultDto<Dictionary<string, object>>(total, documents);
    }
}