using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Serialization;

namespace Aevatar.Silo;

public class HybridGrainStateSerializer : IGrainStateSerializer
{
    private readonly JsonGrainStateSerializer _jsonGrainStateSerializer;
    private readonly BinaryGrainStateSerializer _binaryGrainStateSerializer;
    private readonly ILogger<HybridGrainStateSerializer> _logger;

    public HybridGrainStateSerializer(Serializer serializer, IOptions<JsonGrainStateSerializerOptions> options,
        IServiceProvider serviceProvider, ILogger<HybridGrainStateSerializer> logger)
    {
        _logger = logger;
        _jsonGrainStateSerializer = new JsonGrainStateSerializer(options, serviceProvider);
        _binaryGrainStateSerializer = new BinaryGrainStateSerializer(serializer);
    }

    public T Deserialize<T>(BsonValue value)
    {
        _logger.LogInformation("🔄 Enhanced Orleans HybridGrainStateSerializer.Deserialize called for type {Type}", typeof(T).Name);
        
        if (value == null || value.IsBsonNull)
        {
            _logger.LogInformation("🔄 Enhanced Orleans BsonValue is null or empty, returning default instance");
            return default(T);
        }
        
        if (value.IsBsonDocument)
        {
            var doc = value.AsBsonDocument;
            _logger.LogInformation("🔄 Enhanced Orleans BsonDocument fields: {Fields}", string.Join(", ", doc.Elements.Select(e => e.Name)));
            
            // Check if the document has the 'data' field required for binary deserialization (Orleans format)
            if (doc.Contains("data"))
            {
                _logger.LogInformation("🔄 Enhanced Orleans Found 'data' field (Orleans format), attempting binary deserialization");
                try
                {
                    var result = _binaryGrainStateSerializer.Deserialize<T>(value);
                    _logger.LogInformation("🔄 Enhanced Orleans Binary deserialization successful");
                    return result;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "🔄 Enhanced Orleans Grain state binary deserialize error, falling back to JSON: {ErrorMessage}", e.Message);
                    try
                    {
                        var result = _jsonGrainStateSerializer.Deserialize<T>(value);
                        _logger.LogInformation("🔄 Enhanced Orleans JSON fallback successful");
                        return result;
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogError(jsonEx, "🔄 Enhanced Orleans JSON fallback also failed: {JsonError}", jsonEx.Message);
                        throw;
                    }
                }
            }
        }
        
        // Use JSON deserialization for documents without 'data' field (legacy format)
        try
        {
            var result = _jsonGrainStateSerializer.Deserialize<T>(value);
            _logger.LogInformation("🔄 Enhanced Orleans JSON deserialization successful");
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "🔄 Enhanced Orleans JSON deserialization failed: {ErrorMessage}", e.Message);
            throw;
        }
    }

    public BsonValue Serialize<T>(T state)
    {
        var result = _binaryGrainStateSerializer.Serialize(state);
        _logger.LogInformation("🔄 Enhanced Orleans Binary serialization completed");
        return result;
    }
}