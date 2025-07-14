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
        try
        {
            return _binaryGrainStateSerializer.Deserialize<T>(value);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e,"Grain state binary deserialize error");
            return _jsonGrainStateSerializer.Deserialize<T>(value);
        }
    }

    public BsonValue Serialize<T>(T state)
    {
        return _binaryGrainStateSerializer.Serialize(state);
    }
}