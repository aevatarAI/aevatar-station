// ABOUTME: This file implements the TypeMetadataService as a stateless facade to TypeMetadataGrain
// ABOUTME: Provides assembly introspection and delegates storage/caching to Orleans grain for scalability

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Application.Grains;
using Aevatar.Application.Models;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Application.Services
{
    public class TypeMetadataService : ITypeMetadataService
    {
        private readonly ILogger<TypeMetadataService> _logger;
        private readonly IGrainFactory _grainFactory;

        public TypeMetadataService(
            ILogger<TypeMetadataService> logger,
            IGrainFactory grainFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        }

        public async Task<List<AgentTypeMetadata>> GetTypesByCapabilityAsync(string capability)
        {
            if (string.IsNullOrEmpty(capability))
            {
                return new List<AgentTypeMetadata>();
            }

            try
            {
                var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(0);
                return await grain.GetByCapabilityAsync(capability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get types by capability {Capability} from grain", capability);
                return new List<AgentTypeMetadata>();
            }
        }

        public async Task<AgentTypeMetadata> GetTypeMetadataAsync(string agentType)
        {
            if (string.IsNullOrEmpty(agentType))
            {
                return null;
            }

            try
            {
                var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(0);
                return await grain.GetByTypeAsync(agentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get type metadata for {AgentType} from grain", agentType);
                return null;
            }
        }

        public async Task<List<AgentTypeMetadata>> GetAllTypesAsync()
        {
            try
            {
                var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(0);
                return await grain.GetAllMetadataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all types from grain");
                return new List<AgentTypeMetadata>();
            }
        }

        public async Task RefreshMetadataAsync()
        {
            try
            {
                _logger.LogInformation("Starting assembly scan for GAgent types...");
                
                // Scan assemblies for GAgent types
                var scannedMetadata = ScanAssembliesForGAgentTypes();
                
                _logger.LogInformation("Found {TypeCount} GAgent types during assembly scan", scannedMetadata.Count);
                
                // Persist to grain
                var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(0);
                await grain.SetMetadataAsync(scannedMetadata);
                
                // Log size statistics
                var stats = await grain.GetStatsAsync();
                _logger.LogInformation(
                    "TypeMetadata refreshed: {TotalTypes} types, {SizeInBytes} bytes ({PercentageOf16MB:F2}% of 16MB limit)",
                    stats.TotalTypes, stats.SizeInBytes, stats.PercentageOf16MB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh metadata");
                throw;
            }
        }

        private List<AgentTypeMetadata> ScanAssembliesForGAgentTypes()
        {
            var scannedMetadata = new List<AgentTypeMetadata>();
            
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !IsSystemAssembly(a))
                    .ToList();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var gAgentTypes = assembly.GetTypes()
                            .Where(type => type.IsClass && !type.IsAbstract && HasGAgentAttribute(type))
                            .ToList();

                        foreach (var type in gAgentTypes)
                        {
                            var metadata = ExtractTypeMetadata(type);
                            if (metadata != null)
                            {
                                scannedMetadata.Add(metadata);
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        _logger.LogWarning(ex, "Failed to load types from assembly {AssemblyName}", assembly.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scanning assembly {AssemblyName}", assembly.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during assembly scanning");
            }
            
            return scannedMetadata;
        }

        private bool IsSystemAssembly(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            return assemblyName != null && (
                assemblyName.StartsWith("System") ||
                assemblyName.StartsWith("Microsoft") ||
                assemblyName.StartsWith("mscorlib") ||
                assemblyName.StartsWith("netstandard") ||
                assemblyName.StartsWith("Orleans") ||
                assemblyName.StartsWith("Volo.Abp") ||
                assemblyName.StartsWith("AutoMapper") ||
                assemblyName.StartsWith("Newtonsoft") ||
                assemblyName.StartsWith("Serilog") ||
                assemblyName.StartsWith("MongoDB") ||
                assemblyName.StartsWith("Elasticsearch") ||
                assemblyName.StartsWith("StackExchange") ||
                assemblyName.StartsWith("Polly") ||
                assemblyName.StartsWith("FluentValidation") ||
                assemblyName.StartsWith("Swashbuckle") ||
                assemblyName.StartsWith("Hangfire") ||
                assemblyName.StartsWith("MediatR") ||
                assemblyName.StartsWith("Autofac") ||
                assemblyName.StartsWith("Castle") ||
                assemblyName.StartsWith("Moq") ||
                assemblyName.StartsWith("xunit") ||
                assemblyName.StartsWith("Shouldly") ||
                assemblyName.StartsWith("NUnit") ||
                assemblyName.StartsWith("MSTest") ||
                assemblyName.StartsWith("testhost") ||
                assemblyName.StartsWith("vstest") ||
                assemblyName.StartsWith("Microsoft.TestPlatform") ||
                assemblyName.StartsWith("Microsoft.VisualStudio")
            );
        }

        private bool HasGAgentAttribute(Type type)
        {
            try
            {
                return type.GetCustomAttribute<GAgentAttribute>() != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private AgentTypeMetadata ExtractTypeMetadata(Type type)
        {
            try
            {
                var metadata = new AgentTypeMetadata
                {
                    AgentType = GetAgentTypeName(type),
                    AssemblyVersion = type.Assembly.GetName().Version?.ToString() ?? "Unknown",
                    DeploymentId = Environment.MachineName + "_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"),
                    GrainInterface = FindGrainInterface(type),
                    Description = GetTypeDescription(type),
                    Capabilities = ExtractCapabilities(type),
                    InterfaceVersions = ExtractInterfaceVersions(type)
                };

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting metadata for type {TypeName}", type.FullName);
                return null;
            }
        }

        private string GetAgentTypeName(Type type)
        {
            var gAgentAttr = type.GetCustomAttribute<GAgentAttribute>();
            if (gAgentAttr != null)
            {
                // Use the pattern from GAgentAttribute to generate the name
                return type.Name; // Simplified for now
            }
            
            return type.Name;
        }

        private Type FindGrainInterface(Type type)
        {
            // Find Orleans grain interfaces
            return type.GetInterfaces()
                .FirstOrDefault(i => i.Name.Contains("Grain") || i.Name.StartsWith("I" + type.Name.Replace("GAgent", "")));
        }

        private string GetTypeDescription(Type type)
        {
            // Try to get description from attributes or XML documentation
            return $"GAgent implementation: {type.Name}";
        }

        private List<string> ExtractCapabilities(Type type)
        {
            var capabilities = new List<string>();
            
            try
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => IsEventHandlerMethod(m))
                    .ToList();

                foreach (var method in methods)
                {
                    var capability = GetCapabilityFromMethod(method);
                    if (!string.IsNullOrEmpty(capability))
                    {
                        capabilities.Add(capability);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting capabilities for type {TypeName}", type.FullName);
            }

            return capabilities;
        }

        private bool IsEventHandlerMethod(MethodInfo method)
        {
            try
            {
                // Check for EventHandler attribute
                if (method.GetCustomAttribute<EventHandlerAttribute>() != null)
                {
                    return true;
                }

                // Check for default naming convention
                if (method.Name == "HandleEventAsync" && method.GetParameters().Length == 1)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GetCapabilityFromMethod(MethodInfo method)
        {
            try
            {
                // Convert method name to capability name
                var methodName = method.Name;
                
                // Remove common suffixes
                if (methodName.EndsWith("Async"))
                {
                    methodName = methodName.Substring(0, methodName.Length - 5);
                }
                
                if (methodName.EndsWith("Handler"))
                {
                    methodName = methodName.Substring(0, methodName.Length - 7);
                }

                // Handle default handler
                if (methodName == "HandleEvent")
                {
                    var parameter = method.GetParameters().FirstOrDefault();
                    if (parameter != null)
                    {
                        var paramTypeName = parameter.ParameterType.Name;
                        if (paramTypeName.EndsWith("Event"))
                        {
                            paramTypeName = paramTypeName.Substring(0, paramTypeName.Length - 5);
                        }
                        return paramTypeName;
                    }
                }

                return methodName;
            }
            catch (Exception)
            {
                return method.Name;
            }
        }

        private List<string> ExtractInterfaceVersions(Type type)
        {
            var versions = new List<string>();
            
            try
            {
                // Look for version attributes on interfaces
                var interfaces = type.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    // This is a placeholder - in a real implementation you would look for version attributes
                    versions.Add("1.0");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting interface versions for type {TypeName}", type.FullName);
            }

            return versions;
        }

        
        /// <summary>
        /// Gets statistics about the metadata storage including size and capacity usage.
        /// </summary>
        /// <returns>Metadata statistics from the grain</returns>
        public async Task<MetadataStats> GetStatsAsync()
        {
            try
            {
                var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(0);
                return await grain.GetStatsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metadata statistics from grain");
                return new MetadataStats
                {
                    TotalTypes = 0,
                    SizeInBytes = 0,
                    PercentageOf16MB = 0
                };
            }
        }
    }
}