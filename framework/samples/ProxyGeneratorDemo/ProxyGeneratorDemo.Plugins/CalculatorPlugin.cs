// ABOUTME: Plugin implementation for calculator service without Orleans dependencies
// ABOUTME: Shows how stateless services can be implemented as plugins

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions.Plugin;

namespace ProxyGeneratorDemo.Plugins;

/// <summary>
/// Calculator service plugin - pure business logic, no Orleans
/// </summary>
[AgentPlugin("Calculator", "1.0.0")]
public class CalculatorPlugin : AgentPluginBase
{
    private int _calculationCount = 0;

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger?.LogInformation("Calculator plugin initialized");
        return Task.CompletedTask;
    }

    [AgentMethod("AddAsync", IsReadOnly = true)]
    public async Task<double> AddAsync(double a, double b)
    {
        var result = a + b;
        Logger?.LogDebug("Add: {A} + {B} = {Result}", a, b, result);
        await Task.CompletedTask;
        return result;
    }

    [AgentMethod("SubtractAsync", IsReadOnly = true)]
    public async Task<double> SubtractAsync(double a, double b)
    {
        var result = a - b;
        Logger?.LogDebug("Subtract: {A} - {B} = {Result}", a, b, result);
        await Task.CompletedTask;
        return result;
    }

    [AgentMethod("MultiplyAsync", IsReadOnly = true)]
    public async Task<double> MultiplyAsync(double a, double b)
    {
        var result = a * b;
        Logger?.LogDebug("Multiply: {A} * {B} = {Result}", a, b, result);
        await Task.CompletedTask;
        return result;
    }

    [AgentMethod("DivideAsync", IsReadOnly = true)]
    public async Task<double> DivideAsync(double a, double b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero");
        }
        
        var result = a / b;
        Logger?.LogDebug("Divide: {A} / {B} = {Result}", a, b, result);
        await Task.CompletedTask;
        return result;
    }

    [AgentMethod("PowerAsync", IsReadOnly = true)]
    public async Task<double> PowerAsync(double baseNumber, double exponent)
    {
        var result = Math.Pow(baseNumber, exponent);
        Logger?.LogDebug("Power: {Base} ^ {Exponent} = {Result}", baseNumber, exponent, result);
        await Task.CompletedTask;
        return result;
    }

    [AgentMethod("LogCalculationAsync", OneWay = true)]
    public async Task LogCalculationAsync(string operation, double result)
    {
        // Fire and forget logging
        Interlocked.Increment(ref _calculationCount);
        Logger?.LogInformation("Calculation logged: {Operation} = {Result} (Total calculations: {Count})", 
            operation, result, _calculationCount);
        
        // Publish calculation event
        await PublishEventAsync("CalculationPerformed", new 
        { 
            Operation = operation, 
            Result = result, 
            Timestamp = DateTime.UtcNow,
            TotalCalculations = _calculationCount
        });
    }

    [AgentMethod("ComplexCalculationAsync", AlwaysInterleave = true)]
    public async Task<double> ComplexCalculationAsync(double[] numbers, string operation)
    {
        Logger?.LogInformation("Starting complex calculation: {Operation} on {Count} numbers", 
            operation, numbers.Length);
        
        // Simulate complex calculation that takes time
        await Task.Delay(500);
        
        double result = operation.ToLower() switch
        {
            "sum" => numbers.Sum(),
            "average" => numbers.Average(),
            "min" => numbers.Min(),
            "max" => numbers.Max(),
            "product" => numbers.Aggregate(1.0, (acc, x) => acc * x),
            "variance" => CalculateVariance(numbers),
            _ => throw new NotSupportedException($"Operation '{operation}' is not supported")
        };
        
        Logger?.LogInformation("Complex calculation completed: {Operation} = {Result}", operation, result);
        await LogCalculationAsync($"Complex {operation}", result);
        
        return result;
    }

    private double CalculateVariance(double[] numbers)
    {
        if (numbers.Length == 0) return 0;
        
        double mean = numbers.Average();
        double sumOfSquares = numbers.Sum(x => Math.Pow(x - mean, 2));
        return sumOfSquares / numbers.Length;
    }
}