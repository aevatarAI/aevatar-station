using System.ComponentModel.DataAnnotations;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.GraphRetrievalAgent.Model;

[GenerateSerializer]
public class GraphRetrievalConfig : ConfigurationBase
{
    [Id(0)]
    [Required(ErrorMessage = "Schema is required")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Schema must be between 1 and 5000 characters")]
    public string Schema { get; set; } = string.Empty;
    
    [Id(1)]
    [Required(ErrorMessage = "Example is required")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Example must be between 1 and 2000 characters")]
    public string Example { get; set; } = string.Empty;
    
    [Id(2)]
    [Range(1, 1000, ErrorMessage = "Max Results must be between 1 and 1000")]
    public int MaxResults { get; set; } = 10;
    
    [Id(3)]
    [Range(0.0, 1.0, ErrorMessage = "Similarity Threshold must be between 0.0 and 1.0")]
    public double SimilarityThreshold { get; set; } = 0.8;
    
    [Id(4)]
    [Range(1, 20, ErrorMessage = "Max Depth must be between 1 and 20")]
    public int MaxDepth { get; set; } = 3;
    
    [Id(5)]
    public bool EnableSemanticSearch { get; set; } = true;
}