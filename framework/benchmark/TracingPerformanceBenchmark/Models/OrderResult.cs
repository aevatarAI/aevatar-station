using System;
namespace TracingPerformanceBenchmark.Models;

/// <summary>
/// Result model for order operations in the benchmark.
/// </summary>
public class OrderResult
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
