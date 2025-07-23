using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Aevatar.CQRS;
using Aevatar.CQRS.Dto;
using Aevatar.Query;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Volo.Abp.Application.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using System;

namespace Aevatar;

/// <summary>
/// Decorator for ElasticIndexingService that adds metrics and traces
/// </summary>
public class MetricsElasticIndexingService : IIndexingService
{
    private readonly IIndexingService _inner;
    private readonly ILogger<MetricsElasticIndexingService> _logger;
    private readonly ActivitySource _activitySource;
    
    // Metrics for SaveOrUpdateStateIndexBatchAsync
    private readonly Histogram<double> _bulkDurationHistogram;
    private readonly Counter<long> _bulkSuccessCounter;
    private readonly Counter<long> _bulkFailCounter;
    
    // Metrics for CheckExistOrCreateStateIndex
    private readonly Histogram<double> _checkOrCreateDurationHistogram;
    private readonly Counter<long> _checkOrCreateSuccessCounter;
    private readonly Counter<long> _checkOrCreateFailCounter;

    // Metrics for GetStateIndexDocumentsAsync
    private readonly Histogram<double> _getDocumentsDurationHistogram;
    private readonly Counter<long> _getDocumentsSuccessCounter;
    private readonly Counter<long> _getDocumentsFailCounter;

    // Metrics for QueryWithLuceneAsync
    private readonly Histogram<double> _queryLuceneDurationHistogram;
    private readonly Counter<long> _queryLuceneSuccessCounter;
    private readonly Counter<long> _queryLuceneFailCounter;

    // Metrics for CountWithLuceneAsync
    private readonly Histogram<double> _countLuceneDurationHistogram;
    private readonly Counter<long> _countLuceneSuccessCounter;
    private readonly Counter<long> _countLuceneFailCounter;

    public MetricsElasticIndexingService(IIndexingService inner, ILogger<MetricsElasticIndexingService> logger)
    {
        _inner = inner;
        _logger = logger;
        _activitySource = new ActivitySource("Aevatar.CQRS");
        var meter = new Meter("Aevatar.CQRS");
        
        _bulkDurationHistogram = meter.CreateHistogram<double>("es.bulk.duration", "ms", "ElasticSearch bulk operation duration");
        _bulkSuccessCounter = meter.CreateCounter<long>("es.bulk.success", "count", "ElasticSearch bulk operations succeeded");
        _bulkFailCounter = meter.CreateCounter<long>("es.bulk.failure", "count", "ElasticSearch bulk operations failed");

        _checkOrCreateDurationHistogram = meter.CreateHistogram<double>("es.check_create.duration", "ms", "ElasticSearch check or create index operation duration");
        _checkOrCreateSuccessCounter = meter.CreateCounter<long>("es.check_create.success", "count", "ElasticSearch check or create index operations succeeded");
        _checkOrCreateFailCounter = meter.CreateCounter<long>("es.check_create.failure", "count", "ElasticSearch check or create index operations failed");

        _getDocumentsDurationHistogram = meter.CreateHistogram<double>("es.get_documents.duration", "ms", "ElasticSearch get documents operation duration");
        _getDocumentsSuccessCounter = meter.CreateCounter<long>("es.get_documents.success", "count", "ElasticSearch get documents operations succeeded");
        _getDocumentsFailCounter = meter.CreateCounter<long>("es.get_documents.failure", "count", "ElasticSearch get documents operations failed");

        _queryLuceneDurationHistogram = meter.CreateHistogram<double>("es.query_lucene.duration", "ms", "ElasticSearch Lucene query operation duration");
        _queryLuceneSuccessCounter = meter.CreateCounter<long>("es.query_lucene.success", "count", "ElasticSearch Lucene query operations succeeded");
        _queryLuceneFailCounter = meter.CreateCounter<long>("es.query_lucene.failure", "count", "ElasticSearch Lucene query operations failed");

        _countLuceneDurationHistogram = meter.CreateHistogram<double>("es.count_lucene.duration", "ms", "ElasticSearch Lucene count operation duration");
        _countLuceneSuccessCounter = meter.CreateCounter<long>("es.count_lucene.success", "count", "ElasticSearch Lucene count operations succeeded");
        _countLuceneFailCounter = meter.CreateCounter<long>("es.count_lucene.failure", "count", "ElasticSearch Lucene count operations failed");
    }

