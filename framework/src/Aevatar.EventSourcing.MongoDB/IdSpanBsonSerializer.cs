using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

public class IdSpanBsonSerializer : SerializerBase<IdSpan>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IdSpan value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("Value");
        context.Writer.WriteString(value.ToString());
        context.Writer.WriteEndDocument();
    }

    public override IdSpan Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();
        var value = context.Reader.ReadString();
        context.Reader.ReadEndDocument();
        return IdSpan.Create(value);
    }
}