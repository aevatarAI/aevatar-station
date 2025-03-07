namespace Aevatar.Neo4JStore.Options;

public class Neo4JDriverConfig
{
    public const string ConfigSectionName = "Neo4j";
    public string Uri { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
}