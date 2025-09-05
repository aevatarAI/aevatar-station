using System.ComponentModel.DataAnnotations;

namespace Aevatar.GAgents.AI.Options;

/// <summary>
/// Contains settings to control the RAG experience.
/// </summary>
public class RagConfig
{
    public const string ConfigSectionName = "Rag";

    [Required]
    public string AIChatService { get; set; } = string.Empty;

    [Required]
    public string AIEmbeddingService { get; set; } = string.Empty;

    [Required]
    public bool BuildCollection { get; set; } = true;

    [Required]
    public string CollectionName { get; set; } = string.Empty;

    [Required]
    public int DataLoadingBatchSize { get; set; } = 2;

    [Required]
    public int MaxChunkCount { get; set; } = 500;
    
    [Required]
    public int DataLoadingBetweenBatchDelayInMilliseconds { get; set; } = 0;

    [Required]
    public string[]? PdfFilePaths { get; set; }

    [Required]
    public string VectorStoreType { get; set; } = string.Empty;
}
