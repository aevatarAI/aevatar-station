using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Interception;

namespace Aevatar.Core.Tests.Interception.Infrastructure
{
    /// <summary>
    /// Base class for interception tests that provides DI-based logger setup.
    /// This ensures all interceptors (static, extension, instance methods) use the same mock logger.
    /// </summary>
    public abstract class InterceptionTestBase : IDisposable
    {
        protected readonly TestMockLoggerProvider MockLoggerProvider;
        protected readonly IServiceProvider ServiceProvider;
        private readonly ServiceCollection _services;

        protected InterceptionTestBase()
        {
            // Create service collection and register our mock logger provider
            _services = new ServiceCollection();
            MockLoggerProvider = new TestMockLoggerProvider();
            
            // Register the mock logger provider for all logger types
            _services.AddSingleton<ILoggerProvider>(MockLoggerProvider);
            _services.AddSingleton<ILoggerFactory>(provider => 
            {
                var factory = new LoggerFactory();
                factory.AddProvider(MockLoggerProvider);
                return factory;
            });
            
            // Register generic ILogger<T> factory
            _services.AddTransient(typeof(ILogger<>), typeof(Logger<>));
            
            // Register general ILogger
            _services.AddTransient<ILogger>(provider => 
                provider.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultLogger"));

            // Build service provider
            ServiceProvider = _services.BuildServiceProvider();
            
            // Set the static service provider for interceptor DI resolution
            InterceptorAttribute.ServiceProvider = ServiceProvider;
        }

        public virtual void Dispose()
        {
            // Clean up static service provider
            InterceptorAttribute.ServiceProvider = null;
            MockLoggerProvider?.Dispose();
            
            // Dispose service provider if it implements IDisposable
            if (ServiceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }
        }
    }
}
