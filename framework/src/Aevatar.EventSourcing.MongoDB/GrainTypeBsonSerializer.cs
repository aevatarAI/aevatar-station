using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Aevatar.EventSourcing.MongoDB;

using Orleans.Runtime;

public class GrainTypeBsonSerializer : SerializerBase<GrainType>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GrainType value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("Value");
        context.Writer.WriteString(value.Value.ToString());
        context.Writer.WriteEndDocument();
    }

    public override GrainType Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();
        var value = context.Reader.ReadString();
        context.Reader.ReadEndDocument();
        return GrainType.Create(value);
    }
}