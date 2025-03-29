using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Orleans.Runtime;

namespace Aevatar.EventSourcing.MongoDB.Tests;

public static class IdSpanBsonSerializerConfig
{
    private static readonly object LockObj = new();
    private static bool _isRegistered;

    public static void Configure()
    {
        if (_isRegistered) return;
        
        lock (LockObj)
        {
            if (_isRegistered) return;

            if (!BsonClassMap.IsClassMapRegistered(typeof(IdSpan)))
            {
                BsonSerializer.RegisterSerializer(new IdSpanBsonSerializer());
            }

            _isRegistered = true;
        }
    }
}

public class IdSpanBsonSerializer : SerializerBase<IdSpan>
{
    private readonly StringSerializer _stringSerializer = new();

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IdSpan value)
    {
        _stringSerializer.Serialize(context, args, value.ToString());
    }

    public override IdSpan Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = _stringSerializer.Deserialize(context, args);
        return IdSpan.Create(value);
    }
} 