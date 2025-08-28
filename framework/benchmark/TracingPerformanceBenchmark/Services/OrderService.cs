using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TracingPerformanceBenchmark.Models;

namespace TracingPerformanceBenchmark.Services;

/// <summary>
/// Base order service implementation for benchmarking.
/// </summary>
public class OrderService : IOrderService
{
    private readonly Dictionary<string, OrderResult> _orders = new();
    private int _orderCounter = 0;

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
