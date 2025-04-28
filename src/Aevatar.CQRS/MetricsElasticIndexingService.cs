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

public class MetricsElasticIndexingService : IIndexingService
{
    private readonly IIndexingService _inner;
    private readonly ILogger<MetricsElasticIndexingService> _logger;
    private readonly Meter _meter;
    private readonly Histogram<double> _bulkDurationHistogram;
    private readonly Counter<long> _bulkSuccessCounter;
    private readonly Counter<long> _bulkFailCounter;

    public MetricsElasticIndexingService(IIndexingService inner, ILogger<MetricsElasticIndexingService> logger)
    {
        _inner = inner;
        _logger = logger;
        _meter = new Meter("Aevatar.ElasticIndexing", "1.0.0");
        _bulkDurationHistogram = _meter.CreateHistogram<double>("aevatar_es_bulk_duration", unit: "ms", description: "ES批量写入耗时");
        _bulkSuccessCounter = _meter.CreateCounter<long>("aevatar_es_bulk_success", unit: "operations", description: "ES批量写入成功数");
        _bulkFailCounter = _meter.CreateCounter<long>("aevatar_es_bulk_fail", unit: "operations", description: "ES批量写入失败数");
    }

    public async Task SaveOrUpdateStateIndexBatchAsync(IEnumerable<SaveStateCommand> commands)
    {
        using var activity = new Activity("ElasticIndexingService.SaveOrUpdateStateIndexBatchAsync").Start();
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

    // 其余IIndexingService方法直接转发
    public Task CheckExistOrCreateStateIndex<T>(T stateBase) where T : StateBase => _inner.CheckExistOrCreateStateIndex(stateBase);
    public Task<string> GetStateIndexDocumentsAsync(string stateName, Action<QueryDescriptor<dynamic>> query, int skip = 0, int limit = 1000) => _inner.GetStateIndexDocumentsAsync(stateName, query, skip, limit);
    public Task<PagedResultDto<Dictionary<string, object>>> QueryWithLuceneAsync(LuceneQueryDto queryDto) => _inner.QueryWithLuceneAsync(queryDto);
} 