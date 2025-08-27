namespace Aevatar.Core.Abstractions.Plugin;

[GenerateSerializer]
public class RemovePluginDto
{
    [Id(0)] public required Guid TenantId { get; set; }
    [Id(1)] public required Guid PluginCodeId { get; set; }
}