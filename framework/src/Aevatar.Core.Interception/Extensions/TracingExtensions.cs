using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Aevatar.Core.Interception.Filters;
using Aevatar.Core.Interception.Middleware;
using Aevatar.Core.Interception.Services;

namespace Aevatar.Core.Interception.Extensions;

/// <summary>
/// Extension methods for registering tracing components in ASP.NET Core and Orleans applications
/// </summary>
public static class TracingExtensions
{
    /// <summary>
    /// Adds trace context middleware to the ASP.NET Core application pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseTraceContext(this IApplicationBuilder app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        return app.UseMiddleware<TraceContextMiddleware>();
    }

    /// <summary>
    /// Adds trace context services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTraceContextServices(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register trace management service
        services.AddTransient<ITraceManager, TraceManager>();

        return services;
    }

    /// <summary>
    /// Adds Orleans tracing filters to the silo configuration
    /// </summary>
    /// <param name="siloBuilder">The silo builder</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddTraceContextFilters(this ISiloBuilder siloBuilder)
    {
        if (siloBuilder == null)
            throw new ArgumentNullException(nameof(siloBuilder));

        // Add incoming grain call filter for reading trace ID from RequestContext
        siloBuilder.AddIncomingGrainCallFilter<TraceIncomingGrainCallFilter>();
        
        // Add outgoing grain call filter for setting trace ID in RequestContext
        siloBuilder.AddOutgoingGrainCallFilter<TraceOutgoingGrainCallFilter>();

        return siloBuilder;
    }

    /// <summary>
    /// Adds Orleans tracing filters to the client configuration
    /// </summary>
    /// <param name="clientBuilder">The client builder</param>
    /// <returns>The client builder for chaining</returns>
    public static IClientBuilder AddTraceContextFilters(this IClientBuilder clientBuilder)
    {
        if (clientBuilder == null)
            throw new ArgumentNullException(nameof(clientBuilder));

        // Add outgoing grain call filter for client-side calls
        clientBuilder.AddOutgoingGrainCallFilter<TraceOutgoingGrainCallFilter>();

        return clientBuilder;
    }

    /// <summary>
    /// Adds comprehensive tracing support including middleware, services, and Orleans filters
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddComprehensiveTracing(this IServiceCollection services)
    {
        return services.AddTraceContextServices();
    }

    /// <summary>
    /// Configures comprehensive tracing for Orleans silo including filters
    /// </summary>
    /// <param name="siloBuilder">The silo builder</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddComprehensiveTracing(this ISiloBuilder siloBuilder)
    {
        return siloBuilder.AddTraceContextFilters();
    }

    /// <summary>
    /// Configures comprehensive tracing for Orleans client including filters
    /// </summary>
    /// <param name="clientBuilder">The client builder</param>
    /// <returns>The client builder for chaining</returns>
    public static IClientBuilder AddComprehensiveTracing(this IClientBuilder clientBuilder)
    {
        return clientBuilder.AddTraceContextFilters();
    }
}
