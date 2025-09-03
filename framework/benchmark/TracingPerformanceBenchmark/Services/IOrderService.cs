using System.Threading.Tasks;
using TracingPerformanceBenchmark.Models;

namespace TracingPerformanceBenchmark.Services;

/// <summary>
/// Interface for order service operations in the benchmark.
/// </summary>
public interface IOrderService
{
    Task<OrderResult> CreateOrderAsync(OrderRequest request);
    Task<OrderResult> ProcessOrderAsync(string orderId);
    Task<OrderResult> GetOrderAsync(string orderId);
}
