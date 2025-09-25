namespace TracingPerformanceBenchmark.Models;

/// <summary>
/// Request model for creating orders in the benchmark.
/// </summary>
public class OrderRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
