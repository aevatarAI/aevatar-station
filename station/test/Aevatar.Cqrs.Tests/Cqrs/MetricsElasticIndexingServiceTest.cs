using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Aevatar;
using Aevatar.CQRS;
using Aevatar.CQRS.Dto;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Aevatar.Core.Abstractions;
using Aevatar.Query;
using Volo.Abp.Application.Dtos;
using Elastic.Clients.Elasticsearch.QueryDsl;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

// 修改MockStateBase类继承StateBase
public class MockStateBase : StateBase
{
    public List<object> Children { get; set; } = new List<object>();
    public object? Parent { get; set; }
    public string? GAgentCreator { get; set; }

    public void Apply(object stateLogEvent)
    {
        // 空实现，仅满足接口要求
    }
}

public class MetricsElasticIndexingServiceTest
{
    [Fact]
    public async Task SaveOrUpdateStateIndexBatchAsync_Should_Record_Metrics_And_Trace()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.SaveOrUpdateStateIndexBatchAsync(It.IsAny<IEnumerable<SaveStateCommand>>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        var commands = new List<SaveStateCommand>();
        using var listener = new MeterListener();
        double? observedDuration = null;
        long observedSuccess = 0;
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Name == "es.bulk.duration")
                listener.EnableMeasurementEvents(instrument);
            if (instrument.Name == "es.bulk.success")
                listener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.bulk.duration")
                observedDuration = val;
        });
        listener.SetMeasurementEventCallback<long>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.bulk.success")
                observedSuccess += val;
        });
        listener.Start();
        await metricsService.SaveOrUpdateStateIndexBatchAsync(commands);
        listener.Dispose();
        mockInner.Verify(x => x.SaveOrUpdateStateIndexBatchAsync(commands), Times.Once);
        Assert.True(observedDuration.HasValue && observedDuration.Value >= 0);
        Assert.True(observedSuccess > 0);
    }

    [Fact]
    public async Task SaveOrUpdateStateIndexBatchAsync_Should_LogError_On_Exception()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.SaveOrUpdateStateIndexBatchAsync(It.IsAny<IEnumerable<SaveStateCommand>>()))
            .ThrowsAsync(new InvalidOperationException("test error"));
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        var commands = new List<SaveStateCommand>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => metricsService.SaveOrUpdateStateIndexBatchAsync(commands));
        mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[ES-Bulk-Error]")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CheckExistOrCreateStateIndex_Should_Record_Metrics_And_Trace()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.CheckExistOrCreateStateIndex(It.IsAny<MockStateBase>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        using var listener = new MeterListener();
        double? observedDuration = null;
        long observedSuccess = 0;
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Name == "es.check_create.duration")
                listener.EnableMeasurementEvents(instrument);
            if (instrument.Name == "es.check_create.success")
                listener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.check_create.duration")
                observedDuration = val;
        });
        listener.SetMeasurementEventCallback<long>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.check_create.success")
                observedSuccess += val;
        });
        listener.Start();
        await metricsService.CheckExistOrCreateStateIndex(new MockStateBase());
        listener.Dispose();
        mockInner.Verify(x => x.CheckExistOrCreateStateIndex(It.IsAny<MockStateBase>()), Times.Once);
        Assert.True(observedDuration.HasValue && observedDuration.Value >= 0);
        Assert.True(observedSuccess > 0);
    }

    [Fact]
    public async Task CheckExistOrCreateStateIndex_Should_LogError_On_Exception()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.CheckExistOrCreateStateIndex(It.IsAny<MockStateBase>()))
            .ThrowsAsync(new InvalidOperationException("test error"));
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => metricsService.CheckExistOrCreateStateIndex(new MockStateBase()));
        mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[ES-CheckOrCreate-Error]")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetStateIndexDocumentsAsync_Should_Record_Metrics_And_Trace()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.GetStateIndexDocumentsAsync(
            It.IsAny<string>(), 
            It.IsAny<Action<QueryDescriptor<dynamic>>>(), 
            It.IsAny<int>(), 
            It.IsAny<int>()))
            .ReturnsAsync("result");
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        using var listener = new MeterListener();
        double? observedDuration = null;
        long observedSuccess = 0;
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Name == "es.get_documents.duration")
                listener.EnableMeasurementEvents(instrument);
            if (instrument.Name == "es.get_documents.success")
                listener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.get_documents.duration")
                observedDuration = val;
        });
        listener.SetMeasurementEventCallback<long>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.get_documents.success")
                observedSuccess += val;
        });
        listener.Start();
        var result = await metricsService.GetStateIndexDocumentsAsync("test", q => { });
        listener.Dispose();
        mockInner.Verify(x => x.GetStateIndexDocumentsAsync("test", It.IsAny<Action<QueryDescriptor<dynamic>>>(), 0, 1000), Times.Once);
        Assert.Equal("result", result);
        Assert.True(observedDuration.HasValue && observedDuration.Value >= 0);
        Assert.True(observedSuccess > 0);
    }

    [Fact]
    public async Task GetStateIndexDocumentsAsync_Should_LogError_On_Exception()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.GetStateIndexDocumentsAsync(
            It.IsAny<string>(), 
            It.IsAny<Action<QueryDescriptor<dynamic>>>(), 
            It.IsAny<int>(), 
            It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("test error"));
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => metricsService.GetStateIndexDocumentsAsync("test", q => { }));
        mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[ES-GetDocs-Error]")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task QueryWithLuceneAsync_Should_Record_Metrics_And_Trace()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.QueryWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
            .ReturnsAsync(new PagedResultDto<Dictionary<string, object>>());
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        using var listener = new MeterListener();
        double? observedDuration = null;
        long observedSuccess = 0;
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Name == "es.query_lucene.duration")
                listener.EnableMeasurementEvents(instrument);
            if (instrument.Name == "es.query_lucene.success")
                listener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.query_lucene.duration")
                observedDuration = val;
        });
        listener.SetMeasurementEventCallback<long>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.query_lucene.success")
                observedSuccess += val;
        });
        listener.Start();
        var result = await metricsService.QueryWithLuceneAsync(new LuceneQueryDto());
        listener.Dispose();
        mockInner.Verify(x => x.QueryWithLuceneAsync(It.IsAny<LuceneQueryDto>()), Times.Once);
        Assert.NotNull(result);
        Assert.True(observedDuration.HasValue && observedDuration.Value >= 0);
        Assert.True(observedSuccess > 0);
    }

    [Fact]
    public async Task QueryWithLuceneAsync_Should_LogError_On_Exception()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.QueryWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
            .ThrowsAsync(new InvalidOperationException("test error"));
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => metricsService.QueryWithLuceneAsync(new LuceneQueryDto()));
        mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[ES-LuceneQuery-Error]")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CountWithLuceneAsync_Should_Record_Metrics_And_Trace()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
            .ReturnsAsync(12345);
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        var queryDto = new LuceneQueryDto
        {
            StateName = "TestState",
            QueryString = "status:active"
        };
        using var listener = new MeterListener();
        double? observedDuration = null;
        long observedSuccess = 0;
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Name == "es.count_lucene.duration")
                listener.EnableMeasurementEvents(instrument);
            if (instrument.Name == "es.count_lucene.success")
                listener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.count_lucene.duration")
                observedDuration = val;
        });
        listener.SetMeasurementEventCallback<long>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.count_lucene.success")
                observedSuccess += val;
        });
        listener.Start();
        var result = await metricsService.CountWithLuceneAsync(queryDto);
        listener.Dispose();
        mockInner.Verify(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()), Times.Once);
        Assert.Equal(12345, result);
        Assert.True(observedDuration.HasValue && observedDuration.Value >= 0);
        Assert.True(observedSuccess > 0);
        mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[ES-LuceneCount]")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CountWithLuceneAsync_Should_LogError_On_Exception()
    {
        var mockInner = new Mock<IIndexingService>();
        mockInner.Setup(x => x.CountWithLuceneAsync(It.IsAny<LuceneQueryDto>()))
            .ThrowsAsync(new InvalidOperationException("test error"));
        var mockLogger = new Mock<ILogger<MetricsElasticIndexingService>>();
        var metricsService = new MetricsElasticIndexingService(mockInner.Object, mockLogger.Object);
        var queryDto = new LuceneQueryDto
        {
            StateName = "TestState",
            QueryString = "status:active"
        };
        using var listener = new MeterListener();
        long observedFail = 0;
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Name == "es.count_lucene.failure")
                listener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((inst, val, tags, state) =>
        {
            if (inst.Name == "es.count_lucene.failure")
                observedFail += val;
        });
        listener.Start();
        await Assert.ThrowsAsync<InvalidOperationException>(() => metricsService.CountWithLuceneAsync(queryDto));
        listener.Dispose();
        Assert.True(observedFail > 0);
        mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[ES-LuceneCount-Error]")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
    }
} 