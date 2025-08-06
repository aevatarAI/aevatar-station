using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.CQRS.Dto;
using Aevatar.Query;
using Aevatar.CQRS.Provider;
using Aevatar.Options;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace Aevatar;

public class ElasticIndexingService : IIndexingService, ISingletonDependency
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticIndexingService> _logger;
    private const string CTime = "cTime";
    private const int DefaultSkip = 0;
    private const int DefaultLimit = 1000;
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IMemoryCache _cache;
    private readonly IOptionsSnapshot<HostOptions> _options;
    
    private static readonly HashSet<Type> SupportedDictionaryTypes = new()
    {
        typeof(Dictionary<,>),
        typeof(IDictionary<,>),
        typeof(IReadOnlyDictionary<,>),
        typeof(SortedDictionary<,>),
        typeof(ConcurrentDictionary<,>),
        typeof(ImmutableDictionary<,>),
        typeof(ReadOnlyDictionary<,>)
    };
    
    private static readonly HashSet<Type> SupportedCollectionTypes = new()
    {
        typeof(List<>),
        typeof(IList<>),
        typeof(IEnumerable<>),
        typeof(ICollection<>),
        typeof(HashSet<>),
        typeof(Array),
        typeof(ReadOnlyCollection<>),
        typeof(Queue<>),
        typeof(Stack<>),
        typeof(ConcurrentBag<>),
        typeof(IReadOnlyCollection<>),
        typeof(IReadOnlyList<>),
        typeof(ISet<>),
    };

    static ElasticIndexingService()
    {
        foreach (var dictionaryType in SupportedDictionaryTypes)
        {
            SupportedCollectionTypes.Add(dictionaryType);
        }
    }

    public ElasticIndexingService(ILogger<ElasticIndexingService> logger, ElasticsearchClient client,
        ICQRSProvider cqrsProvider, IMemoryCache cache, IOptionsSnapshot<HostOptions> hostOptions)
    {
        _logger = logger;
        _client = client;
        _cqrsProvider = cqrsProvider;
        _cache = cache;
        _options = hostOptions;
    }

    public string GetIndexName(string index)
    {
        return $"{CqrsConstant.IndexPrefix}-{_options.Value.HostId}-{index}{CqrsConstant.IndexSuffix}".ToLower();
    }

    public async Task CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase
    {
        var indexName = GetIndexName(stateBase.GetType().Name.ToLower());
        if (_cache.TryGetValue(indexName, out bool? _))
        {
            return;
        }

        var indexExistsResponse = _client.Indices.Exists(indexName);
        if (!indexExistsResponse.Exists)
        {
            var createIndexResponse = await CreateIndexAsync<T>(indexName);

            if (!createIndexResponse.IsValidResponse)
            {
                _logger.LogError(
                    "Error creating state index. indexName:{indexName},error:{error},DebugInfo:{DebugInfo}",
                    indexName,
                    createIndexResponse.ElasticsearchServerError?.Error,
                    createIndexResponse.DebugInformation);
                return;
            }

            _logger.LogInformation("Successfully created state index. indexName:{indexName}", indexName);
        }

        _cache.Set(indexName, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
    }

    private async Task<CreateIndexResponse> CreateIndexAsync<T>(string indexName) where T : StateBase
    {
        var createIndexResponse = await _client.Indices.CreateAsync(indexName, c => c
            .Mappings(m => m
                .Properties<T>(props =>
                {
                    var type = typeof(T);
                    foreach (var property in type.GetProperties())
                    {
                        var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                        var propType = property.PropertyType;

                        // Map based on property type
                        if (propType == typeof(string))
                        {
                            props.Text(propertyName);
                           // props.Keyword(propertyName, k => k.IgnoreAbove(256));
                        }
                        else if (propType == typeof(short) || propType == typeof(int) || propType == typeof(long))
                        {
                            props.LongNumber(propertyName);
                        }
                        else if (propType == typeof(float))
                        {
                            props.FloatNumber(propertyName);
                        }
                        else if (propType == typeof(double) || propType == typeof(decimal))
                        {
                            props.DoubleNumber(propertyName);
                        }
                        else if (propType == typeof(DateTime))
                        {
                            props.Date(propertyName); // Date for datetime fields
                        }
                        else if (propType == typeof(bool))
                        {
                            props.Boolean(propertyName); // Boolean for boolean fields
                        }
                        else if (propType == typeof(Guid))
                        {
                            props.Keyword(propertyName, k => k.ToString());
                        }
                        else
                        {
                            props.Text(propertyName);
                        }
                    }

                    // Add CTime as a Date field
                    props.Date("CTime");
                })
            )
        );

        return createIndexResponse;
    }

    private static bool IsBasicType(Type type)
    {
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsGenericType)
        {
            var genericDef = underlyingType.GetGenericTypeDefinition();
            if (SupportedCollectionTypes.Contains(genericDef))
            {
                var genericArgs = underlyingType.GetGenericArguments();
                if (genericArgs.Length == 2 && SupportedDictionaryTypes.Contains(genericDef))
                {
                    return IsBasicType(genericArgs[0]) && IsBasicType(genericArgs[1]);
                }
                
                return IsBasicType(genericArgs[0]);
            }
        }

        if (underlyingType.IsPrimitive)
            return true;

        if (underlyingType == typeof(string) ||
            underlyingType == typeof(DateTime) ||
            underlyingType == typeof(decimal) ||
            underlyingType == typeof(Guid))
            return true;
        return false;
    }

    public async Task SaveOrUpdateStateIndexBatchAsync(IEnumerable<SaveStateCommand> commands)
    {
        var bulkOperations = new BulkOperationsCollection();

        foreach (var command in commands)
        {
            var (stateBase, id) = (command.State, command.GuidKey);
            var indexName = GetIndexName(stateBase.GetType().Name.ToLower());
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
                    document[propertyName] = value;
                }
            }

            document["ctime"] = DateTime.UtcNow;
            document["version"] = command.Version;

            // Use BulkUpdateOperation with script-based version checking for updates
            var item = new BulkUpdateOperation<Dictionary<string, object>, object>(id)
            {
                Index = indexName,
                Script = new Script
                {
                    Source = "if (ctx.op == 'create' || ctx._source.version == null || params.version > ctx._source.version) { ctx._source = params.doc; } else { ctx.op = 'noop'; }",
                    Params = new Dictionary<string, object>
                    {
                        ["version"] = document["version"],
                        ["doc"] = document
                    }
                },
                ScriptedUpsert = true,
                Upsert = document
            };

            bulkOperations.Add(item);
        }

        var bulkRequest = new BulkRequest
        {
            Operations = bulkOperations,
            Refresh = Refresh.WaitFor
        };

        var response = await _client.BulkAsync(bulkRequest);

        ProcessBulkResponse(response);
    }

    private void ProcessBulkResponse(BulkResponse response)
    {
        if (response.Errors)
        {
            var errorDetails = response.Items
                .Where(item => item.Error != null)
                .Select(item => new
                {
                    DocumentId = item.Id,
                    ErrorType = item.Error?.Type,
                    ErrorReason = item.Error?.Reason
                });

            _logger.LogError(
                "Save State Batch Error: {ErrorCount} failures. Details: {Errors}",
                errorDetails.Count(),
                JsonConvert.SerializeObject(errorDetails)
            );
        }
    }


    public async Task<string> GetStateIndexDocumentsAsync(string stateName,
        Action<QueryDescriptor<dynamic>> query, int skip = DefaultSkip, int limit = DefaultLimit)
    {
        var indexName = GetIndexName(stateName.ToLower());
        try
        {
            var response = await _client.SearchAsync<dynamic>(s => s
                .Index(indexName)
                .Query(query)
                .From(skip)
                .Size(limit));

            if (!response.IsValidResponse)
            {
                var errorReason = response.ElasticsearchServerError?.Error?.Reason;
                _logger.LogError(
                    "State documents query failed. Index: {Index}, Error: {Error}, Debug: {Debug}",
                    indexName,
                    errorReason,
                    response.DebugInformation);
                return string.Empty;
            }

            var documents = response.Hits.Select(hit => hit.Source).ToList();
            if (documents.Count == 0)
            {
                return "";
            }

            var documentContent = documents.FirstOrDefault("")!.ToString();
            return documentContent;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "state documents query Exception,indexName:{indexName}", indexName);
            throw;
        }
    }

    public async Task<PagedResultDto<Dictionary<string, object>>> QueryWithLuceneAsync(LuceneQueryDto queryDto)
    {
        _logger.LogInformation("[Lucene Query] Index: {Index}, Query: {QueryString}",
            queryDto.StateName, queryDto.QueryString);

        var sortOptions = new List<SortOptions>();
        foreach (var sortField in queryDto.SortFields)
        {
            var parts = sortField.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid sort field: {SortField}", sortField);
                continue;
            }

            var fieldName = parts[0].Trim();
            var sortOrder = parts[1].Trim().ToLower();
            if (sortOrder != "asc" && sortOrder != "desc")
            {
                _logger.LogWarning("Invalid sort order for field: {Field}. Expected 'asc' or 'desc'.", fieldName);
                continue;
            }

            var order = sortOrder == "desc" ? SortOrder.Desc : SortOrder.Asc;

            var field = new Field(fieldName);
            var fieldSort = new FieldSort { Order = order };
            sortOptions.Add(SortOptions.Field(field, fieldSort));
        }

        var from = queryDto.PageIndex * queryDto.PageSize;
        var size = queryDto.PageSize;

        var index = GetIndexName(queryDto.StateName);


        var searchRequest = new SearchRequest<Dictionary<string, object>>(index)
        {
            From = from,
            Size = size,

            Sort = sortOptions
        };
        if (!queryDto.QueryString.IsNullOrEmpty())
        {
            searchRequest.Query = new QueryStringQuery
            {
                Query = queryDto.QueryString,
                AllowLeadingWildcard = false
            };
        }

        var response = await _client.SearchAsync<Dictionary<string, object>>(searchRequest);

        if (!response.IsValidResponse)
        {
            var error = response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error";
            var errorType = response.ElasticsearchServerError?.Error?.Type;
            _logger.LogError("Elasticsearch query failed: {Error}, Debug: {Debug}",
                error, response.DebugInformation);
            throw new UserFriendlyException($"ES Query Failed: {error}", code: errorType);
        }

        var total = response.Total;
        var results = response.Hits
            .Select(h => ConvertJsonElementToDictionary(h.Source))
            .Where(s => s != null)
            .ToList();

        _logger.LogInformation("[Lucene Query] Index: {Index}, Found {Count} results",
            queryDto.StateName, results.Count);

        return new PagedResultDto<Dictionary<string, object>>(total, results);
    }

    public async Task<long> CountWithLuceneAsync(LuceneQueryDto queryDto)
    {
        _logger.LogInformation("[Lucene Count] Index: {Index}, Query: {QueryString}",
            queryDto.StateName, queryDto.QueryString);

        try
        {
            var index = GetIndexName(queryDto.StateName);
            
            var countRequest = new CountRequest(index);
            
            if (!queryDto.QueryString.IsNullOrEmpty())
            {
                countRequest.Query = new QueryStringQuery
                {
                    Query = queryDto.QueryString,
                    AllowLeadingWildcard = false
                };
            }

            var response = await _client.CountAsync(countRequest);

            if (!response.IsValidResponse)
            {
                var error = response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error";
                _logger.LogError("Elasticsearch count failed: {Error}, Debug: {Debug}",
                    error, response.DebugInformation);
                throw new UserFriendlyException($"ES Count Failed: {error}");
            }

            var count = response.Count;
            _logger.LogInformation("[Lucene Count] Index: {Index}, Total Count: {Count}",
                queryDto.StateName, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Lucene Count] Exception occurred. Index: {Index}", queryDto.StateName);
            throw new UserFriendlyException(ex.Message);
        }
    }
    
    public async Task<long> CountWithLuceneAsync(LuceneQueryDto queryDto)
    {
        _logger.LogInformation("[Lucene Count] Index: {Index}, Query: {QueryString}",
            queryDto.StateName, queryDto.QueryString);

        try
        {
            var index = GetIndexName(queryDto.StateName);

            var countRequest = new CountRequest(index);

            if (!queryDto.QueryString.IsNullOrEmpty())
            {
                countRequest.Query = new QueryStringQuery
                {
                    Query = queryDto.QueryString,
                    AllowLeadingWildcard = false
                };
            }

            var response = await _client.CountAsync(countRequest);

            if (!response.IsValidResponse)
            {
                var error = response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error";
                _logger.LogError("Elasticsearch count failed: {Error}, Debug: {Debug}",
                    error, response.DebugInformation);
                throw new UserFriendlyException($"ES Count Failed: {error}");
            }

            var count = response.Count;
            _logger.LogInformation("[Lucene Count] Index: {Index}, Total Count: {Count}",
                queryDto.StateName, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Lucene Count] Exception occurred. Index: {Index}", queryDto.StateName);
            throw new UserFriendlyException(ex.Message);
        }
    }

    private static Dictionary<string, object?> ConvertJsonElementToDictionary(Dictionary<string, object?> source)
    {
        if (source == null)
            return null;

        var result = new Dictionary<string, object?>();
        foreach (var key in source.Keys)
        {
            var value = source[key];
            if (value is JsonElement element)
            {
                result[key] = ConvertJsonElement(element);
            }
            else
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                prop => prop.Name,
                prop => ConvertJsonElement(prop.Value)
            ),
            _ => null
        };
    }
}