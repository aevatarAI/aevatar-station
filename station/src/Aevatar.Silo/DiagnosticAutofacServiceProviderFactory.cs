using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Linq;

namespace Aevatar.Silo;

/// <summary>
/// Custom Autofac service provider factory with diagnostic logging
/// </summary>
public class DiagnosticAutofacServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
{
    private readonly AutofacServiceProviderFactory _inner;

    public DiagnosticAutofacServiceProviderFactory()
    {
        _inner = new AutofacServiceProviderFactory(ConfigureContainer);
    }

    public ContainerBuilder CreateBuilder(IServiceCollection services)
    {
        Log.Information("Creating Autofac container builder with {ServiceCount} services", services.Count);
        return _inner.CreateBuilder(services);
    }

    public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
    {
        Log.Information("Building Autofac container...");
        
        var serviceProvider = _inner.CreateServiceProvider(containerBuilder);
        
        Log.Information("Autofac container built successfully");
        
        // Wrap in diagnostic provider
        return new DiagnosticServiceProviderWrapper(serviceProvider);
    }

    private static void ConfigureContainer(ContainerBuilder builder)
    {
        // Enable diagnostic tracing to see what's being registered
        builder.ComponentRegistryBuilder.Registered += (sender, e) =>
        {
            var serviceNames = string.Join(", ", e.ComponentRegistration.Services.Select(s => s.Description));
            Log.Debug("Autofac: Registered services [{Services}] with {Activator}", 
                serviceNames,
                e.ComponentRegistration.Activator.GetType().Name);
        };
    }
}

/// <summary>
/// Wrapper for IServiceProvider that provides diagnostic logging
/// </summary>
public class DiagnosticServiceProviderWrapper : IServiceProvider, IDisposable
{
    private readonly IServiceProvider _inner;
    private volatile bool _disposed = false;

    public DiagnosticServiceProviderWrapper(IServiceProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public object? GetService(Type serviceType)
    {
        if (_disposed)
        {
            Log.Warning("Attempting to resolve service {ServiceType} from disposed provider!", serviceType.Name);
            Log.Warning("Stack trace: {StackTrace}", Environment.StackTrace);
            return null;
        }

        try
        {
            Log.Debug("Resolving service: {ServiceType}", serviceType.Name);
            var service = _inner.GetService(serviceType);
            
            if (service != null)
            {
                Log.Debug("Successfully resolved {ServiceType} -> {ImplementationType}", 
                         serviceType.Name, service.GetType().Name);
            }
            else
            {
                Log.Debug("Service {ServiceType} not found", serviceType.Name);
            }
            
            return service;
        }
        catch (ObjectDisposedException ex)
        {
            Log.Error(ex, "ObjectDisposedException while resolving {ServiceType}! " +
                         "This indicates the DI container was disposed during service resolution.", 
                         serviceType.Name);
            Log.Error("Full stack trace: {StackTrace}", Environment.StackTrace);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception while resolving service {ServiceType}: {Message}", 
                     serviceType.Name, ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        Log.Information("DiagnosticServiceProviderWrapper disposing...");
        _disposed = true;
        
        if (_inner is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
                Log.Information("Inner service provider disposed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error disposing inner service provider");
            }
        }
    }
} 