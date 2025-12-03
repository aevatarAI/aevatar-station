using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Aevatar.Core.Interception.Context;
using Aevatar.Core.Interception.Configurations;
using TracingPerformanceBenchmark.Services;
using TracingPerformanceBenchmark.Models;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Aevatar.Core.Abstractions.Tracing;

namespace TracingPerformanceBenchmark;

/// <summary>
/// Performance benchmark comparing different tracing approaches:
/// 1. No Tracing - Baseline performance
/// 2. Fody IL Weaving - Build-time code injection with FodyTraceAttribute
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, 
    warmupCount: 3, 
    iterationCount: 5,  // Reduced from 20 for faster testing
    invocationCount: 10)]  // Reduced from 100 for faster testing - 10 invocations × 1-2ms = 10-20ms per iteration
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class TracingPerformanceBenchmarks
{
    #region Fields

    private ServiceProvider _noTracingServices;
    private ServiceProvider _fodyServices;
    
    private IOrderService _noTracingOrderService;
    private IOrderService _fodyOrderService;
    
    private OrderRequest _testRequest;
    private string _testTraceId;


    #endregion

    #region Benchmark Methods

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("NoTracing")]
    public async Task<OrderResult> NoTracing_CreateOrder()
    {
        return await _noTracingOrderService.CreateOrderAsync(_testRequest);
    }

    [Benchmark]
    [BenchmarkCategory("NoTracing")]
    public async Task<OrderResult> NoTracing_ProcessOrder()
    {
        // Create an order first, then process it - completely independent
        var order = await _noTracingOrderService.CreateOrderAsync(_testRequest);
        return await _noTracingOrderService.ProcessOrderAsync(order.OrderId);
    }

    [Benchmark]
    [BenchmarkCategory("NoTracing")]
    public async Task<OrderResult> NoTracing_GetOrder()
    {
        // Create an order first, then retrieve it - completely independent
        var order = await _noTracingOrderService.CreateOrderAsync(_testRequest);
        return await _noTracingOrderService.GetOrderAsync(order.OrderId);
    }

    [Benchmark]
    [BenchmarkCategory("Fody")]
    public async Task<OrderResult> Fody_CreateOrder()
    {
        return await _fodyOrderService.CreateOrderAsync(_testRequest);
    }

    [Benchmark]
    [BenchmarkCategory("Fody")]
    public async Task<OrderResult> Fody_ProcessOrder()
    {
        // Create an order first, then process it - completely independent
        var order = await _fodyOrderService.CreateOrderAsync(_testRequest);
        return await _fodyOrderService.ProcessOrderAsync(order.OrderId);
    }

    [Benchmark]
    [BenchmarkCategory("Fody")]
    public async Task<OrderResult> Fody_GetOrder()
    {
        // Create an order first, then retrieve it - completely independent
        var order = await _fodyOrderService.CreateOrderAsync(_testRequest);
        return await _fodyOrderService.GetOrderAsync(order.OrderId);
    }

    #endregion

    #region Memory Benchmarks

    // Memory benchmarks removed - NoTracing_CreateOrder serves as our baseline
    // for comparing tracing overhead between Fody approaches

    #endregion

    #region Setup Methods

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Setup test data
        _testRequest = new OrderRequest
        {
            CustomerId = "customer_001",
            ProductId = "product_001",
            Quantity = 2,
            UnitPrice = 29.99m
        };
        
        _testTraceId = Activity.Current?.Id ?? "test-trace-001";
        
        // Setup service providers for each approach ONCE
        SetupNoTracingServices();
        SetupFodyServices();
        
        // No need to create shared orders - each benchmark is now independent
    }

    [IterationSetup(Targets = new[] { "NoTracing_CreateOrder", "NoTracing_ProcessOrder", "NoTracing_GetOrder", 
                                      "Fody_CreateOrder", "Fody_ProcessOrder", "Fody_GetOrder" })]
    public void ServiceBenchmarkSetup()
    {
        // No need to recreate services - they're already created in GlobalSetup
        // Just ensure the order exists for ProcessOrder and GetOrder benchmarks
        // The order ID is already set in GlobalSetup
    }

    [IterationSetup(Targets = new[] { "NoTracing_MemoryBaseline", "Fody_MemoryOverhead" })]
    public void MemoryBenchmarkSetup()
    {
        // Memory benchmarks don't need services - they're independent
        // This method exists just to satisfy BenchmarkDotNet's requirement for IterationSetup
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        try
        {
            _noTracingServices?.Dispose();
            _fodyServices?.Dispose();
        }
        finally
        {
            TraceContext.Clear();
        }
    }

    private void SetupNoTracingServices()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Minimal logging for performance
        });
        
        // Add order service directly (no proxy, no tracing)
        services.AddScoped<OrderService>(); // Register concrete type
        services.AddScoped<IOrderService, OrderService>();
        
        _noTracingServices = services.BuildServiceProvider();
        _noTracingOrderService = _noTracingServices.GetRequiredService<IOrderService>();
    }

    private void SetupFodyServices()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        
        // Add OpenTelemetry with OTLP exporter for Fody tracing
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddSource("Aevatar.MethodTracing")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri("http://localhost:4317");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddMeter("Aevatar.MethodTracing")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri("http://localhost:4317");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            });
        
        // Add order service (Fody weavers handle tracing at build time)
        services.AddScoped<FodyOrderService>(); // Register concrete type
        services.AddScoped<IOrderService, FodyOrderService>();
        
        _fodyServices = services.BuildServiceProvider();
        _fodyOrderService = _fodyServices.GetRequiredService<IOrderService>();
        
        // OpenTelemetry providers are automatically started when service provider is built
        
        // Enable tracing for Fody benchmark
        var fodyTraceConfig = new TraceConfig
        {
            Enabled = true,
            TrackedIds = new HashSet<string> { "fody-benchmark-trace" }
        };
        TraceContext.UpdateTraceConfig(config =>
        {
            config.Enabled = fodyTraceConfig.Enabled;
            config.TrackedIds.Clear();
            foreach (var id in fodyTraceConfig.TrackedIds)
            {
                config.TrackedIds.Add(id);
            }
        });
        TraceContext.EnableTracing("fody-benchmark-trace");
    }



    #endregion
}
