using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Orleans.Runtime;

namespace Aevatar.EventSourcing.MongoDB.Tests;

public static class GrainTypeBsonSerializerConfig
{
    private static readonly object LockObj = new();
    private static bool _isRegistered;

    public static void Configure()
    {
        if (_isRegistered) return;
        
        lock (LockObj)
        {
            if (_isRegistered) return;

            if (!BsonClassMap.IsClassMapRegistered(typeof(GrainType)))
            {
                BsonSerializer.RegisterSerializer(new GrainTypeBsonSerializer());
            }

            _isRegistered = true;
        }
    }
}

// public class GrainTypeBsonSerializer : SerializerBase<GrainType>
// {
//     private readonly StringSerializer _stringSerializer = new();
//
//     public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GrainType value)
//     {
//         _stringSerializer.Serialize(context, args, value.ToString());
//     }
//
//     public override GrainType Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
//     {
//         var value = _stringSerializer.Deserialize(context, args);
//         return GrainType.Create(value);
//     }
// } 