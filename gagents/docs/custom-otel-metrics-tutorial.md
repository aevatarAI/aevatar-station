# Custom OpenTelemetry Metrics Tutorial for GAgentBase Developers

This tutorial shows you how to implement custom OpenTelemetry metrics when building applications on top of GAgentBase, following the framework's established patterns and best practices.

## Table of Contents
1. [Understanding the Framework's Metrics Pattern](#understanding-the-frameworks-metrics-pattern)
2. [Setting Up Your Custom Metrics](#setting-up-your-custom-metrics)
3. [Implementing Different Metric Types](#implementing-different-metric-types)
4. [Integration with GAgentBase](#integration-with-gagentbase)
5. [Best Practices](#best-practices)
6. [Testing Your Metrics](#testing-your-metrics)

## Understanding the Framework's Metrics Pattern

The Aevatar framework uses a standardized approach for OpenTelemetry metrics:

- **Centralized Constants**: All metric names and tags are defined in constants
- **Static Metric Classes**: Metrics are organized in static classes with singleton Meters
- **Automatic Context Capture**: Uses `[CallerMemberName]` and reflection for rich context
- **Consistent Naming**: Follows `aevatar_<component>_<metric_name>` pattern

## Setting Up Your Custom Metrics

### Step 1: Define Your Metric Constants

First, create constants for your metrics. Follow the framework's naming convention:

```csharp
// MyGAgentConstants.cs
namespace MyApplication.Observability
{
    public static class MyGAgentConstants
    {
        // Meter name - use your component namespace
        public const string MyComponentMeterName = "MyApplication.BusinessLogic";
        
        // Metric names - follow aevatar_component_metric pattern
        public const string BusinessOperationDuration = "myapp_business_operation_duration";
        public const string BusinessOperationCount = "myapp_business_operation_count";
        public const string ActiveUserSessions = "myapp_active_user_sessions";
        public const string DataProcessingErrors = "myapp_data_processing_errors";
        
        // Event categories for labeling
        public const string BusinessEventType = "business_operation";
        public const string UserEventType = "user_session";
        public const string DataEventType = "data_processing";
    }
}
```

### Step 2: Create Your Metrics Class

Create a static class to manage your custom metrics:

```csharp
// BusinessOperationMetrics.cs
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace MyApplication.Observability
{
    public static class BusinessOperationMetrics
    {
        private static readonly Meter Meter = new(MyGAgentConstants.MyComponentMeterName);
        
        // Histogram for operation duration
        private static readonly Histogram<double> OperationDurationHistogram = Meter.CreateHistogram<double>(
            MyGAgentConstants.BusinessOperationDuration, "ms", "Business operation execution duration");
        
        // Counter for operation count
        private static readonly Counter<long> OperationCounter = Meter.CreateCounter<long>(
            MyGAgentConstants.BusinessOperationCount, "operations", "Total number of business operations");
        
        // UpDownCounter for active sessions (can go up and down)
        private static readonly UpDownCounter<long> ActiveSessionsGauge = Meter.CreateUpDownCounter<long>(
            MyGAgentConstants.ActiveUserSessions, "sessions", "Number of active user sessions");
        
        // Counter for errors
        private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>(
            MyGAgentConstants.DataProcessingErrors, "errors", "Number of data processing errors");

        /// <summary>
        /// Records the duration of a business operation
        /// </summary>
        public static void RecordOperationDuration(double durationMs, string operationType, 
            string grainId, bool success, ILogger? logger = null,
            [CallerMemberName] string methodName = "", 
            [CallerFilePath] string? filePath = null)
        {
            var className = GetClassNameFromFilePath(filePath);
            var fullMethodName = className != null ? $"{className}.{methodName}" : methodName;
            
            OperationDurationHistogram.Record(durationMs,
                new KeyValuePair<string, object?>("operation_type", operationType),
                new KeyValuePair<string, object?>("grain_id", grainId),
                new KeyValuePair<string, object?>("success", success),
                new KeyValuePair<string, object?>("method_name", fullMethodName),
                new KeyValuePair<string, object?>("event_category", MyGAgentConstants.BusinessEventType));
            
            // Increment operation counter
            OperationCounter.Add(1,
                new KeyValuePair<string, object?>("operation_type", operationType),
                new KeyValuePair<string, object?>("success", success));
            
            logger?.LogInformation(
                "[BusinessOperation] duration={Duration}ms operation={OperationType} grain={GrainId} success={Success} method={MethodName}",
                durationMs, operationType, grainId, success, fullMethodName);
        }

        /// <summary>
        /// Tracks user session changes
        /// </summary>
        public static void TrackSessionChange(int delta, string sessionType, ILogger? logger = null)
        {
            ActiveSessionsGauge.Add(delta,
                new KeyValuePair<string, object?>("session_type", sessionType),
                new KeyValuePair<string, object?>("event_category", MyGAgentConstants.UserEventType));
            
            logger?.LogInformation("[UserSession] delta={Delta} type={SessionType}", delta, sessionType);
        }

        /// <summary>
        /// Records data processing errors
        /// </summary>
        public static void RecordError(string errorType, string grainId, Exception? exception = null, 
            ILogger? logger = null)
        {
            ErrorCounter.Add(1,
                new KeyValuePair<string, object?>("error_type", errorType),
                new KeyValuePair<string, object?>("grain_id", grainId),
                new KeyValuePair<string, object?>("event_category", MyGAgentConstants.DataEventType));
            
            logger?.LogError(exception, "[DataProcessingError] type={ErrorType} grain={GrainId}", 
                errorType, grainId);
        }

        private static string? GetClassNameFromFilePath(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }
    }
}
```

## Implementing Different Metric Types

### Histogram - For Measuring Distributions

Best for: latency, request sizes, processing times

```csharp
// In your GAgent class
public class OrderProcessingGAgent : GAgentBase<OrderState, OrderStateLogEvent>
{
    private readonly ILogger<OrderProcessingGAgent> _logger;

    public OrderProcessingGAgent(ILogger<OrderProcessingGAgent> logger)
    {
        _logger = logger;
    }

    public async Task<OrderResult> ProcessOrderAsync(Order order)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            // Your business logic here
            var result = await ExecuteOrderProcessingAsync(order);
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            BusinessOperationMetrics.RecordError("order_processing", 
                this.GetGrainId().ToString(), ex, _logger);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            BusinessOperationMetrics.RecordOperationDuration(
                stopwatch.Elapsed.TotalMilliseconds, 
                "process_order",
                this.GetGrainId().ToString(), 
                success, 
                _logger);
        }
    }
}
```

### Counter - For Counting Events

Best for: requests, errors, completed operations

```csharp
[EventHandler]
public async Task HandlePaymentProcessedEventAsync(PaymentProcessedEvent paymentEvent)
{
    // Your event handling logic
    await ProcessPaymentAsync(paymentEvent);
    
    // Record the payment processing metric
    PaymentMetrics.RecordPaymentProcessed(
        paymentEvent.Amount, 
        paymentEvent.Currency, 
        paymentEvent.PaymentMethod,
        this.GetGrainId().ToString(),
        _logger);
}
```

### UpDownCounter (Gauge) - For Values That Go Up and Down

Best for: active connections, queue sizes, resource utilization

```csharp
public override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
{
    await base.OnGAgentActivateAsync(cancellationToken);
    
    // Track session start
    BusinessOperationMetrics.TrackSessionChange(1, "user_session", _logger);
}

public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
{
    // Track session end
    BusinessOperationMetrics.TrackSessionChange(-1, "user_session", _logger);
    
    await base.OnDeactivateAsync(reason, cancellationToken);
}
```

## Integration with GAgentBase

### Using Metrics in Event Handlers

```csharp
[GAgent]
public class InventoryGAgent : GAgentBase<InventoryState, InventoryStateLogEvent>
{
    private readonly ILogger<InventoryGAgent> _logger;

    public InventoryGAgent(ILogger<InventoryGAgent> logger)
    {
        _logger = logger;
    }

    [EventHandler]
    public async Task HandleInventoryUpdateEventAsync(InventoryUpdateEvent updateEvent)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            // Update inventory logic
            await UpdateInventoryAsync(updateEvent);
            success = true;
        }
        catch (Exception ex)
        {
            InventoryMetrics.RecordError("inventory_update", 
                this.GetGrainId().ToString(), ex, _logger);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            InventoryMetrics.RecordOperationDuration(
                stopwatch.Elapsed.TotalMilliseconds,
                "inventory_update",
                this.GetGrainId().ToString(),
                success,
                _logger);
        }
    }

    private async Task UpdateInventoryAsync(InventoryUpdateEvent updateEvent)
    {
        // Track inventory level changes
        var oldQuantity = State.Quantity;
        
        // Perform update
        State.Quantity = updateEvent.NewQuantity;
        
        // Record inventory level change
        var delta = State.Quantity - oldQuantity;
        InventoryMetrics.TrackInventoryChange(delta, updateEvent.ProductId, _logger);
        
        await ConfirmEvents();
    }
}
```

### Automated Metrics in State Changes

```csharp
protected override async Task HandleStateChangedAsync()
{
    await base.HandleStateChangedAsync();
    
    // Automatically track state changes
    StateMetrics.RecordStateChange(
        this.GetType().Name,
        this.GetGrainId().ToString(),
        State.Version,
        _logger);
}
```

## Best Practices

### 1. Metric Naming Convention

```csharp
// ✅ Good - follows framework pattern
public const string UserRegistrationDuration = "myapp_user_registration_duration";
public const string ActiveConnections = "myapp_active_connections";

// ❌ Bad - inconsistent naming
public const string RegTime = "registration_time";
public const string Connections = "conn_count";
```

### 2. Use Appropriate Tags

```csharp
// ✅ Good - rich context with reasonable cardinality
OperationHistogram.Record(duration,
    new KeyValuePair<string, object?>("operation_type", "user_registration"),
    new KeyValuePair<string, object?>("success", true),
    new KeyValuePair<string, object?>("user_tier", "premium"));

// ❌ Bad - high cardinality tags (user ID changes frequently)
OperationHistogram.Record(duration,
    new KeyValuePair<string, object?>("user_id", userId), // High cardinality!
    new KeyValuePair<string, object?>("timestamp", DateTime.Now)); // Unique values!
```

### 3. Handle Exceptions Properly

```csharp
public async Task<Result> ProcessDataAsync(DataRequest request)
{
    var stopwatch = Stopwatch.StartNew();
    var operationType = "data_processing";
    var success = false;
    
    try
    {
        var result = await PerformDataProcessingAsync(request);
        success = true;
        return result;
    }
    catch (ValidationException ex)
    {
        DataMetrics.RecordError("validation_error", this.GetGrainId().ToString(), ex, _logger);
        throw;
    }
    catch (TimeoutException ex)
    {
        DataMetrics.RecordError("timeout_error", this.GetGrainId().ToString(), ex, _logger);
        throw;
    }
    catch (Exception ex)
    {
        DataMetrics.RecordError("unknown_error", this.GetGrainId().ToString(), ex, _logger);
        throw;
    }
    finally
    {
        stopwatch.Stop();
        DataMetrics.RecordOperationDuration(
            stopwatch.Elapsed.TotalMilliseconds,
            operationType,
            this.GetGrainId().ToString(),
            success,
            _logger);
    }
}
```

### 4. Conditional Metrics Collection

```csharp
public static class AdvancedMetrics
{
    private static readonly bool MetricsEnabled = 
        Environment.GetEnvironmentVariable("ENABLE_DETAILED_METRICS") == "true";

    public static void RecordDetailedOperation(string operation, double duration)
    {
        if (!MetricsEnabled) return;
        
        DetailedOperationHistogram.Record(duration,
            new KeyValuePair<string, object?>("operation", operation));
    }
}
```

## Testing Your Metrics

### Unit Testing Metrics

```csharp
[Test]
public async Task ProcessOrder_ShouldRecordMetrics()
{
    // Arrange
    var agent = new OrderProcessingGAgent(_logger);
    var order = new Order { Id = "123", Amount = 100.50m };
    
    // Act
    await agent.ProcessOrderAsync(order);
    
    // Assert
    // Verify logs contain metric information
    _logger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[BusinessOperation]")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
}
```

### Integration Testing with Metrics Collection

```csharp
[Test]
public async Task IntegrationTest_WithMetricsCollection()
{
    // Arrange - Setup test meter listener
    using var meterProvider = Sdk.CreateMeterProviderBuilder()
        .AddMeter(MyGAgentConstants.MyComponentMeterName)
        .AddInMemoryExporter(out var exportedItems)
        .Build();
    
    var agent = new OrderProcessingGAgent(_logger);
    
    // Act
    await agent.ProcessOrderAsync(new Order { Id = "test" });
    
    // Force export
    meterProvider.ForceFlush(TimeSpan.FromSeconds(5));
    
    // Assert - Check metrics were exported
    Assert.That(exportedItems.Count, Is.GreaterThan(0));
    var operationMetric = exportedItems.FirstOrDefault(
        m => m.Name == MyGAgentConstants.BusinessOperationDuration);
    Assert.That(operationMetric, Is.Not.Null);
}
```

### Performance Testing Metrics Overhead

```csharp
[Test]
public void MetricsOverhead_ShouldBeMinimal()
{
    var stopwatch = Stopwatch.StartNew();
    const int iterations = 10000;
    
    for (int i = 0; i < iterations; i++)
    {
        BusinessOperationMetrics.RecordOperationDuration(
            100.0, "test_operation", "test_grain", true);
    }
    
    stopwatch.Stop();
    
    // Assert overhead is reasonable (adjust threshold as needed)
    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), 
        $"Metrics collection took {stopwatch.ElapsedMilliseconds}ms for {iterations} operations");
}
```

## Complete Example: E-commerce Order Processing GAgent

Here's a complete example showing how to implement comprehensive metrics for an e-commerce order processing system:

```csharp
// OrderProcessingMetrics.cs
public static class OrderProcessingMetrics
{
    private static readonly Meter Meter = new("ECommerce.OrderProcessing");
    
    // Duration metrics
    private static readonly Histogram<double> OrderProcessingDuration = Meter.CreateHistogram<double>(
        "ecommerce_order_processing_duration", "ms", "Order processing duration");
    
    private static readonly Histogram<double> PaymentProcessingDuration = Meter.CreateHistogram<double>(
        "ecommerce_payment_processing_duration", "ms", "Payment processing duration");
    
    // Counters
    private static readonly Counter<long> OrdersProcessed = Meter.CreateCounter<long>(
        "ecommerce_orders_processed_total", "orders", "Total orders processed");
    
    private static readonly Counter<long> PaymentFailures = Meter.CreateCounter<long>(
        "ecommerce_payment_failures_total", "failures", "Payment processing failures");
    
    // Gauges
    private static readonly UpDownCounter<long> PendingOrders = Meter.CreateUpDownCounter<long>(
        "ecommerce_pending_orders", "orders", "Number of pending orders");

    public static void RecordOrderProcessing(double durationMs, string orderType, 
        decimal orderValue, bool success, ILogger? logger = null)
    {
        OrderProcessingDuration.Record(durationMs,
            new KeyValuePair<string, object?>("order_type", orderType),
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("value_tier", GetValueTier(orderValue)));
        
        OrdersProcessed.Add(1,
            new KeyValuePair<string, object?>("order_type", orderType),
            new KeyValuePair<string, object?>("success", success));
    }

    public static void TrackPendingOrderChange(int delta)
    {
        PendingOrders.Add(delta);
    }

    private static string GetValueTier(decimal value) => value switch
    {
        < 50 => "low",
        < 200 => "medium", 
        < 500 => "high",
        _ => "premium"
    };
}

// OrderProcessingGAgent.cs
[GAgent]
public class OrderProcessingGAgent : GAgentBase<OrderState, OrderStateLogEvent>
{
    private readonly ILogger<OrderProcessingGAgent> _logger;
    private readonly IPaymentService _paymentService;

    public OrderProcessingGAgent(ILogger<OrderProcessingGAgent> logger, 
        IPaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }

    [EventHandler]
    public async Task HandleNewOrderEventAsync(NewOrderEvent orderEvent)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        // Track pending order increase
        OrderProcessingMetrics.TrackPendingOrderChange(1);

        try
        {
            await ProcessNewOrderAsync(orderEvent);
            success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order {OrderId}", orderEvent.OrderId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Track pending order decrease
            OrderProcessingMetrics.TrackPendingOrderChange(-1);
            
            // Record processing metrics
            OrderProcessingMetrics.RecordOrderProcessing(
                stopwatch.Elapsed.TotalMilliseconds,
                orderEvent.OrderType,
                orderEvent.TotalAmount,
                success,
                _logger);
        }
    }

    private async Task ProcessNewOrderAsync(NewOrderEvent orderEvent)
    {
        // Update state
        State.OrderId = orderEvent.OrderId;
        State.Status = OrderStatus.Processing;
        State.TotalAmount = orderEvent.TotalAmount;

        // Process payment with metrics
        await ProcessPaymentWithMetricsAsync(orderEvent);

        // Update final state
        State.Status = OrderStatus.Completed;
        await ConfirmEvents();
    }

    private async Task ProcessPaymentWithMetricsAsync(NewOrderEvent orderEvent)
    {
        var paymentStopwatch = Stopwatch.StartNew();
        var paymentSuccess = false;

        try
        {
            await _paymentService.ProcessPaymentAsync(orderEvent.PaymentInfo);
            paymentSuccess = true;
        }
        catch (PaymentException ex)
        {
            OrderProcessingMetrics.RecordPaymentFailure(ex.ErrorCode, orderEvent.OrderId);
            throw;
        }
        finally
        {
            paymentStopwatch.Stop();
            OrderProcessingMetrics.RecordPaymentProcessing(
                paymentStopwatch.Elapsed.TotalMilliseconds,
                orderEvent.PaymentInfo.Method,
                paymentSuccess,
                _logger);
        }
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("E-commerce Order Processing Agent");
    }
}
```

## Conclusion

This tutorial provides a comprehensive guide for implementing custom OpenTelemetry metrics in your GAgentBase applications. Remember to:

1. Follow the framework's established patterns
2. Use appropriate metric types for your use cases  
3. Include rich but low-cardinality tags
4. Handle exceptions properly
5. Test your metrics implementation
6. Monitor performance overhead

Your custom metrics will automatically integrate with any OpenTelemetry-compatible monitoring system when properly configured in your application startup. 