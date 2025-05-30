using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Aevatar.Core.Abstractions.Plugin;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Aevatar.Core.Plugin;

/// <summary>
/// Routes Orleans grain method calls to plugin methods with attribute mapping
/// </summary>
public class OrleansMethodRouter
{
    private readonly ILogger<OrleansMethodRouter> _logger;
    private readonly ConcurrentDictionary<string, MethodRoutingInfo> _routingCache = new();
    private readonly ConcurrentDictionary<string, Type> _generatedTypes = new();

    public OrleansMethodRouter(ILogger<OrleansMethodRouter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register plugin methods for routing
    /// </summary>
    public void RegisterPlugin(IAgentPlugin plugin)
    {
        var pluginType = plugin.GetType();
        var methods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var agentMethodAttr = method.GetCustomAttribute<AgentMethodAttribute>();
            if (agentMethodAttr != null)
            {
                var routingInfo = CreateRoutingInfo(method, agentMethodAttr);
                var methodName = agentMethodAttr.MethodName ?? method.Name;
                
                // Register under the AgentMethod name (or actual method name if no AgentMethod name specified)
                _routingCache[methodName] = routingInfo;
                _logger.LogDebug("Registered plugin method: {MethodName} with routing info", methodName);
                
                // Also register under the actual method name if it's different from the AgentMethod name
                // This allows interface method calls to be properly routed
                if (!string.IsNullOrEmpty(agentMethodAttr.MethodName) && agentMethodAttr.MethodName != method.Name)
                {
                    _routingCache[method.Name] = routingInfo;
                    _logger.LogDebug("Also registered plugin method under actual name: {ActualMethodName}", method.Name);
                }
            }
        }
    }

    /// <summary>
    /// Route a method call to the plugin
    /// </summary>
    public async Task<object?> RouteMethodCallAsync(IAgentPlugin plugin, string methodName, object?[] parameters)
    {
        if (!_routingCache.TryGetValue(methodName, out var routingInfo))
        {
            throw new InvalidOperationException($"Method '{methodName}' not found or not exposed");
        }

        try
        {
            // Apply Orleans attribute behavior
            using var scope = CreateOrleansAttributeScope(routingInfo);
            
            // Convert parameters if needed
            var convertedParameters = ConvertParameters(routingInfo.MethodInfo, parameters);
            
            // Call the plugin method
            var result = await plugin.ExecuteMethodAsync(methodName, convertedParameters);
            
            _logger.LogDebug("Successfully routed method call: {MethodName}", methodName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing method call: {MethodName}", methodName);
            throw;
        }
    }

    /// <summary>
    /// Get method routing information
    /// </summary>
    public MethodRoutingInfo? GetRoutingInfo(string methodName)
    {
        return _routingCache.TryGetValue(methodName, out var info) ? info : null;
    }

    /// <summary>
    /// Check if method is read-only (for Orleans optimization)
    /// </summary>
    public bool IsReadOnly(string methodName)
    {
        return _routingCache.TryGetValue(methodName, out var info) && info.IsReadOnly;
    }

    /// <summary>
    /// Check if method always interleaves (for Orleans optimization)
    /// </summary>
    public bool AlwaysInterleave(string methodName)
    {
        return _routingCache.TryGetValue(methodName, out var info) && info.AlwaysInterleave;
    }

    /// <summary>
    /// Check if method is one-way (for Orleans optimization)
    /// </summary>
    public bool IsOneWay(string methodName)
    {
        return _routingCache.TryGetValue(methodName, out var info) && info.OneWay;
    }

    private MethodRoutingInfo CreateRoutingInfo(MethodInfo method, AgentMethodAttribute attribute)
    {
        return new MethodRoutingInfo
        {
            MethodInfo = method,
            MethodName = attribute.MethodName ?? method.Name,
            IsReadOnly = attribute.IsReadOnly,
            AlwaysInterleave = attribute.AlwaysInterleave,
            OneWay = attribute.OneWay,
            ParameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray(),
            ReturnType = method.ReturnType
        };
    }

    private object?[] ConvertParameters(MethodInfo method, object?[] parameters)
    {
        var parameterTypes = method.GetParameters();
        if (parameters.Length != parameterTypes.Length)
        {
            throw new ArgumentException($"Parameter count mismatch. Expected {parameterTypes.Length}, got {parameters.Length}");
        }

        var convertedParameters = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            convertedParameters[i] = ConvertParameter(parameters[i], parameterTypes[i].ParameterType);
        }

        return convertedParameters;
    }

    private object? ConvertParameter(object? parameter, Type targetType)
    {
        if (parameter == null)
        {
            return null;
        }

        var sourceType = parameter.GetType();
        if (targetType.IsAssignableFrom(sourceType))
        {
            return parameter;
        }

        // Try basic conversions
        try
        {
            return Convert.ChangeType(parameter, targetType);
        }
        catch
        {
            // Could add more sophisticated conversion logic here
            _logger.LogWarning("Could not convert parameter from {SourceType} to {TargetType}", sourceType, targetType);
            return parameter;
        }
    }

    private IDisposable CreateOrleansAttributeScope(MethodRoutingInfo routingInfo)
    {
        // This would create a scope that applies Orleans attribute behavior
        // For now, just return a dummy scope
        return new OrleansAttributeScope(routingInfo, _logger);
    }

    /// <summary>
    /// Get or create routing info for interface method
    /// </summary>
    private MethodRoutingInfo? GetOrCreateRoutingInfo(MethodInfo interfaceMethod, IAgentPlugin plugin)
    {
        // Try to find corresponding plugin method
        var pluginType = plugin.GetType();
        var pluginMethods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var pluginMethod in pluginMethods)
        {
            var agentMethodAttr = pluginMethod.GetCustomAttribute<AgentMethodAttribute>();
            if (agentMethodAttr != null)
            {
                var methodName = agentMethodAttr.MethodName ?? pluginMethod.Name;
                if (methodName == interfaceMethod.Name)
                {
                    // Always create routing info from interface method to ensure interface attributes are authoritative
                    var routingInfo = CreateRoutingInfoFromInterface(interfaceMethod, pluginMethod, agentMethodAttr);
                    
                    // Verify Orleans attribute compatibility and log if mismatched
                    if (!OrleansAttributeMapper.AreAttributesCompatible(agentMethodAttr, interfaceMethod))
                    {
                        _logger.LogWarning("Orleans attribute mismatch between plugin method {PluginMethod} and interface method {InterfaceMethod}. Using interface attributes as authoritative.", 
                            pluginMethod.Name, interfaceMethod.Name);
                    }
                    
                    return routingInfo;
                }
            }
        }

