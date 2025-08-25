using Aevatar.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.HttpApi.Extensions;

/// <summary>
/// Apple attribution service extensions
/// Used to register Apple attribution related services
/// </summary>
public static class AppleAttributionServiceExtensions
{
    /// <summary>
    /// Add Apple attribution services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddAppleAttributionServices(this IServiceCollection services)
    {
        // Register Apple signature verification service
        services.AddScoped<IAppleSignatureVerificationService, AppleSignatureVerificationService>();
        
        return services;
    }
}
