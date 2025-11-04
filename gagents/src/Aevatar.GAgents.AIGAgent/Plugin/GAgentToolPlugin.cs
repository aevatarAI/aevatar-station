using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;

namespace Aevatar.GAgents.AIGAgent.Plugin;

/// <summary>
/// Plugin that enables invoking GAgents as Semantic Kernel functions
/// </summary>
public class GAgentToolPlugin
{
    private readonly IGAgentExecutor _executor;
    private readonly IGAgentService _service;
    private readonly ILogger _logger;

    public GAgentToolPlugin(IGAgentExecutor executor, IGAgentService service, ILogger logger)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes a GAgent with the specified event
    /// </summary>
    /// <param name="grainType">The GrainType of the target GAgent</param>
    /// <param name="eventTypeName">The event type name to send</param>
    /// <param name="parameters">JSON serialized event parameters</param>
    /// <returns>JSON serialized result from the GAgent</returns>
    [KernelFunction("InvokeGAgent")]
    [Description("Invoke a GAgent with specified event")]
    public async Task<string> InvokeGAgentAsync(
        [Description("The GrainType of the target GAgent")]
        string grainType,
        [Description("The event type name to send")]
        string eventTypeName,
        [Description("JSON serialized event parameters")]
        string parameters)
    {
        try
        {
            _logger.LogInformation("Invoking GAgent {GrainType} with event {EventType}", grainType, eventTypeName);

            // Parse GrainType
            var targetGrainType = GrainType.Create(grainType);

            // Find event type
            var allGAgents = await _service.GetAllAvailableGAgentInformation();
            var eventTypes = allGAgents.GetValueOrDefault(targetGrainType);

            if (eventTypes == null)
            {
                var error = $"GAgent {grainType} not found";
                _logger.LogError(error);
                return JsonConvert.SerializeObject(new { success = false, error });
            }

            var eventType = eventTypes.FirstOrDefault(t => t.Name == eventTypeName);
            if (eventType == null)
            {
                var error = $"Event type {eventTypeName} not found for GAgent {grainType}";
                _logger.LogError(error);
                return JsonConvert.SerializeObject(new { success = false, error });
            }

            // Deserialize event parameters
            if (JsonConvert.DeserializeObject(parameters, eventType, new JsonSerializerSettings
                {
                    Converters = { new GrainIdConverter() }
                }) is not EventBase @event)
            {
                const string error = "Failed to deserialize event parameters";
                _logger.LogError(error);
                return JsonConvert.SerializeObject(new { success = false, error });
            }

            // Execute GAgent event handler
            var result = await _executor.ExecuteGAgentEventHandler(targetGrainType, @event);

            _logger.LogInformation("GAgent {GrainType} execution completed successfully", grainType);

            return JsonConvert.SerializeObject(new { success = true, result });
        }
        catch (TimeoutException tex)
        {
            _logger.LogError(tex, "Timeout invoking GAgent {GrainType} with event {EventType}", grainType,
                eventTypeName);
            return JsonConvert.SerializeObject(new
                { success = false, error = "Operation timed out", details = tex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking GAgent {GrainType} with event {EventType}", grainType, eventTypeName);
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Gets information about a specific GAgent
    /// </summary>
    [KernelFunction("GetGAgentInfo")]
    [Description("Get detailed information about a specific GAgent")]
    public async Task<string> GetGAgentInfoAsync(
        [Description("The GrainType of the GAgent")]
        string grainType)
    {
        try
        {
            var targetGrainType = GrainType.Create(grainType);
            var detailInfo = await _service.GetGAgentDetailInfoAsync(targetGrainType);

            return JsonConvert.SerializeObject(new
            {
                success = true,
                info = new
                {
                    grainType = detailInfo.GrainType.ToString(),
                    description = detailInfo.Description,
                    supportedEvents = detailInfo.SupportedEventTypes.Select(t => new
                    {
                        name = t.Name,
                        fullName = t.FullName
                    })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GAgent info for {GrainType}", grainType);
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Lists all available GAgents in the system
    /// </summary>
    [KernelFunction("ListGAgents")]
    [Description("List all available GAgents in the system")]
    public async Task<string> ListGAgentsAsync()
    {
        try
        {
            var allGAgents = await _service.GetAllAvailableGAgentInformation();

            var gAgentList = new List<object>();
            foreach (var (grainType, eventTypes) in allGAgents)
            {
                gAgentList.Add(new
                {
                    grainType = grainType.ToString(),
                    eventCount = eventTypes.Count,
                    events = eventTypes.Select(t => t.Name)
                });
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                gAgents = gAgentList,
                total = gAgentList.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing GAgents");
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }
}

public class GrainIdConverter : JsonConverter<GrainId>
{
    public override GrainId ReadJson(JsonReader reader, Type objectType, GrainId existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return default;

        if (reader.TokenType == JsonToken.String)
        {
            string value = (string)reader.Value;
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    return GrainId.Parse(value);
                }
                catch (FormatException)
                {
                    return GrainId.Create("User", value);
                }
            }
        }
        else if (reader.TokenType == JsonToken.StartObject)
        {
            JObject jsonObject = JObject.Load(reader);
            
            if (jsonObject.TryGetValue("tv", out var typeValue) && 
                jsonObject.TryGetValue("kv", out var keyValue))
            {
                string typeStr = typeValue.ToString();
                string keyStr = keyValue.ToString();

                try
                {
                    return GrainId.Parse($"{typeStr}/{keyStr}");
                }
                catch
                {
                    return GrainId.Create(typeStr, keyStr);
                }
            }
        }

        throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing GrainId");
    }

    public override void WriteJson(JsonWriter writer, GrainId value, JsonSerializer serializer)
    {
        if (value == default)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(value.ToString());
        }
    }
}