        _logger.LogWarning("No corresponding plugin method found for interface method: {MethodName}", interfaceMethod.Name);
        return null;
    }

    /// <summary>
    /// Create routing info from interface method, taking Orleans attributes into account
    /// </summary>
    private MethodRoutingInfo CreateRoutingInfoFromInterface(MethodInfo interfaceMethod, MethodInfo pluginMethod, AgentMethodAttribute agentMethodAttr)
    {
        var routingInfo = new MethodRoutingInfo
        {
            MethodInfo = pluginMethod,
            MethodName = interfaceMethod.Name,
            ParameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
            ReturnType = interfaceMethod.ReturnType
        };

        // Use Orleans attributes from interface as authoritative
        UpdateRoutingInfoFromInterface(routingInfo, interfaceMethod);

        return routingInfo;
    }

    /// <summary>
    /// Update routing info with Orleans attributes from interface method
    /// </summary>
    private void UpdateRoutingInfoFromInterface(MethodRoutingInfo routingInfo, MethodInfo interfaceMethod)
    {
        routingInfo.IsReadOnly = interfaceMethod.GetCustomAttribute<ReadOnlyAttribute>() != null;
        routingInfo.AlwaysInterleave = interfaceMethod.GetCustomAttribute<AlwaysInterleaveAttribute>() != null;
        routingInfo.OneWay = interfaceMethod.GetCustomAttribute<OneWayAttribute>() != null;
        
        _logger.LogTrace("Updated routing info for {MethodName}: ReadOnly={ReadOnly}, AlwaysInterleave={AlwaysInterleave}, OneWay={OneWay}",
            routingInfo.MethodName, routingInfo.IsReadOnly, routingInfo.AlwaysInterleave, routingInfo.OneWay);
    }

    /// <summary>
    /// Create method implementation with full Orleans attribute support
    /// </summary>
    private void CreateMethodImplementationWithAttributes(TypeBuilder typeBuilder, MethodRoutingInfo routingInfo, 
        FieldBuilder pluginField, FieldBuilder routerField, MethodInfo interfaceMethod)
    {
        _logger.LogTrace("Creating method implementation for: {MethodName} with Orleans attributes", routingInfo.MethodName);

        var methodBuilder = typeBuilder.DefineMethod(
            routingInfo.MethodName,
            MethodAttributes.Public | MethodAttributes.Virtual,
            routingInfo.ReturnType,
            routingInfo.ParameterTypes);

        // Apply Orleans attributes - this is the key advantage of Reflection.Emit
        ApplyOrleansAttributes(methodBuilder, routingInfo);

        // Generate method implementation IL
        var il = methodBuilder.GetILGenerator();
        GenerateMethodImplementationIL(il, routingInfo, pluginField, routerField);

        _logger.LogTrace("Successfully created method implementation for: {MethodName}", routingInfo.MethodName);
    }

    /// <summary>
    /// Generate IL for method implementation
    /// </summary>
    private void GenerateMethodImplementationIL(ILGenerator il, MethodRoutingInfo routingInfo, 
        FieldBuilder pluginField, FieldBuilder routerField)
    {
        // Declare local variables
        var parametersArray = il.DeclareLocal(typeof(object[]));
        
        try
        {
            // Create parameters array
            il.Emit(OpCodes.Ldc_I4, routingInfo.ParameterTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, parametersArray);

            // Fill parameters array
            for (int i = 0; i < routingInfo.ParameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, parametersArray);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, i + 1);
                
                // Box value types
                if (routingInfo.ParameterTypes[i].IsValueType)
                {
                    il.Emit(OpCodes.Box, routingInfo.ParameterTypes[i]);
                }
                
                il.Emit(OpCodes.Stelem_Ref);
            }

            // Load router field
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, routerField);

            // Load plugin field
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, pluginField);

            // Load method name
            il.Emit(OpCodes.Ldstr, routingInfo.MethodName);

            // Load parameters array
            il.Emit(OpCodes.Ldloc, parametersArray);

            // Call RouteMethodCallAsync
            var routeMethod = typeof(OrleansMethodRouter).GetMethod(nameof(OrleansMethodRouter.RouteMethodCallAsync))!;
            il.Emit(OpCodes.Callvirt, routeMethod);

            // Handle different return types
            HandleReturnType(il, routingInfo.ReturnType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating IL for method: {MethodName}", routingInfo.MethodName);
            throw new MethodGenerationException($"Failed to generate IL for method {routingInfo.MethodName}", ex);
        }
    }

    /// <summary>
    /// Handle different return types in IL generation
    /// </summary>
    private void HandleReturnType(ILGenerator il, Type returnType)
    {
        if (returnType == typeof(Task))
        {
            // For Task return type, just return the task
            il.Emit(OpCodes.Castclass, typeof(Task));
            il.Emit(OpCodes.Ret);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            // For Task<T> return type, we need to convert Task<object?> to Task<T>
            var genericArgument = returnType.GetGenericArguments()[0];
            var convertMethod = typeof(OrleansGrainProxyGenerator)
                .GetMethod("ConvertTaskResult", BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(genericArgument);

            il.Emit(OpCodes.Call, convertMethod);
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(void))
        {
            // For void methods, await the task and return
            var waitMethod = typeof(Task).GetMethod(nameof(Task.Wait), new Type[0])!;
            il.Emit(OpCodes.Castclass, typeof(Task));
            il.Emit(OpCodes.Callvirt, waitMethod);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            // For synchronous return types, get result from task
            var taskOfObjectType = typeof(Task<object>);
            var resultProperty = taskOfObjectType.GetProperty(nameof(Task<object>.Result))!;
            
            il.Emit(OpCodes.Castclass, taskOfObjectType);
            il.Emit(OpCodes.Callvirt, resultProperty.GetMethod!);
            
            // Cast to expected return type
            if (returnType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, returnType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, returnType);
            }
            
            il.Emit(OpCodes.Ret);
        }
    }

    /// <summary>
    /// Convert Task<object?> to Task<T> for generic task results
    /// </summary>
    public static async Task<T> ConvertTaskResult<T>(Task<object?> task)
    {
        var result = await task;
        if (result == null)
        {
            return default(T)!;
        }

        if (result is T directResult)
        {
            return directResult;
        }

        // Try type conversion
        try
        {
            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch
        {
            // Try JSON conversion as last resort
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
        }
    }

    /// <summary>
    /// Create grain method with proper Orleans attributes using Reflection.Emit
    /// </summary>
    public MethodInfo CreateGrainMethod(MethodRoutingInfo routingInfo, Type grainType)
    {
        try
        {
            _logger.LogDebug("Creating grain method: {MethodName} for type: {GrainType}", 
                routingInfo.MethodName, grainType.Name);

            var typeName = $"{grainType.Name}_{routingInfo.MethodName}_Proxy";
            
            // Check if we already generated this type
            if (_generatedTypes.TryGetValue(typeName, out var existingType))
            {
                return existingType.GetMethod(routingInfo.MethodName)!;
            }

            // Create dynamic assembly and module
            var assemblyName = new AssemblyName($"DynamicGrainProxy_{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            // Create type that does not implement the grain interface (standalone method)
            var typeBuilder = moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(object));

            // Add plugin field
            var pluginField = typeBuilder.DefineField("_plugin", typeof(IAgentPlugin), FieldAttributes.Private | FieldAttributes.InitOnly);
            var routerField = typeBuilder.DefineField("_router", typeof(OrleansMethodRouter), FieldAttributes.Private | FieldAttributes.InitOnly);

            // Create constructor
            CreateConstructor(typeBuilder, pluginField, routerField);

            // Create the method
            var methodBuilder = CreateMethodImplementation(typeBuilder, routingInfo, pluginField, routerField);

            // Apply Orleans attributes to the method
            ApplyOrleansAttributes(methodBuilder, routingInfo);

            // Create the type
            var generatedType = typeBuilder.CreateType()!;
            _generatedTypes[typeName] = generatedType;

            var generatedMethod = generatedType.GetMethod(routingInfo.MethodName)!;
            
            _logger.LogInformation("Successfully created grain method: {MethodName}", routingInfo.MethodName);
            return generatedMethod;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create grain method: {MethodName}", routingInfo.MethodName);
            throw new MethodGenerationException($"Failed to create method {routingInfo.MethodName}", ex);
        }
    }

    private void CreateConstructor(TypeBuilder typeBuilder, FieldBuilder pluginField, FieldBuilder routerField)
    {
        var constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            new[] { typeof(IAgentPlugin), typeof(OrleansMethodRouter) });

        var il = constructor.GetILGenerator();

        // Call base constructor
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);

        // Set plugin field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, pluginField);

        // Set router field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stfld, routerField);

        il.Emit(OpCodes.Ret);
    }

    private MethodBuilder CreateMethodImplementation(TypeBuilder typeBuilder, MethodRoutingInfo routingInfo, 
        FieldBuilder pluginField, FieldBuilder routerField)
    {
        var methodBuilder = typeBuilder.DefineMethod(
            routingInfo.MethodName,
            MethodAttributes.Public | MethodAttributes.Virtual,
            routingInfo.ReturnType,
            routingInfo.ParameterTypes);

        var il = methodBuilder.GetILGenerator();

        // Declare local variables
        var parametersArray = il.DeclareLocal(typeof(object[]));
        var result = il.DeclareLocal(typeof(object));

        // Create parameters array
        il.Emit(OpCodes.Ldc_I4, routingInfo.ParameterTypes.Length);
        il.Emit(OpCodes.Newarr, typeof(object));
        il.Emit(OpCodes.Stloc, parametersArray);

        // Fill parameters array
        for (int i = 0; i < routingInfo.ParameterTypes.Length; i++)
        {
            il.Emit(OpCodes.Ldloc, parametersArray);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 1);
            
            // Box value types
            if (routingInfo.ParameterTypes[i].IsValueType)
            {
                il.Emit(OpCodes.Box, routingInfo.ParameterTypes[i]);
            }
            
            il.Emit(OpCodes.Stelem_Ref);
        }

        // Load router field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, routerField);

        // Load plugin field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, pluginField);

        // Load method name
        il.Emit(OpCodes.Ldstr, routingInfo.MethodName);

        // Load parameters array
        il.Emit(OpCodes.Ldloc, parametersArray);

        // Call RouteMethodCallAsync
        var routeMethod = typeof(OrleansMethodRouter).GetMethod(nameof(OrleansMethodRouter.RouteMethodCallAsync))!;
        il.Emit(OpCodes.Callvirt, routeMethod);

        // Handle async return types
        if (routingInfo.ReturnType == typeof(Task))
        {
            // For Task return type, just return the task
            il.Emit(OpCodes.Castclass, typeof(Task));
            il.Emit(OpCodes.Ret);
        }
        else if (routingInfo.ReturnType.IsGenericType && routingInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            // For Task<T> return type, cast and return
            il.Emit(OpCodes.Castclass, routingInfo.ReturnType);
            il.Emit(OpCodes.Ret);
        }
        else if (routingInfo.ReturnType == typeof(void))
        {
            // For void methods, await the task and return
            var waitMethod = typeof(Task).GetMethod(nameof(Task.Wait), new[] { typeof(int) })!;
            il.Emit(OpCodes.Castclass, typeof(Task));
            il.Emit(OpCodes.Ldc_I4, -1); // Infinite timeout
            il.Emit(OpCodes.Callvirt, waitMethod);
            il.Emit(OpCodes.Pop); // Discard result
            il.Emit(OpCodes.Ret);
        }
        else
        {
            // For synchronous return types, get result from task
            var taskOfObjectType = typeof(Task<object>);
            var resultProperty = taskOfObjectType.GetProperty(nameof(Task<object>.Result))!;
            
            il.Emit(OpCodes.Castclass, taskOfObjectType);
            il.Emit(OpCodes.Callvirt, resultProperty.GetMethod!);
            
            // Cast to expected return type
            if (routingInfo.ReturnType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, routingInfo.ReturnType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, routingInfo.ReturnType);
            }
            
            il.Emit(OpCodes.Ret);
        }

        return methodBuilder;
    }

    private void ApplyOrleansAttributes(MethodBuilder methodBuilder, MethodRoutingInfo routingInfo)
    {
        // Apply ReadOnly attribute
        if (routingInfo.IsReadOnly)
        {
            var readOnlyAttr = new CustomAttributeBuilder(
                typeof(ReadOnlyAttribute).GetConstructor(Type.EmptyTypes)!,
                Array.Empty<object>());
            methodBuilder.SetCustomAttribute(readOnlyAttr);
        }

        // Apply AlwaysInterleave attribute
        if (routingInfo.AlwaysInterleave)
        {
            var interleaveAttr = new CustomAttributeBuilder(
                typeof(AlwaysInterleaveAttribute).GetConstructor(Type.EmptyTypes)!,
                Array.Empty<object>());
            methodBuilder.SetCustomAttribute(interleaveAttr);
        }

        // Apply OneWay attribute
        if (routingInfo.OneWay)
        {
            var oneWayAttr = new CustomAttributeBuilder(
                typeof(OneWayAttribute).GetConstructor(Type.EmptyTypes)!,
                Array.Empty<object>());
            methodBuilder.SetCustomAttribute(oneWayAttr);
        }

        // Apply AsyncStateMachine attribute for async methods
        if (routingInfo.ReturnType == typeof(Task) || 
            (routingInfo.ReturnType.IsGenericType && routingInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
        {
            var asyncAttr = new CustomAttributeBuilder(
                typeof(AsyncStateMachineAttribute).GetConstructor(new[] { typeof(Type) })!,
                new object[] { typeof(OrleansGrainProxyGenerator) });
            methodBuilder.SetCustomAttribute(asyncAttr);
        }
    }
}

/// <summary>
/// Dynamic proxy generator for Orleans grain interfaces with full attribute support
/// </summary>
public class OrleansGrainProxyGenerator
{
    private readonly OrleansMethodRouter _methodRouter;
    private readonly ILogger<OrleansGrainProxyGenerator> _logger;
    private readonly ProxyGenerator _proxyGenerator;
    private readonly ConcurrentDictionary<string, Type> _generatedTypes = new();

    public OrleansGrainProxyGenerator(OrleansMethodRouter methodRouter, ILogger<OrleansGrainProxyGenerator> logger)
    {
        _methodRouter = methodRouter ?? throw new ArgumentNullException(nameof(methodRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _proxyGenerator = new ProxyGenerator();
    }

    /// <summary>
    /// Generate Orleans grain implementation with full attribute support using Reflection.Emit
    /// This is the primary method for Orleans integration as it preserves actual Orleans attributes
    /// </summary>
    public TGrainInterface GenerateGrainImplementation<TGrainInterface>(IAgentPlugin plugin)
        where TGrainInterface : class
    {
        try
        {
            _logger.LogDebug("Generating grain implementation for interface: {InterfaceType} using Reflection.Emit", typeof(TGrainInterface).Name);

            // Register plugin methods with router first
            _methodRouter.RegisterPlugin(plugin);

            // Generate complete grain implementation using Reflection.Emit
            var grainType = CreateGrainImplementationType<TGrainInterface>(plugin);
            
            // Create instance of the generated type
            var grainInstance = Activator.CreateInstance(grainType, plugin, _methodRouter) as TGrainInterface;
            
            if (grainInstance == null)
            {
                throw new ProxyGenerationException($"Failed to create instance of generated type for {typeof(TGrainInterface).Name}");
            }

            _logger.LogInformation("Successfully generated grain implementation for: {InterfaceType} with full Orleans attributes", typeof(TGrainInterface).Name);
            return grainInstance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate grain implementation for: {InterfaceType}", typeof(TGrainInterface).Name);
            throw new ProxyGenerationException($"Failed to generate implementation for {typeof(TGrainInterface).Name}", ex);
        }
    }

    /// <summary>
    /// Generate Orleans grain interface that routes to plugin (fallback using Castle DynamicProxy)
    /// Note: This approach has limited Orleans attribute support compared to Reflection.Emit
    /// </summary>
    public TGrainInterface GenerateGrainProxy<TGrainInterface>(IAgentPlugin plugin)
        where TGrainInterface : class
    {
        try
        {
            _logger.LogDebug("Generating grain proxy for interface: {InterfaceType} using Castle DynamicProxy (fallback)", typeof(TGrainInterface).Name);
            _logger.LogWarning("Using Castle DynamicProxy for {InterfaceType}. For full Orleans attribute support, use GenerateGrainImplementation instead", typeof(TGrainInterface).Name);

            // Register plugin methods with router first
            _methodRouter.RegisterPlugin(plugin);

            // Create interceptor that routes calls to plugin
            var interceptor = new PluginGrainInterceptor<TGrainInterface>(plugin, _methodRouter, _logger);

            // Generate proxy using Castle DynamicProxy
            var proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget<TGrainInterface>(interceptor);

            _logger.LogInformation("Successfully generated grain proxy for: {InterfaceType}", typeof(TGrainInterface).Name);
            return proxy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate grain proxy for: {InterfaceType}", typeof(TGrainInterface).Name);
            throw new ProxyGenerationException($"Failed to generate proxy for {typeof(TGrainInterface).Name}", ex);
        }
    }

    /// <summary>
    /// Create complete grain implementation type using Reflection.Emit with full Orleans attribute support
    /// </summary>
    private Type CreateGrainImplementationType<TGrainInterface>(IAgentPlugin plugin)
        where TGrainInterface : class
    {
        var interfaceType = typeof(TGrainInterface);
        var typeName = $"{interfaceType.Name}_{plugin.GetType().Name}_GrainImpl_{Guid.NewGuid():N}";
        
        // Check if we already generated this type (cache by plugin type + interface type)
        var cacheKey = $"{plugin.GetType().FullName}_{interfaceType.FullName}";
        if (_generatedTypes.TryGetValue(cacheKey, out var existingType))
        {
            _logger.LogDebug("Using cached grain implementation type: {TypeName}", existingType.Name);
            return existingType;
        }

        _logger.LogDebug("Creating grain implementation type: {TypeName} for interface: {InterfaceType}", typeName, interfaceType.Name);

        // Create dynamic assembly and module
        var assemblyName = new AssemblyName($"DynamicGrainImpl_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // Create type that implements the grain interface
        var typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(object),
            new[] { interfaceType });

        // Add plugin and router fields
        var pluginField = typeBuilder.DefineField("_plugin", typeof(IAgentPlugin), FieldAttributes.Private | FieldAttributes.InitOnly);
        var routerField = typeBuilder.DefineField("_router", typeof(OrleansMethodRouter), FieldAttributes.Private | FieldAttributes.InitOnly);

        // Create constructor
        CreateConstructor(typeBuilder, pluginField, routerField);

        // Implement all interface methods with full Orleans attribute support
        var interfaceMethods = interfaceType.GetMethods();
        foreach (var interfaceMethod in interfaceMethods)
        {
            var routingInfo = GetOrCreateRoutingInfo(interfaceMethod, plugin);
            if (routingInfo != null)
            {
                CreateMethodImplementationWithAttributes(typeBuilder, routingInfo, pluginField, routerField, interfaceMethod);
            }
        }

        // Create the type
        var generatedType = typeBuilder.CreateType()!;
        _generatedTypes[cacheKey] = generatedType;

        _logger.LogInformation("Successfully created grain implementation type: {TypeName} with {MethodCount} methods", 
            typeName, interfaceMethods.Length);

        return generatedType;
    }

    /// <summary>
    /// Get or create routing info for interface method
    /// </summary>
    private MethodRoutingInfo? GetOrCreateRoutingInfo(MethodInfo interfaceMethod, IAgentPlugin plugin)
    {
        // Try to find corresponding plugin method
        var pluginType = plugin.GetType();
        var pluginMethods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var pluginMethod in pluginMethods)
        {
            var agentMethodAttr = pluginMethod.GetCustomAttribute<AgentMethodAttribute>();
            if (agentMethodAttr != null)
            {
                var methodName = agentMethodAttr.MethodName ?? pluginMethod.Name;
                if (methodName == interfaceMethod.Name)
                {
                    // Always create routing info from interface method to ensure interface attributes are authoritative
                    var routingInfo = CreateRoutingInfoFromInterface(interfaceMethod, pluginMethod, agentMethodAttr);
                    
                    // Verify Orleans attribute compatibility and log if mismatched
                    if (!OrleansAttributeMapper.AreAttributesCompatible(agentMethodAttr, interfaceMethod))
                    {
                        _logger.LogWarning("Orleans attribute mismatch between plugin method {PluginMethod} and interface method {InterfaceMethod}. Using interface attributes as authoritative.", 
                            pluginMethod.Name, interfaceMethod.Name);
                    }
                    
                    return routingInfo;
                }
            }
        }

        _logger.LogWarning("No corresponding plugin method found for interface method: {MethodName}", interfaceMethod.Name);
        return null;
    }

    /// <summary>
    /// Create routing info from interface method, taking Orleans attributes into account
    /// </summary>
    private MethodRoutingInfo CreateRoutingInfoFromInterface(MethodInfo interfaceMethod, MethodInfo pluginMethod, AgentMethodAttribute agentMethodAttr)
    {
        var routingInfo = new MethodRoutingInfo
        {
            MethodInfo = pluginMethod,
            MethodName = interfaceMethod.Name,
            ParameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
            ReturnType = interfaceMethod.ReturnType
        };

        // Use Orleans attributes from interface as authoritative
        UpdateRoutingInfoFromInterface(routingInfo, interfaceMethod);

        return routingInfo;
    }

    /// <summary>
    /// Update routing info with Orleans attributes from interface method
    /// </summary>
    private void UpdateRoutingInfoFromInterface(MethodRoutingInfo routingInfo, MethodInfo interfaceMethod)
    {
        routingInfo.IsReadOnly = interfaceMethod.GetCustomAttribute<ReadOnlyAttribute>() != null;
        routingInfo.AlwaysInterleave = interfaceMethod.GetCustomAttribute<AlwaysInterleaveAttribute>() != null;
        routingInfo.OneWay = interfaceMethod.GetCustomAttribute<OneWayAttribute>() != null;
        
        _logger.LogTrace("Updated routing info for {MethodName}: ReadOnly={ReadOnly}, AlwaysInterleave={AlwaysInterleave}, OneWay={OneWay}",
            routingInfo.MethodName, routingInfo.IsReadOnly, routingInfo.AlwaysInterleave, routingInfo.OneWay);
    }

    /// <summary>
    /// Create method implementation with full Orleans attribute support
    /// </summary>
    private void CreateMethodImplementationWithAttributes(TypeBuilder typeBuilder, MethodRoutingInfo routingInfo, 
        FieldBuilder pluginField, FieldBuilder routerField, MethodInfo interfaceMethod)
    {
        _logger.LogTrace("Creating method implementation for: {MethodName} with Orleans attributes", routingInfo.MethodName);

        var methodBuilder = typeBuilder.DefineMethod(
            routingInfo.MethodName,
            MethodAttributes.Public | MethodAttributes.Virtual,
            routingInfo.ReturnType,
            routingInfo.ParameterTypes);

        // Apply Orleans attributes - this is the key advantage of Reflection.Emit
        ApplyOrleansAttributes(methodBuilder, routingInfo);

        // Generate method implementation IL
        var il = methodBuilder.GetILGenerator();
        GenerateMethodImplementationIL(il, routingInfo, pluginField, routerField);

        _logger.LogTrace("Successfully created method implementation for: {MethodName}", routingInfo.MethodName);
    }

    /// <summary>
    /// Generate IL for method implementation
    /// </summary>
    private void GenerateMethodImplementationIL(ILGenerator il, MethodRoutingInfo routingInfo, 
        FieldBuilder pluginField, FieldBuilder routerField)
    {
        // Declare local variables
        var parametersArray = il.DeclareLocal(typeof(object[]));
        
        try
        {
            // Create parameters array
            il.Emit(OpCodes.Ldc_I4, routingInfo.ParameterTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, parametersArray);

            // Fill parameters array
            for (int i = 0; i < routingInfo.ParameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, parametersArray);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, i + 1);
                
                // Box value types
                if (routingInfo.ParameterTypes[i].IsValueType)
                {
                    il.Emit(OpCodes.Box, routingInfo.ParameterTypes[i]);
                }
                
                il.Emit(OpCodes.Stelem_Ref);
            }

            // Load router field
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, routerField);

            // Load plugin field
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, pluginField);

            // Load method name
            il.Emit(OpCodes.Ldstr, routingInfo.MethodName);

            // Load parameters array
            il.Emit(OpCodes.Ldloc, parametersArray);

            // Call RouteMethodCallAsync
            var routeMethod = typeof(OrleansMethodRouter).GetMethod(nameof(OrleansMethodRouter.RouteMethodCallAsync))!;
            il.Emit(OpCodes.Callvirt, routeMethod);

            // Handle different return types
            HandleReturnType(il, routingInfo.ReturnType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating IL for method: {MethodName}", routingInfo.MethodName);
            throw new MethodGenerationException($"Failed to generate IL for method {routingInfo.MethodName}", ex);
        }
    }

    /// <summary>
    /// Handle different return types in IL generation
    /// </summary>
    private void HandleReturnType(ILGenerator il, Type returnType)
    {
        if (returnType == typeof(Task))
        {
            // For Task return type, cast and return
            il.Emit(OpCodes.Castclass, typeof(Task));
            il.Emit(OpCodes.Ret);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            // For Task<T>, we need to convert Task<object?> to Task<T>
            var genericArgument = returnType.GetGenericArguments()[0];
            var convertMethod = typeof(OrleansGrainProxyGenerator)
                .GetMethod("ConvertTaskResult", BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(genericArgument);
            
            il.Emit(OpCodes.Call, convertMethod);
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(void))
        {
            // For void methods, await the task
            var waitMethod = typeof(Task).GetMethod(nameof(Task.Wait), Type.EmptyTypes)!;
            il.Emit(OpCodes.Castclass, typeof(Task));
            il.Emit(OpCodes.Callvirt, waitMethod);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            // For synchronous return types, get result from task
            var taskOfObjectType = typeof(Task<object>);
            var resultProperty = taskOfObjectType.GetProperty(nameof(Task<object>.Result))!;
            
            il.Emit(OpCodes.Castclass, taskOfObjectType);
            il.Emit(OpCodes.Callvirt, resultProperty.GetMethod!);
            
            // Cast to expected return type
            if (returnType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, returnType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, returnType);
            }
            
            il.Emit(OpCodes.Ret);
        }
    }

    /// <summary>
    /// Convert Task<object?> to Task<T> for generic task results
    /// </summary>
    public static async Task<T> ConvertTaskResult<T>(Task<object?> task)
    {
        var result = await task;
        if (result == null)
        {
            return default(T)!;
        }

        if (result is T directResult)
        {
            return directResult;
        }

        // Try type conversion
        try
        {
            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch
        {
            // Try JSON conversion as last resort
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
        }
    }

    /// <summary>
    /// Create grain method with proper Orleans attributes using Reflection.Emit
    /// </summary>
    public MethodInfo CreateGrainMethod(MethodRoutingInfo routingInfo, Type grainType)
    {
        try
        {
            _logger.LogDebug("Creating grain method: {MethodName} for type: {GrainType}", 
                routingInfo.MethodName, grainType.Name);

            var typeName = $"{grainType.Name}_{routingInfo.MethodName}_Proxy";
            
            // Check if we already generated this type
            if (_generatedTypes.TryGetValue(typeName, out var existingType))
            {
                return existingType.GetMethod(routingInfo.MethodName)!;
            }

            // Create dynamic assembly and module
            var assemblyName = new AssemblyName($"DynamicGrainProxy_{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            // Create type that does not implement the grain interface (standalone method)
            var typeBuilder = moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(object));

            // Add plugin field
            var pluginField = typeBuilder.DefineField("_plugin", typeof(IAgentPlugin), FieldAttributes.Private | FieldAttributes.InitOnly);
            var routerField = typeBuilder.DefineField("_router", typeof(OrleansMethodRouter), FieldAttributes.Private | FieldAttributes.InitOnly);

            // Create constructor
            CreateConstructor(typeBuilder, pluginField, routerField);

            // Create the method
            var methodBuilder = CreateMethodImplementation(typeBuilder, routingInfo, pluginField, routerField);

            // Apply Orleans attributes to the method
            ApplyOrleansAttributes(methodBuilder, routingInfo);

            // Create the type
            var generatedType = typeBuilder.CreateType()!;
            _generatedTypes[typeName] = generatedType;

            var generatedMethod = generatedType.GetMethod(routingInfo.MethodName)!;
            
            _logger.LogInformation("Successfully created grain method: {MethodName}", routingInfo.MethodName);
            return generatedMethod;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create grain method: {MethodName}", routingInfo.MethodName);
            throw new MethodGenerationException($"Failed to create method {routingInfo.MethodName}", ex);
        }
    }

    private void CreateConstructor(TypeBuilder typeBuilder, FieldBuilder pluginField, FieldBuilder routerField)
    {
        var constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            new[] { typeof(IAgentPlugin), typeof(OrleansMethodRouter) });

        var il = constructor.GetILGenerator();

        // Call base constructor
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);

        // Set plugin field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, pluginField);

        // Set router field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stfld, routerField);

        il.Emit(OpCodes.Ret);
    }

    private MethodBuilder CreateMethodImplementation(TypeBuilder typeBuilder, MethodRoutingInfo routingInfo, 
        FieldBuilder pluginField, FieldBuilder routerField)
    {
        var methodBuilder = typeBuilder.DefineMethod(
            routingInfo.MethodName,
            MethodAttributes.Public | MethodAttributes.Virtual,
            routingInfo.ReturnType,
            routingInfo.ParameterTypes);

        var il = methodBuilder.GetILGenerator();

        // Declare local variables
        var parametersArray = il.DeclareLocal(typeof(object[]));
        var result = il.DeclareLocal(typeof(object));

        // Create parameters array
        il.Emit(OpCodes.Ldc_I4, routingInfo.ParameterTypes.Length);
        il.Emit(OpCodes.Newarr, typeof(object));
        il.Emit(OpCodes.Stloc, parametersArray);

        // Fill parameters array
        for (int i = 0; i < routingInfo.ParameterTypes.Length; i++)
        {
            il.Emit(OpCodes.Ldloc, parametersArray);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 1);
            
            // Box value types
            if (routingInfo.ParameterTypes[i].IsValueType)
            {
                il.Emit(OpCodes.Box, routingInfo.ParameterTypes[i]);
            }
            
            il.Emit(OpCodes.Stelem_Ref);
        }

        // Load router field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, routerField);

        // Load plugin field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, pluginField);

        // Load method name
        il.Emit(OpCodes.Ldstr, routingInfo.MethodName);

        // Load parameters array
        il.Emit(OpCodes.Ldloc, parametersArray);

        // Call RouteMethodCallAsync
        var routeMethod = typeof(OrleansMethodRouter).GetMethod(nameof(OrleansMethodRouter.RouteMethodCallAsync))!;
        il.Emit(OpCodes.Callvirt, routeMethod);

        // Handle async return types
        if (routingInfo.ReturnType == typeof(Task))
        {
            // For Task return type, just return the task
            il.Emit(OpCodes.Castclass, typeof(Task));
            il.Emit(OpCodes.Ret);
        }
        else if (routingInfo.ReturnType.IsGenericType && routingInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            // For Task<T> return type, cast and return
            il.Emit(OpCodes.Castclass, routingInfo.ReturnType);
            il.Emit(OpCodes.Ret);
        }
        else if (routingInfo.ReturnType == typeof(void))
        {
            // For void methods, await the task and return
            var waitMethod = typeof(Task).GetMethod(nameof(Task.Wait), new[] { typeof(int) })!;
            il.Emit(OpCodes.Castclass, typeof(Task));
            il.Emit(OpCodes.Ldc_I4, -1); // Infinite timeout
            il.Emit(OpCodes.Callvirt, waitMethod);
            il.Emit(OpCodes.Pop); // Discard result
            il.Emit(OpCodes.Ret);
        }
        else
        {
            // For synchronous return types, get result from task
            var taskOfObjectType = typeof(Task<object>);
            var resultProperty = taskOfObjectType.GetProperty(nameof(Task<object>.Result))!;
            
            il.Emit(OpCodes.Castclass, taskOfObjectType);
            il.Emit(OpCodes.Callvirt, resultProperty.GetMethod!);
            
            // Cast to expected return type
            if (routingInfo.ReturnType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, routingInfo.ReturnType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, routingInfo.ReturnType);
            }
            
            il.Emit(OpCodes.Ret);
        }

        return methodBuilder;
    }

    private void ApplyOrleansAttributes(MethodBuilder methodBuilder, MethodRoutingInfo routingInfo)
    {
        // Apply ReadOnly attribute
        if (routingInfo.IsReadOnly)
        {
            var readOnlyAttr = new CustomAttributeBuilder(
                typeof(ReadOnlyAttribute).GetConstructor(Type.EmptyTypes)!,
                Array.Empty<object>());
            methodBuilder.SetCustomAttribute(readOnlyAttr);
        }

        // Apply AlwaysInterleave attribute
        if (routingInfo.AlwaysInterleave)
        {
            var interleaveAttr = new CustomAttributeBuilder(
                typeof(AlwaysInterleaveAttribute).GetConstructor(Type.EmptyTypes)!,
                Array.Empty<object>());
            methodBuilder.SetCustomAttribute(interleaveAttr);
        }

        // Apply OneWay attribute
        if (routingInfo.OneWay)
        {
            var oneWayAttr = new CustomAttributeBuilder(
                typeof(OneWayAttribute).GetConstructor(Type.EmptyTypes)!,
                Array.Empty<object>());
            methodBuilder.SetCustomAttribute(oneWayAttr);
        }

        // Apply AsyncStateMachine attribute for async methods
        if (routingInfo.ReturnType == typeof(Task) || 
            (routingInfo.ReturnType.IsGenericType && routingInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
        {
            var asyncAttr = new CustomAttributeBuilder(
                typeof(AsyncStateMachineAttribute).GetConstructor(new[] { typeof(Type) })!,
                new object[] { typeof(OrleansGrainProxyGenerator) });
            methodBuilder.SetCustomAttribute(asyncAttr);
        }
    }
}

/// <summary>
/// Castle DynamicProxy interceptor that routes grain calls to plugins
/// </summary>
public class PluginGrainInterceptor<TGrainInterface> : IInterceptor
    where TGrainInterface : class
{
    private readonly IAgentPlugin _plugin;
    private readonly OrleansMethodRouter _methodRouter;
    private readonly ILogger _logger;

    public PluginGrainInterceptor(IAgentPlugin plugin, OrleansMethodRouter methodRouter, ILogger logger)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _methodRouter = methodRouter ?? throw new ArgumentNullException(nameof(methodRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Intercept(IInvocation invocation)
    {
        var methodName = invocation.Method.Name;
        var parameters = invocation.Arguments;

        try
        {
            _logger.LogDebug("Intercepting grain method call: {MethodName}", methodName);

            // Route the call through the method router
            var resultTask = _methodRouter.RouteMethodCallAsync(_plugin, methodName, parameters);

            // Handle different return types
            if (invocation.Method.ReturnType == typeof(Task))
            {
                invocation.ReturnValue = resultTask;
            }
            else if (invocation.Method.ReturnType.IsGenericType && 
                     invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // For Task<T>, we need to convert Task<object?> to Task<T>
                var genericArgument = invocation.Method.ReturnType.GetGenericArguments()[0];
                var convertMethod = typeof(OrleansGrainProxyGenerator)
                    .GetMethod("ConvertTaskResult", BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(genericArgument);

                invocation.ReturnValue = convertMethod.Invoke(null, new object[] { resultTask });
            }
            else if (invocation.Method.ReturnType == typeof(void))
            {
                // For void methods, start the task but don't wait
                _ = resultTask;
            }
            else
            {
                // For synchronous return types, block and get result
                var task = resultTask as Task<object>;
                if (task != null)
                {
                    task.Wait();
                    var result = task.Result;
                    
                    // Convert result if needed
                    if (result != null && !invocation.Method.ReturnType.IsAssignableFrom(result.GetType()))
                    {
                        try
                        {
                            result = Convert.ChangeType(result, invocation.Method.ReturnType);
                        }
                        catch
                        {
                            _logger.LogWarning("Could not convert result type from {SourceType} to {TargetType}", 
                                result.GetType(), invocation.Method.ReturnType);
                        }
                    }
                    
                    invocation.ReturnValue = result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error intercepting method call: {MethodName}", methodName);
            throw;
        }
    }
}

/// <summary>
/// Exception thrown when proxy generation fails
/// </summary>
public class ProxyGenerationException : Exception
{
    public ProxyGenerationException(string message) : base(message) { }
    public ProxyGenerationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when method generation fails
/// </summary>
public class MethodGenerationException : Exception
{
    public MethodGenerationException(string message) : base(message) { }
    public MethodGenerationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Information about method routing
/// </summary>
public class MethodRoutingInfo
{
    public MethodInfo MethodInfo { get; set; } = null!;
    public string MethodName { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
    public bool AlwaysInterleave { get; set; }
    public bool OneWay { get; set; }
    public Type[] ParameterTypes { get; set; } = Array.Empty<Type>();
    public Type ReturnType { get; set; } = typeof(void);
}

/// <summary>
/// Scope for applying Orleans attribute behavior
/// </summary>
public class OrleansAttributeScope : IDisposable
{
    private readonly MethodRoutingInfo _routingInfo;
    private readonly ILogger _logger;

    public OrleansAttributeScope(MethodRoutingInfo routingInfo, ILogger logger)
    {
        _routingInfo = routingInfo ?? throw new ArgumentNullException(nameof(routingInfo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Apply Orleans attribute behavior on enter
        ApplyOrleansAttributes();
    }

    public void Dispose()
    {
        // Clean up Orleans attribute behavior on exit
        CleanupOrleansAttributes();
    }

    private void ApplyOrleansAttributes()
    {
        // For ReadOnly methods, we could set thread-local flags or other optimizations
        if (_routingInfo.IsReadOnly)
        {
            _logger.LogTrace("Applying ReadOnly behavior for method: {MethodName}", _routingInfo.MethodName);
            // Orleans would handle this automatically, but we can add additional logic here
        }

        if (_routingInfo.AlwaysInterleave)
        {
            _logger.LogTrace("Applying AlwaysInterleave behavior for method: {MethodName}", _routingInfo.MethodName);
            // Orleans would handle this automatically
        }

        if (_routingInfo.OneWay)
        {
            _logger.LogTrace("Applying OneWay behavior for method: {MethodName}", _routingInfo.MethodName);
            // Orleans would handle this automatically
        }
    }

    private void CleanupOrleansAttributes()
    {
        // Cleanup any applied attribute behavior
        _logger.LogTrace("Cleaning up Orleans attribute behavior for method: {MethodName}", _routingInfo.MethodName);
    }
}

/// <summary>
/// Extension methods for Orleans attribute mapping
/// </summary>
public static class OrleansAttributeMapper
{
    /// <summary>
    /// Map plugin attributes to Orleans attributes
    /// </summary>
    public static IEnumerable<Attribute> MapToOrleansAttributes(AgentMethodAttribute agentMethodAttr)
    {
        var attributes = new List<Attribute>();

        if (agentMethodAttr.IsReadOnly)
        {
            attributes.Add(new ReadOnlyAttribute());
        }

        if (agentMethodAttr.AlwaysInterleave)
        {
            attributes.Add(new AlwaysInterleaveAttribute());
        }

        if (agentMethodAttr.OneWay)
        {
            attributes.Add(new OneWayAttribute());
        }

        return attributes;
    }

    /// <summary>
    /// Check if Orleans attributes are compatible with plugin attributes
    /// </summary>
    public static bool AreAttributesCompatible(AgentMethodAttribute agentAttr, MethodInfo orleansMethod)
    {
        var hasReadOnly = orleansMethod.GetCustomAttribute<ReadOnlyAttribute>() != null;
        var hasAlwaysInterleave = orleansMethod.GetCustomAttribute<AlwaysInterleaveAttribute>() != null;
        var hasOneWay = orleansMethod.GetCustomAttribute<OneWayAttribute>() != null;

        return agentAttr.IsReadOnly == hasReadOnly &&
               agentAttr.AlwaysInterleave == hasAlwaysInterleave &&
               agentAttr.OneWay == hasOneWay;
    }
}