    public async Task SaveOrUpdateStateIndexBatchAsync(IEnumerable<SaveStateCommand> commands)
    {
        using var activity = _activitySource.StartActivity("SaveOrUpdateStateIndexBatchAsync", ActivityKind.Client);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _inner.SaveOrUpdateStateIndexBatchAsync(commands);
            stopwatch.Stop();
            _bulkSuccessCounter.Add(1);
            _bulkDurationHistogram.Record(stopwatch.ElapsedMilliseconds);
            activity?.SetTag("es.bulk.success", 1);
            _logger.LogInformation("[ES-Bulk] traceId:{traceId} spanId:{spanId} success", activity?.TraceId, activity?.SpanId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _bulkFailCounter.Add(1);
            activity?.SetTag("exception", true);
            activity?.SetTag("exception.message", ex.Message);
            _logger.LogError(ex, "[ES-Bulk-Error] traceId:{traceId} spanId:{spanId}", activity?.TraceId, activity?.SpanId);
            throw;
        }
        finally
        {
            activity?.SetTag("es.bulk.elapsedMs", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase
    {
        using var activity = _activitySource.StartActivity("CheckExistOrCreateStateIndex", ActivityKind.Client);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _inner.CheckExistOrCreateStateIndex(stateBase);
            stopwatch.Stop();
            _checkOrCreateDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
            _checkOrCreateSuccessCounter.Add(1);
            activity?.SetTag("es.check_create.success", 1);
            _logger.LogInformation("[ES-CheckOrCreate] traceId:{traceId} spanId:{spanId} success", activity?.TraceId, activity?.SpanId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _checkOrCreateDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
            _checkOrCreateFailCounter.Add(1);
            activity?.SetTag("exception", true);
            activity?.SetTag("exception.message", ex.Message);
            _logger.LogError(ex, "[ES-CheckOrCreate-Error] traceId:{traceId} spanId:{spanId}", activity?.TraceId, activity?.SpanId);
            throw;
        }
        finally
        {
            activity?.SetTag("es.check_create.elapsedMs", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<string> GetStateIndexDocumentsAsync(string stateName, Action<QueryDescriptor<dynamic>> query, int skip = 0, int limit = 1000)
    {
        using var activity = _activitySource.StartActivity("GetStateIndexDocumentsAsync", ActivityKind.Client);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _inner.GetStateIndexDocumentsAsync(stateName, query, skip, limit);
            stopwatch.Stop();
            _getDocumentsDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
            _getDocumentsSuccessCounter.Add(1);
            activity?.SetTag("es.get_documents.success", 1);
            _logger.LogInformation("[ES-GetDocs] traceId:{traceId} spanId:{spanId} success", activity?.TraceId, activity?.SpanId);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _getDocumentsDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
            _getDocumentsFailCounter.Add(1);
            activity?.SetTag("exception", true);
            activity?.SetTag("exception.message", ex.Message);
            _logger.LogError(ex, "[ES-GetDocs-Error] traceId:{traceId} spanId:{spanId}", activity?.TraceId, activity?.SpanId);
            throw;
        }
        finally
        {
            activity?.SetTag("es.get_documents.elapsedMs", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<PagedResultDto<Dictionary<string, object>>> QueryWithLuceneAsync(LuceneQueryDto queryDto)
    {
        using var activity = _activitySource.StartActivity("QueryWithLuceneAsync", ActivityKind.Client);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _inner.QueryWithLuceneAsync(queryDto);
            stopwatch.Stop();
            _queryLuceneDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
            _queryLuceneSuccessCounter.Add(1);
            activity?.SetTag("es.query_lucene.success", 1);
            _logger.LogInformation("[ES-LuceneQuery] traceId:{traceId} spanId:{spanId} success", activity?.TraceId, activity?.SpanId);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _queryLuceneDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
            _queryLuceneFailCounter.Add(1);
            activity?.SetTag("exception", true);
            activity?.SetTag("exception.message", ex.Message);
            _logger.LogError(ex, "[ES-LuceneQuery-Error] traceId:{traceId} spanId:{spanId}", activity?.TraceId, activity?.SpanId);
            throw;
        }
        finally
        {
            activity?.SetTag("es.query_lucene.elapsedMs", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<long> CountWithLuceneAsync(LuceneQueryDto queryDto)
    {
        using var activity = _activitySource.StartActivity("CountWithLuceneAsync", ActivityKind.Client);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _inner.CountWithLuceneAsync(queryDto);
            stopwatch.Stop();
            _countLuceneDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
            _countLuceneSuccessCounter.Add(1);
            activity?.SetTag("es.count_lucene.success", 1);
            _logger.LogInformation("[ES-LuceneCount] traceId:{traceId} spanId:{spanId} success", activity?.TraceId, activity?.SpanId);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _countLuceneDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
            _countLuceneFailCounter.Add(1);
            activity?.SetTag("exception", true);
            activity?.SetTag("exception.message", ex.Message);
            _logger.LogError(ex, "[ES-LuceneCount-Error] traceId:{traceId} spanId:{spanId}", activity?.TraceId, activity?.SpanId);
            throw;
        }
        finally
        {
            activity?.SetTag("es.count_lucene.elapsedMs", stopwatch.ElapsedMilliseconds);
        }
    }
} 