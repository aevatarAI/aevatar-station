using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.CQRS.Dto;
using Aevatar.Query;
using Aevatar.CQRS.Provider;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Aevatar;

public class ElasticIndexingService : IIndexingService, ISingletonDependency
{
    private readonly IElasticClient _elasticClient;
    private readonly ILogger<ElasticIndexingService> _logger;
    private const string CTime = "cTime";
    private const int DefaultSkip = 0;
    private const int DefaultLimit = 1000;
    private readonly ICQRSProvider _cqrsProvider;

    public ElasticIndexingService(ILogger<ElasticIndexingService> logger, IElasticClient elasticClient,
        ICQRSProvider cqrsProvider)
    {
        _logger = logger;
        _elasticClient = elasticClient;
        _cqrsProvider = cqrsProvider;
    }

    public void CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase
    {
        var indexName = _cqrsProvider.GetIndexName(stateBase.GetType().Name.ToLower());
        var indexExistsResponse = _elasticClient.Indices.Exists(indexName);
        if (indexExistsResponse.Exists)
        {
            return;
        }

        var createIndexResponse = _elasticClient.Indices.Create(indexName, c => c
            .Map<T>(m => m
                .Dynamic(DynamicMapping.Strict)
                .DynamicTemplates(dt => dt
                    .DynamicTemplate("numbers_as_integer", t => t
                        .MatchMappingType("long")
                        .Mapping(f => { return new NumberProperty(NumberType.Long); })
                    )
                    .DynamicTemplate("strings_as_text", t => t
                        .MatchMappingType("string")
                        .Mapping(_ => new TextProperty
                        {
                            Fields = new Properties { { "keyword", new KeywordProperty() } }
                        })
                    ).DynamicTemplate("numbers_as_float", t => t
                        .MatchMappingType("double")
                        .Mapping(f => { return new NumberProperty(NumberType.Float); })
                    )
                    .DynamicTemplate("objects_as_nested", t => t
                        .MatchMappingType("object")
                        .Mapping(f => new ObjectProperty { Dynamic = true })
                    ).DynamicTemplate("nested_objects_array", t => t
                        .PathMatch("*")
                        .MatchMappingType("object")
                        .Mapping(f => new NestedProperty())
                    )
                ).Properties(props =>
                {
                    var type = stateBase.GetType();
                    foreach (var property in type.GetProperties())
                    {
                        var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                        if (property.PropertyType == typeof(string))
                        {
                            props.Keyword(k => k
                                .Name(propertyName)
                            );
                        }
                        else if (property.PropertyType == typeof(DateTime))
                        {
                            props.Date(d => d
                                .Name(propertyName)
                            );
                        }
                        else if (property.PropertyType == typeof(Guid))
                        {
                            props.Keyword(k => k
                                .Name(propertyName)
                            );
                        }
                        else if (property.PropertyType == typeof(bool))
                        {
                            props.Boolean(b => b
                                .Name(propertyName)
                            );
                        }
                        else
                        {
                            props.Text(o => o
                                .Name(propertyName)
                            );
                        }
                    }

                    props.Date(d => d
                        .Name(CTime)
                    );
                    return props;
                })
            )
        );

        if (!createIndexResponse.IsValid)
        {
            _logger.LogError("Error creating state index. indexName:{indexName},error:{error},DebugInfo:{DebugInfo}",
                indexName,
                createIndexResponse.ServerError?.Error,
                JsonConvert.SerializeObject(createIndexResponse.DebugInformation));
        }
        else
        {
            _logger.LogInformation("Successfully created state index. indexName:{indexName}", indexName);
        }
    }

    public async Task SaveOrUpdateStateIndexAsync<T>(string id, T stateBase) where T : StateBase
    {
        var indexName = _cqrsProvider.GetIndexName(stateBase.GetType().Name.ToLower());
        var properties = stateBase.GetType().GetProperties();
        var document = new Dictionary<string, object>();

        foreach (var property in properties)
        {
            var value = property.GetValue(stateBase);
            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            if (value is IDictionary or GrainId)
            {
                document[propertyName] = JsonConvert.SerializeObject(value);
            }
            else
            {
                if (value != null)
                {
                    document.Add(propertyName, value);
                }
            }
        }

        document.Add(CTime, DateTime.UtcNow);

        var response = await _elasticClient.IndexAsync(document, i => i
            .Index(indexName)
            .Id(id)
        );

        if (!response.IsValid)
        {
            _logger.LogError(
                "Save State Error, indexing document error,indexName:{indexName} error:{error}, DebugInfo{DebugInfo} ",
                indexName,
                response.ServerError, JsonConvert.SerializeObject(response.DebugInformation));
        }
        else
        {
            _logger.LogInformation("Save State Successfully. indexName:{indexName}", indexName);
        }
    }

