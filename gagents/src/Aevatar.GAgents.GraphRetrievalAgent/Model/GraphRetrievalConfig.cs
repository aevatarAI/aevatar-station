using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.GraphRetrievalAgent.Model;

[GenerateSerializer]
public class GraphRetrievalConfig : ConfigurationBase
{
    [Id(0)]
    public string Schema { get; set; } = string.Empty;
    
    [Id(1)]
    public string Example { get; set; } = string.Empty;
    
    [Id(2)]
    public int MaxResults { get; set; } = 10;
    
    [Id(3)]
    public double SimilarityThreshold { get; set; } = 0.8;
    
    [Id(4)]
    public int MaxDepth { get; set; } = 3;
    
    [Id(5)]
    public bool EnableSemanticSearch { get; set; } = true;
}