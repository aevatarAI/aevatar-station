using System;
using System.Threading.Tasks;
using Aevatar.Core.Interception.Attributes;
using Aevatar.Core.Interception.Context;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Instrumentation.Runtime;

namespace FodyTracingDemo;

/// <summary>
/// Sample service demonstrating Fody-based method tracing.
/// </summary>
public class SampleService
{
    /// <summary>
    /// Synchronous method with tracing enabled.
    /// </summary>
    [FodyTrace(OperationName = "SampleService.SyncMethod")]
    public string SyncMethod(string input)
    {
        Console.WriteLine($"Processing input: {input}");
        return $"Processed: {input}";
    }

    /// <summary>
    /// Asynchronous method with tracing enabled.
    /// </summary>
    [FodyTrace(OperationName = "SampleService.AsyncMethod")]
    public async Task<string> AsyncMethod(string input)
    {
        await Task.Delay(100); // Simulate async work
        Console.WriteLine($"Processing input asynchronously: {input}");
        return $"Async processed: {input}";
    }

    /// <summary>
    /// Method with parameter capture enabled.
    /// </summary>
    [FodyTrace(OperationName = "SampleService.MethodWithParams")]
    public string MethodWithParams(string input, int count)
    {
        Console.WriteLine($"Processing input: {input}, count: {count}");
        return $"Processed: {input} x{count}";
    }

    /// <summary>
    /// Method with return value capture enabled.
    /// </summary>
    [FodyTrace(OperationName = "SampleService.MethodWithReturnValue")]
    public int MethodWithReturnValue(int a, int b)
    {
        var result = a + b;
        Console.WriteLine($"Calculating {a} + {b} = {result}");
        return result;
    }

    /// <summary>
    /// Method that throws an exception to test error handling.
    /// </summary>
    [FodyTrace(OperationName = "SampleService.MethodWithException")]
    public string MethodWithException(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }
        return $"Processed: {input}";
    }
}

/// <summary>
/// Main program demonstrating the Fody weaver.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Fody Method Tracing Demo ===");
        Console.WriteLine();

        // Configure OpenTelemetry
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FodyTracingDemo"))
            .AddSource("Aevatar.Core.Fody")
            .Build();

        var service = new SampleService();

        try
        {
            // Test synchronous method
            Console.WriteLine("Testing synchronous method...");
            var result1 = service.SyncMethod("Hello World");
            Console.WriteLine($"Result: {result1}");
            Console.WriteLine();

            // Test asynchronous method
            Console.WriteLine("Testing asynchronous method...");
            var result2 = await service.AsyncMethod("Async Hello");
            Console.WriteLine($"Result: {result2}");
            Console.WriteLine();

            // Test method with parameters
            Console.WriteLine("Testing method with parameters...");
            var result3 = service.MethodWithParams("Test", 3);
            Console.WriteLine($"Result: {result3}");
            Console.WriteLine();

            // Test method with return value
            Console.WriteLine("Testing method with return value...");
            var result4 = service.MethodWithReturnValue(5, 7);
            Console.WriteLine($"Result: {result4}");
            Console.WriteLine();

            // Test exception handling
            Console.WriteLine("Testing exception handling...");
            try
            {
                service.MethodWithException("");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Caught expected exception: {ex.Message}");
            }
            Console.WriteLine();

            Console.WriteLine("All tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during testing: {ex}");
        }
    }
}
