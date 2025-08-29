using System.ComponentModel.DataAnnotations;

namespace SimpleAIGAgent.Client.Options;

public class KnowledgeConfig
{
    [Required]
    public string[]? PdfFilePaths { get; set; }
}