using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Core.Interception.Context;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Interception.Models;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aevatar.Core.Interception
{
    /// <summary>
    /// Attribute that provides method tracing capabilities through Fody MethodDecorator
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
    public class InterceptorAttribute : Attribute
    {
        private static readonly ActivitySource _activitySource = new("Aevatar.Core.Interception");
        
        /// <summary>
        /// Static service provider for DI-based logger resolution
        /// This allows tests and applications to provide centralized logging services
        /// </summary>
        public static IServiceProvider? ServiceProvider { get; set; }
        
        private object? _instance;
        private MethodBase? _method;
        private object[]? _args;
        private ILogger? _logger;
        private Activity? _activity;

        /// <summary>
        /// Called before the method execution to initialize the interceptor
        /// </summary>
        public void Init(object instance, MethodBase method, object[] args)
        {
            _instance = instance;
            _method = method;
            _args = args;

            // Logger discovery priority:
            // 1. Instance implements ILogger directly
            if (instance is ILogger logger)
            {
                _logger = logger;
            }
            // 2. Instance has Logger property that returns ILogger
            else if (instance?.GetType().GetProperty("Logger")?.GetValue(instance) is ILogger instanceLogger)
            {
                _logger = instanceLogger;
            }
            // 3. Try to resolve logger from DI container (for static methods, extension methods, etc.)
            else if (ServiceProvider != null)
            {
                try
                {
                    // Try to get a specific logger for the declaring type
                    var declaringType = method.DeclaringType;
                    if (declaringType != null)
                    {
                        var loggerType = typeof(ILogger<>).MakeGenericType(declaringType);
                        _logger = ServiceProvider.GetService(loggerType) as ILogger;
                    }
                    
                    // If specific logger not found, try general ILogger
                    if (_logger == null)
                    {
                        _logger = ServiceProvider.GetService<ILogger>();
                    }
                }
                catch
                {
                    // If DI resolution fails, continue to fallback
                    _logger = null;
                }
            }
            
            // 4. Fallback to console logger
            if (_logger == null)
            {
                _logger = new ConsoleLogger();
            }

            Init();

            // Log initialization with appropriate level
            if (ShouldTrace())
            {
                _logger?.LogDebug("TRACE: Init: {MethodFullName}.{MethodName} [{ParameterCount} parameters]", 
                    method.DeclaringType?.FullName, method.Name, args.Length);
            }
        }

        /// <summary>
        /// Called before the method execution to initialize the interceptor
        /// </summary>
        public void Init()
        {
            var traceId = TraceContext.ActiveTraceId;
            _logger?.LogDebug("TRACE: InterceptorAttribute.Init: {TraceId}", traceId);
            
            // Log complete TraceContext state for debugging
            LogTraceContextState("InterceptorAttribute.Init");
            
            if (string.IsNullOrEmpty(traceId))
            {
                _logger?.LogWarning("TRACE: InterceptorAttribute.Init: No active trace ID found");
                return;
            }

            if (TraceContext.IsTracingEnabled)
            {
                _logger?.LogDebug("TRACE: InterceptorAttribute.Init: Tracing enabled for {TraceId}", traceId);
            }
            else
            {
                _logger?.LogDebug("TRACE: InterceptorAttribute.Init: Tracing disabled for {TraceId}", traceId);
            }
        }
        
        /// <summary>
        /// Serializes parameter values for logging, using ToString() for simple types and JSON for complex objects
        /// </summary>
        /// <param name="value">The value to serialize</param>
        /// <returns>String representation of the value</returns>
        private static string SerializeParameterValue(object? value)
        {
            if (value == null) return "null";
            
            var type = value.GetType();
            
            // Use ToString() for simple types (bool.IsPrimitive is true and returns "True"/"False")
            if (type.IsPrimitive || type.IsEnum || value is string || value is DateTime || value is DateTimeOffset || 
                value is Guid || value is TimeSpan || value is decimal)
            {
                return value.ToString() ?? "null";
            }
            
            // Use JSON for complex objects
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    MaxDepth = 3,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };
                
                var json = JsonSerializer.Serialize(value, options);
                
                // Truncate if too long for logs
                if (json.Length > 500)
                {
                    json = json.Substring(0, 500) + "...";
                }
                
                return json;
            }
            catch (Exception)
            {
                // Fallback to ToString if JSON serialization fails
                return value.ToString() ?? "null";
            }
        }
        
        /// <summary>
        /// Logs the complete state of TraceContext including all properties and nested TraceConfig values
        /// </summary>
        private void LogTraceContextState(string context)
        {
            try
            {
                var activeTraceId = TraceContext.ActiveTraceId;
                var isTracingEnabled = TraceContext.IsTracingEnabled;
                var traceConfig = TraceContext.GetTraceConfig();
                var trackedIds = TraceContext.GetTrackedIds();
                
                _logger?.LogInformation("=== TraceContext State {Context} ===", context);
                _logger?.LogInformation("ActiveTraceId: {ActiveTraceId}", activeTraceId ?? "NULL");
                _logger?.LogInformation("IsTracingEnabled: {IsTracingEnabled}", isTracingEnabled);
                
                // Log TraceConfig object properties
                if (traceConfig != null)
                {
                    _logger?.LogInformation("TraceConfig.Enabled: {Enabled}", traceConfig.Enabled);
                    _logger?.LogInformation("TraceConfig.TrackedIds.Count: {TrackedIdsCount}", traceConfig.TrackedIds.Count);
                    
                    if (traceConfig.TrackedIds.Count > 0)
                    {
                        var trackedIdsList = string.Join(", ", traceConfig.TrackedIds);
                        _logger?.LogInformation("TraceConfig.TrackedIds: [{TrackedIds}]", trackedIdsList);
                    }
                    else
                    {
                        _logger?.LogInformation("TraceConfig.TrackedIds: [EMPTY]");
                    }
                }
                else
                {
                    _logger?.LogWarning("TraceConfig is NULL");
                }
                
                // Log tracked IDs from TraceContext
                if (trackedIds.Count > 0)
                {
                    var trackedIdsList = string.Join(", ", trackedIds);
                    _logger?.LogInformation("GetTrackedIds().Count: {Count}", trackedIds.Count);
                    _logger?.LogInformation("GetTrackedIds(): [{TrackedIds}]", trackedIdsList);
                }
                else
                {
                    _logger?.LogInformation("GetTrackedIds(): [EMPTY]");
                }
                
                _logger?.LogInformation("=== End TraceContext State ===");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging TraceContext state for {Context}", context);
            }
        }

        /// <summary>
        /// Called when entering the method
        /// </summary>
        public void OnEntry()
        {
            if (_method != null)
            {
                // Only log trace messages if tracing is enabled
                if (ShouldTrace())
                {
                    _logger?.LogDebug("TRACE: Entering {MethodName}", _method.Name);
                    
                    // Create OpenTelemetry activity
                    CreateActivity();
                    
                    // Log parameters if available
                    if (_args != null && _args.Length > 0)
                    {
                        for (int i = 0; i < _args.Length; i++)
                        {
                            var paramName = _method.GetParameters()[i].Name;
                            var paramValue = SerializeParameterValue(_args[i]);
                            _logger?.LogDebug("TRACE: Parameter {ParameterName} = {ParameterValue}", paramName, paramValue);
                            
                            // Add parameter to OpenTelemetry activity
                            if (_activity != null)
                            {
                                _activity.SetTag($"parameter.{paramName}", paramValue);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when exiting the method normally
        /// </summary>
        public void OnExit()
        {
            if (_method != null)
            {
                // Only log trace messages if tracing is enabled
                if (ShouldTrace())
                {
                    _logger?.LogDebug("TRACE: Exiting {MethodName}", _method.Name);
                }
                
                // Complete OpenTelemetry activity if it exists
                if (_activity != null)
                {
                    _activity.Dispose();
                    _activity = null;
                }
            }
        }

        /// <summary>
        /// Called when an exception occurs in the method
        /// </summary>
        public void OnException(Exception exception)
        {
            if (_method != null)
            {
                // Only log trace messages if tracing is enabled
                if (ShouldTrace())
                {
                    // Always include stack trace when logging exceptions
                    _logger?.LogError(exception, "TRACE: Exception in {MethodName}: {ExceptionType}: {ExceptionMessage}", 
                        _method.Name, exception.GetType().Name, exception.Message);
                }
                
                // Record exception in OpenTelemetry activity if it exists
                if (_activity != null)
                {
                    _activity.SetTag("exception.type", exception.GetType().Name);
                    _activity.SetTag("exception.message", exception.Message);
                    _activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                    _activity.Dispose();
                    _activity = null;
                }
            }
        }

        /// <summary>
        /// Called for async methods when the task completes
        /// </summary>
        public void OnTaskContinuation(Task task)
        {
            if (_method == null) return;

            LogDebugTaskInfo(task);
                
            if (task.Status == TaskStatus.WaitingForActivation)
            {
                SetupTaskContinuation(task);
            }
            else
            {
                HandleCompletedTask(task);
            }
        }

        /// <summary>
        /// Sets up a continuation to log when the task actually completes
        /// </summary>
        private void SetupTaskContinuation(Task task)
        {
            _logger?.LogDebug("DEBUG: Setting up task continuation for {MethodName}", _method?.Name);
            
            task.ContinueWith(LogTaskCompletion, TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Logs the completion status of a task
        /// </summary>
        private void LogTaskCompletion(Task completedTask)
        {
            // Only log trace messages if tracing is enabled
            if (ShouldTrace())
            {
                if (completedTask.IsCanceled)
                {
                    _logger?.LogWarning("TRACE: Async canceled {MethodName}", _method?.Name);
                    // Create OpenTelemetry activity for canceled async method
                    CreateAsyncActivity("canceled");
                }
                else if (completedTask.IsFaulted && completedTask.Exception != null)
                {
                    var exception = completedTask.Exception.InnerException ?? completedTask.Exception;
                    // Always include stack trace when logging async exceptions
                    _logger?.LogError(exception, "TRACE: Async exception in {MethodName}: {ExceptionType}: {ExceptionMessage}", 
                        _method?.Name, exception.GetType().Name, exception.Message);
                    // Create OpenTelemetry activity for faulted async method
                    CreateAsyncActivity("faulted", exception);
                }
                else if (completedTask.IsCompleted)
                {
                    _logger?.LogDebug("TRACE: Async completed {MethodName}", _method?.Name);
                    // Create OpenTelemetry activity for completed async method
                    CreateAsyncActivity("completed");
                }
            }
        }

        /// <summary>
        /// Handles tasks that are already in a final state when OnTaskContinuation is called
        /// </summary>
        private void HandleCompletedTask(Task task)
        {
            LogTaskExceptionDetails(task);
            LogTaskCompletion(task);
        }

        /// <summary>
        /// Logs detailed exception information for debugging
        /// </summary>
        private void LogTaskExceptionDetails(Task task)
        {
            if (task.Exception != null)
            {
                _logger?.LogDebug("DEBUG: Task has exception: {ExceptionType}", task.Exception.GetType().Name);
                if (task.Exception.InnerException != null)
                {
                    _logger?.LogDebug("DEBUG: Inner exception: {InnerExceptionType}: {InnerExceptionMessage}", 
                        task.Exception.InnerException.GetType().Name, task.Exception.InnerException.Message);
                }
            }
        }

        /// <summary>
        /// Logs debug information about the task status
        /// </summary>
        private void LogDebugTaskInfo(Task task)
        {
            _logger?.LogDebug("DEBUG: OnTaskContinuation called for {MethodName}", _method?.Name);
            _logger?.LogDebug("DEBUG: Task Status: {TaskStatus}, IsCompleted: {IsCompleted}, IsFaulted: {IsFaulted}, IsCanceled: {IsCanceled}", 
                task.Status, task.IsCompleted, task.IsFaulted, task.IsCanceled);
        }

        /// <summary>
        /// Checks if tracing should be enabled for the current context.
        /// </summary>
        /// <returns>True if tracing is enabled, false otherwise.</returns>
        private bool ShouldTrace()
        {
            // Check if tracing is enabled via TraceContext
            return TraceContext.IsTracingEnabled;
        }



        /// <summary>
        /// Creates an OpenTelemetry activity for method tracing
        /// </summary>
        private void CreateActivity()
        {
            if (_method == null) return;

            var activityName = $"{_method.DeclaringType?.Name}.{_method.Name}";
            
            _activity = _activitySource.StartActivity(activityName);
            
            if (_activity != null)
            {
                // Set basic activity attributes
                _activity.SetTag("method.name", _method.Name);
                _activity.SetTag("method.declaring_type", _method.DeclaringType?.FullName ?? "unknown");
                _activity.SetTag("method.parameters_count", _args?.Length ?? 0);
                
                // Set trace ID from TraceContext if available
                var traceId = TraceContext.ActiveTraceId;
                if (!string.IsNullOrEmpty(traceId))
                {
                    _activity.SetTag("aevatar.trace_id", traceId);
                }
            }
        }

        /// <summary>
        /// Creates an OpenTelemetry activity for async method completion states
        /// </summary>
        private void CreateAsyncActivity(string completionState, Exception? exception = null)
        {
            if (_method == null) return;

            var activityName = $"{_method.DeclaringType?.Name}.{_method.Name}.{completionState}";
            
            var asyncActivity = _activitySource.StartActivity(activityName);
            
            if (asyncActivity != null)
            {
                // Set basic activity attributes
                asyncActivity.SetTag("method.name", _method.Name);
                asyncActivity.SetTag("method.declaring_type", _method.DeclaringType?.FullName ?? "unknown");
                asyncActivity.SetTag("method.parameters_count", _args?.Length ?? 0);
                asyncActivity.SetTag("async.completion_state", completionState);
                asyncActivity.SetTag("async.method", true);
                
                // Set trace ID from TraceContext if available
                var traceId = TraceContext.ActiveTraceId;
                if (!string.IsNullOrEmpty(traceId))
                {
                    asyncActivity.SetTag("aevatar.trace_id", traceId);
                }

                // Handle exception for faulted tasks
                if (exception != null)
                {
                    asyncActivity.SetTag("exception.type", exception.GetType().Name);
                    asyncActivity.SetTag("exception.message", exception.Message);
                    asyncActivity.SetStatus(ActivityStatusCode.Error, exception.Message);
                }
                else if (completionState == "canceled")
                {
                    asyncActivity.SetStatus(ActivityStatusCode.Error, "Task was canceled");
                }
                else
                {
                    asyncActivity.SetStatus(ActivityStatusCode.Ok);
                }

                // Dispose the activity immediately since it represents a completion event
                asyncActivity.Dispose();
            }
        }

        /// <summary>
        /// Simple console logger implementation for fallback scenarios when no logger is available
        /// </summary>
        private class ConsoleLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => null;
            
            public bool IsEnabled(LogLevel logLevel) => true;
            
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (formatter != null)
                {
                    var message = formatter(state, exception);
                    Console.WriteLine(message);
                }
            }
        }
    }
}
