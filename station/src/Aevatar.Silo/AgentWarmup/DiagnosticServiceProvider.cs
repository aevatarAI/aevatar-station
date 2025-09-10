using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Diagnostic wrapper for IServiceProvider to track service resolution during disposal
/// </summary>
public class DiagnosticServiceProvider : IServiceProvider, IDisposable
{
    private readonly IServiceProvider _inner;
    private readonly ILogger<DiagnosticServiceProvider> _logger;
    private volatile bool _disposed = false;

    public DiagnosticServiceProvider(IServiceProvider inner, ILogger<DiagnosticServiceProvider> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public object? GetService(Type serviceType)
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempting to resolve service {ServiceType} from disposed provider. " +
                             "Stack trace: {StackTrace}", 
                             serviceType.Name, 
                             Environment.StackTrace);
            return null;
        }

        try
        {
            _logger.LogDebug("Resolving service: {ServiceType}", serviceType.Name);
            var service = _inner.GetService(serviceType);
            if (service != null)
            {
                _logger.LogDebug("Successfully resolved service: {ServiceType} -> {ImplementationType}", 
                               serviceType.Name, service.GetType().Name);
            }
            return service;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "ObjectDisposedException while resolving {ServiceType}. " +
                               "This indicates the DI container was disposed during service resolution. " +
                               "Stack trace: {StackTrace}",
                               serviceType.Name, Environment.StackTrace);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while resolving service {ServiceType}: {Message}", 
                           serviceType.Name, ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("DiagnosticServiceProvider disposing");
        _disposed = true;
        
        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// Extension methods for diagnostic service provider
/// </summary>
public static class DiagnosticServiceProviderExtensions
{
    public static T? GetService<T>(this DiagnosticServiceProvider provider) where T : class
    {
        return provider.GetService(typeof(T)) as T;
    }

    public static T GetRequiredService<T>(this DiagnosticServiceProvider provider) where T : class
    {
        var service = provider.GetService<T>();
        if (service == null)
        {
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }
        return service;
    }
} 