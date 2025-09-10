using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TracingPerformanceBenchmark.Models;
using Aevatar.Core.Interception.Attributes;

namespace TracingPerformanceBenchmark.Services;

/// <summary>
/// Order service implementation that will be processed by Fody weavers.
/// This service uses FodyTraceAttribute and will have tracing code injected at build time.
/// </summary>
public class FodyOrderService : IOrderService
{
    private readonly Dictionary<string, OrderResult> _orders = new();
    private int _orderCounter = 0;

    [FodyTrace(OperationName = "CreateOrder", CaptureParameters = true, CaptureReturnValue = true)]
    public async Task<OrderResult> CreateOrderAsync(OrderRequest request)
    {
        // Simulate some work
        await Task.Delay(1);
        
        var orderId = $"order_{++_orderCounter:D6}";
        var result = new OrderResult
        {
            OrderId = orderId,
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            TotalPrice = request.UnitPrice * request.Quantity,
            CreatedAt = DateTime.UtcNow,
            Status = "Created"
        };
        
        _orders[orderId] = result;
        return result;
    }

    [FodyTrace(OperationName = "ProcessOrder", CaptureParameters = true, CaptureReturnValue = true)]
    public async Task<OrderResult> ProcessOrderAsync(string orderId)
    {
        // Simulate some work
        await Task.Delay(1);
        
        if (!_orders.TryGetValue(orderId, out var order))
        {
            throw new ArgumentException($"Order {orderId} not found");
        }
        
        order.Status = "Processed";
        _orders[orderId] = order;
        return order;
    }

    [FodyTrace(OperationName = "GetOrder", CaptureParameters = true, CaptureReturnValue = false)]
    public async Task<OrderResult> GetOrderAsync(string orderId)
    {
        // Simulate some work
        await Task.Delay(1);
        
        if (!_orders.TryGetValue(orderId, out var order))
        {
            throw new ArgumentException($"Order {orderId} not found");
        }
        
        return order;
    }
}