    public async Task<string> GetStateIndexDocumentsAsync(string indexName,
        Func<QueryContainerDescriptor<dynamic>, QueryContainer> query, int skip = DefaultSkip, int limit = DefaultLimit)
    {
        try
        {
            var response = await _elasticClient.SearchAsync<dynamic>(s => s
                .Index(indexName)
                .Query(query)
                .From(skip)
                .Size(limit));

            if (!response.IsValid)
            {
                _logger.LogError(
                    "state documents query fail, indexName:{indexName} error:{error} ,DebugInfo{DebugInfo}", indexName,
                    response.ServerError?.Error.Reason, JsonConvert.SerializeObject(response.DebugInformation));
                return "";
            }

            var documents = response.Hits.Select(hit => hit.Source).ToList();
            if (documents.Count == 0)
            {
                return "";
            }

            var documentContent = JsonConvert.SerializeObject(documents.FirstOrDefault());
            return documentContent;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "state documents query Exception,indexName:{indexName}", indexName);
            throw;
        }
    }

    public void CheckExistOrCreateIndex<T>(T baseIndex) where T : BaseIndex
    {
        _logger.LogInformation("CheckExistOrCreateIndex, indexName:{indexName}", baseIndex.GetType().Name.ToLower());

        var indexName = _cqrsProvider.GetIndexName(baseIndex.GetType().Name.ToLower());
        var indexExistsResponse = _elasticClient.Indices.Exists(indexName);
        if (indexExistsResponse.Exists)
        {
            _logger.LogInformation("Index already exists. indexName:{indexName}", indexName);
            return;
        }

        var createIndexResponse = _elasticClient.Indices.Create(indexName, c => c
            .Map<T>(m => m
                .Dynamic(false)
                .Properties(props =>
                {
                    var type = baseIndex.GetType();
                    foreach (var property in type.GetProperties())
                    {
                        var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                        if (property.PropertyType == typeof(string))
                        {
                            props.Keyword(k => k
                                .Name(propertyName)
                            );
                        }
                        else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(long))
                        {
                            props.Number(n => n
                                .Name(propertyName)
                                .Type(NumberType.Long)
                            );
                        }
                        else if (property.PropertyType == typeof(float))
                        {
                            props.Number(n => n
                                .Name(propertyName)
                                .Type(NumberType.Float)
                            );
                        }
                        else if (property.PropertyType == typeof(double) || property.PropertyType == typeof(decimal))
                        {
                            props.Number(n => n
                                .Name(propertyName)
                                .Type(NumberType.Double)
                            );
                        }
                        else if (property.PropertyType == typeof(DateTime))
                        {
                            props.Date(d => d
                                .Name(propertyName)
                            );
                        }
                        else if (property.PropertyType == typeof(Guid))
                        {
                            props.Keyword(k => k
                                .Name(propertyName)
                            );
                        }
                        else if (property.PropertyType == typeof(bool))
                        {
                            props.Boolean(b => b
                                .Name(propertyName)
                            );
                        }
                    }

                    props.Date(d => d
                        .Name(CTime)
                    );
                    return props;
                })
            )
        );
        if (!createIndexResponse.IsValid)
        {
            _logger.LogError("Error creating index. indexName:{indexName},error:{error},DebugInfo{DebugInfo}",
                indexName,
                createIndexResponse.ServerError?.Error,
                JsonConvert.SerializeObject(createIndexResponse.DebugInformation));
        }
        else
        {
            _logger.LogInformation("Successfully created index . indexName:{indexName}", indexName);
        }
    }

    public async Task SaveOrUpdateIndexAsync<T>(string id, T baseIndex) where T : BaseIndex
    {
        _logger.LogInformation("SaveOrUpdateIndexAsync, indexName:{indexName}", baseIndex.GetType().Name.ToLower());

        var indexName = _cqrsProvider.GetIndexName(baseIndex.GetType().Name.ToLower());
        var properties = baseIndex.GetType().GetProperties();
        var document = new Dictionary<string, object>();

        foreach (var property in properties)
        {
            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            var value = property.GetValue(baseIndex);
            if (value is IList or IDictionary)
            {
                document[propertyName] = JsonConvert.SerializeObject(value);
            }
            else
            {
                if (value != null)
                {
                    document.Add(propertyName, value);
                }
            }
        }

        document.Add(CTime, DateTime.UtcNow);

        var response = await _elasticClient.IndexAsync(document, i => i
            .Index(indexName)
            .Id(id)
        );

        if (!response.IsValid)
        {
            _logger.LogError("Index save Error, indexName:{indexName} error:{error},DebugInfo:{DebugInfo} ", indexName,
                response.ServerError, JsonConvert.SerializeObject(response.DebugInformation));
        }
        else
        {
            _logger.LogInformation("Index save Successfully, indexName: {indexName} ", indexName);
        }
    }

    public async Task<Tuple<long, List<TEntity>>> GetSortListAsync<TEntity>(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sortFunc = null, int limit = DefaultLimit,
        int skip = DefaultSkip, string? index = null) where TEntity : class

    {
        var indexName = index ?? _cqrsProvider.GetIndexName(typeof(TEntity).Name.ToLower());
        try
        {
            Func<SearchDescriptor<TEntity>, ISearchRequest> selector;
            if (sortFunc != null)
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filterFunc ?? (q => q.MatchAll()))
                    .Sort(sortFunc)
                    .Source(includeFieldFunc ?? (i => i.IncludeAll()))
                    .From(skip)
                    .Size(limit));
            }
            else
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filterFunc ?? (q => q.MatchAll()))
                    .Source(includeFieldFunc ?? (i => i.IncludeAll()))
                    .From(skip)
                    .Size(limit));
            }

            var result = await _elasticClient.SearchAsync(selector);
            if (result.IsValid)
            {
                return new Tuple<long, List<TEntity>>(result.Total, result.Documents.ToList());
            }

            _logger.LogError("{indexName} Search fail. error:{error}, DebugInfo{DebugInfo}", indexName,
                result.ServerError?.Error, JsonConvert.SerializeObject(result.DebugInformation));
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{indexName} Search Exception.", indexName);
            throw;
        }
    }


    public async Task<Tuple<long, string>> GetSortDataDocumentsAsync(string indexName,
        Func<QueryContainerDescriptor<dynamic>, QueryContainer> query, int skip = 0, int limit = 1000)
    {
        try
        {
            var response = await _elasticClient.SearchAsync<dynamic>(s => s
                .Index(indexName)
                .Query(query)
                .From(skip)
                .Size(limit));

            if (!response.IsValid)
            {
                _logger.LogError(
                    "index documents query fail, indexName:{indexName} error:{error} ,DebugInfo{DebugInfo}", indexName,
                    response.ServerError?.Error.Reason, JsonConvert.SerializeObject(response.DebugInformation));
                return null;
            }

            var total = response.Total;
            var documents = response.Hits.Select(hit => hit.Source);
            var documentContent = JsonConvert.SerializeObject(documents);
            return new Tuple<long, string>(total, documentContent);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "index documents query Exception,indexName:{indexName}", indexName);
            throw;
        }
    }

    public async Task<PagedResultDto<Dictionary<string, object>>> QueryWithLuceneAsync(LuceneQueryDto queryDto)
    {
        _logger.LogInformation("[Lucene Query] Index: {Index}, Query: {QueryString}", queryDto.Index,
            queryDto.QueryString);
        var sortDescriptor = new SortDescriptor<Dictionary<string, object>>();
        foreach (var sortField in queryDto.SortFields)
        {
            var parts = sortField.Split(':');
            if (parts.Length == 2)
            {
                var fieldName = parts[0].Trim();
                var sortOrder = parts[1].Trim().ToLower() == "desc" ? SortOrder.Descending : SortOrder.Ascending;
                sortDescriptor = sortDescriptor.Field(f => f.Field(fieldName).Order(sortOrder));
            }
        }

        var from = queryDto.PageIndex * queryDto.PageSize;
        var size = queryDto.PageSize;

        var searchDescriptor = new SearchDescriptor<Dictionary<string, object>>()
            .Index(queryDto.Index)
            .Query(q => q.QueryString(qs => qs.Query(queryDto.QueryString).AllowLeadingWildcard(false)))
            .From(from)
            .Size(size)
            .Sort(ss => sortDescriptor);

        var response = await _elasticClient.SearchAsync<Dictionary<string, object>>(searchDescriptor);
        if (!response.IsValid)
        {
            _logger.LogError("Elasticsearch query failed: {info}", response.DebugInformation);
            throw new UserFriendlyException("Elasticsearch query failed");
        }

        var resultList = response.Documents.ToList();
        _logger.LogInformation("[Lucene Query] Index: {Index}, Query: {QueryString}, result: {Result}", queryDto.Index,
            queryDto.QueryString, resultList);

        return new PagedResultDto<Dictionary<string, object>>(response.Total, resultList);
    }
}