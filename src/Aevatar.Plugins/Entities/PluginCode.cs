using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Volo.Abp.Domain.Entities;

namespace Aevatar.Plugins.Entities;

public class PluginCode : Entity<ObjectId>
{
    [BsonElement("tenantId")] public Guid TenantId { get; set; }

    [BsonElement("pluginAssemblies")] public List<byte[]> PluginAssemblies { get; set; }
}