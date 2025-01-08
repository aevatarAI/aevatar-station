using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;

namespace Aevatar.CQRS;

public class ElasticIndexingService : IIndexingService
{
    private readonly IElasticClient _elasticClient;
    private readonly ILogger<ElasticIndexingService> _logger;
    private const string IndexSuffix = "index";
    private const string CTime = "CTime";
    private const int DefaultPageIndex = 1;
    private const int DefaultPageSize = 50;

    public ElasticIndexingService(ILogger<ElasticIndexingService> logger, IElasticClient elasticClient)
    {
        _logger = logger;
        _elasticClient = elasticClient;
    }

    public void CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase
    {
        var indexName = stateBase.GetType().Name.ToLower() + IndexSuffix;
        var indexExistsResponse = _elasticClient.Indices.Exists(indexName);
        if (indexExistsResponse.Exists)
        {
            return;
        }

        var createIndexResponse = _elasticClient.Indices.Create(indexName, c => c
            .Map<T>(m => m
                .AutoMap()
                .Properties(props =>
                {
                    var type = stateBase.GetType();
                    foreach (var property in type.GetProperties())
                    {
                        var propertyName = property.Name;
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
            _logger.LogError("Error creating state index. indexName:{indexName}  {error}", indexName,
                createIndexResponse.ServerError?.Error);
        }
        else
        {
            _logger.LogError("Successfully created state index . indexName:{indexName}", indexName);
        }
    }

    public async Task SaveOrUpdateStateIndexAsync<T>(string id, T stateBase) where T : StateBase
    {
        var indexName = stateBase.GetType().Name.ToLower() + IndexSuffix;
        var properties = stateBase.GetType().GetProperties();
        var document = new Dictionary<string, object>();

        foreach (var property in properties)
        {
            var value = property.GetValue(stateBase);
            if (value is IList or IDictionary)
            {
                document[property.Name] = JsonConvert.SerializeObject(value);
            }
            else
            {
                document.Add(property.Name, value);
            }
        }

        document.Add(CTime, DateTime.Now);

        var response = await _elasticClient.IndexAsync(document, i => i
            .Index(indexName)
            .Id(id)
        );

        if (!response.IsValid)
        {
            _logger.LogInformation("State {indexName} save Error, indexing document error:{error}: ", indexName,
                response.ServerError);
        }
        else
        {
            _logger.LogInformation("State {indexName} save Successfully.", indexName);
        }
    }

    public async Task<BaseStateIndex> QueryStateIndexAsync(string id, string indexName)
    {
        var response = await _elasticClient.GetAsync<BaseStateIndex>(id, g => g.Index(indexName));
        return response.Source;
    }

    public void CheckExistOrCreateIndex<T>(T baseIndex) where T : BaseIndex
    {
        var indexName = baseIndex.GetType().Name.ToLower();
        var indexExistsResponse = _elasticClient.Indices.Exists(indexName);
        if (indexExistsResponse.Exists)
        {
            return;
        }

        var createIndexResponse = _elasticClient.Indices.Create(indexName, c => c
            .Map<T>(m => m.AutoMap())
        );
        if (!createIndexResponse.IsValid)
        {
            _logger.LogError("Error creating index. indexName:{indexName}  {error}", indexName,
                createIndexResponse.ServerError?.Error);
        }
        else
        {
            _logger.LogError("Successfully created index . indexName:{indexName}", indexName);
        }
    }

    public async Task SaveOrUpdateGEventIndexAsync<T>(string id, T baseIndex) where T : BaseIndex
    {
        var indexName = baseIndex.GetType().Name.ToLower();
        var properties = baseIndex.GetType().GetProperties();
        var document = new Dictionary<string, object>();

        foreach (var property in properties)
        {
            var value = property.GetValue(baseIndex);
            if (value is IList or IDictionary)
            {
                document[property.Name] = JsonConvert.SerializeObject(value);
            }
            else
            {
                document.Add(property.Name, value);
            }
        }

        document.Add(CTime, DateTime.Now);

        var response = await _elasticClient.IndexAsync(document, i => i
            .Index(indexName)
            .Id(id)
        );

        if (!response.IsValid)
        {
            _logger.LogInformation("Index: {indexName} save Error, indexing document error:{error}: ", indexName,
                response.ServerError);
        }
        else
        {
            _logger.LogInformation("Index: {indexName} save Successfully.", indexName);
        }
    }

    public async Task<string> QueryEventIndexAsync(string id, string indexName)
    {
        try
        {
            var response = await _elasticClient.GetAsync<dynamic>(id, g => g.Index(indexName));
            var source = response.Source;
            if (source == null)
            {
                return "";
            }

            var documentContent = JsonConvert.SerializeObject(source);
            return documentContent;
        }
        catch (Exception e)
        {
            _logger.LogInformation("{indexName} ,id:{id}QueryEventIndexAsync fail.", indexName, id);
            throw e;
        }
    }

    public async Task<string> QueryAsync<T>(Func<QueryContainerDescriptor<T>, QueryContainer> query,
        int pageNumber = DefaultPageIndex, int pageSize = DefaultPageSize, Func<SortDescriptor<T>, IPromise<IList<ISort>>> sort = null)
        where T : BaseIndex
    {
        /*var queryJson = JsonConvert.SerializeObject(query);
        var sortJson = JsonConvert.SerializeObject(sort);*/
        var indexName = typeof(T).Name.ToLower();
        try
        {
            var response = await _elasticClient.SearchAsync<T>(s => s
                .Index(indexName)
                .Query(query)
                .Sort(sort)
                .From((pageNumber - 1) * pageSize)
                .Size(pageSize)
            );

            if (!response.IsValid)
            {
                _logger.LogError("{IndexName} QueryAsync fail. Error: {Error}", indexName, response.ServerError);
                return null;
            }

            var documents = JsonConvert.SerializeObject(response.Documents);
            return documents;
        }
        catch (Exception e)
        {
            _logger.LogError("{IndexName} QueryAsync fail. Exception: {Error}", indexName, e.Message);
            throw;
        }
    }
}