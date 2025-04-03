namespace Aevatar.Neo4JStore.Options;

public class Neo4JConfigOptions
{
    public string Uri { get; set; } = "bolt://localhost:7687";
    public string User { get; set; }
    public string Password { get; set; }
    public int MaxConnectionPoolSize { get; set; } = 100;
    public long ConnectionTimeout { get; set; } = 30 * 1000;
    public long ConnectionAcquisitionTimeout { get; set; } = 60 * 1000;
    public long MaxConnectionLifetime { get; set; } = 60 * 1000;
}