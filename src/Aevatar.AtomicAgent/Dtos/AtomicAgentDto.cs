namespace Aevatar.AtomicAgent.Dtos;

public class AtomicAgentDto
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
}