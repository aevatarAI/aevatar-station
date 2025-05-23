namespace Aevatar.Core.Abstractions.Plugin;

[GenerateSerializer]
public class UpdatePluginDto
{
    [Id(0)] public required Guid TenantId { get; set; }
    [Id(1)] public required Guid PluginCodeId { get; set; }
    [Id(2)] public required byte[] Code { get; set; }
}