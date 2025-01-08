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
            _logger.LogError("Error creating index {error}", createIndexResponse.ServerError?.Error);
        }
        else
        {
            _logger.LogError("Index created successfully. {indexName}", indexName);
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

        var response = await _elasticClient.IndexAsync(document , i => i
            .Index(indexName)
            .Id(id)
        );

        if (!response.IsValid)
        {
            _logger.LogInformation("State {indexName} save Error, indexing document error:{error}: " ,indexName, response.ServerError);
        }
        else
        {
            _logger.LogInformation("State {indexName} save Successfully.",indexName);
        }

    }

    public async Task<BaseStateIndex> QueryStateIndexAsync(string id,string indexName)
    {
        var response = await _elasticClient.GetAsync<BaseStateIndex>(id, g => g.Index(indexName));
        return response.Source; 
    }

    public void CheckExistOrCreateGEventIndex<T>(T gEvent) where T : GEventBase
    {
        var indexName = gEvent.GetType().Name.ToLower() + IndexSuffix;
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
                    var type = gEvent.GetType();
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
            _logger.LogError("Error creating gevent index {error}", createIndexResponse.ServerError?.Error);
        }
        else
        {
            _logger.LogError("Index created gevent successfully. {indexName}", indexName);
        }
    }

    public async Task SaveOrUpdateGEventIndexAsync<T>(T gEvent) where T : GEventBase
    {
        var indexName = gEvent.GetType().Name.ToLower() + IndexSuffix;
        var properties = gEvent.GetType().GetProperties();
        var document = new Dictionary<string, object>();

        foreach (var property in properties)
        {
            var value = property.GetValue(gEvent);
            document.Add(property.Name, value);
        }
        document.Add(CTime, DateTime.Now);

        var response = await _elasticClient.IndexAsync(document , i => i
            .Index(indexName)
            .Id(gEvent.Id)
        );

        if (!response.IsValid)
        {
            _logger.LogInformation("{indexName} save Error, indexing document error:{error}: " ,indexName, response.ServerError);
        }
        else
        {
            _logger.LogInformation("{indexName} save Successfully.",indexName);
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
            _logger.LogInformation("{indexName} ,id:{id}QueryEventIndexAsync fail.", indexName,id);
            throw e;
        }
        
    }
}