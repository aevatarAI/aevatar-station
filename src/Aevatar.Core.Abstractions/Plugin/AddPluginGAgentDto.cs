namespace Aevatar.Core.Abstractions.Plugin;

public class AddPluginGAgentDto
{
    public required byte[] Code { get; set; } = [];
    public required Guid TenantId { get; set; }
}