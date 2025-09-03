using Orleans;
using System.ComponentModel;
using Aevatar.GAgents.Basic;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;



[GenerateSerializer]
public enum ChatAISystemLLMEnum
{
    [Description("OpenAI GPT models for text generation and completion")]
    // [GenericMeta(SystemLLMMetaKeys.Provider, "openai")]
    // [GenericMeta(SystemLLMMetaKeys.Type, "general-purpose llm")]
    // [GenericMeta(SystemLLMMetaKeys.Strengths, "creative writing, general reasoning, versatile, well-balanced performance")]
    // [GenericMeta(SystemLLMMetaKeys.BestFor, "general tasks, creative writing, conversational AI, content generation")]
    // [GenericMeta(SystemLLMMetaKeys.Speed, "fast and reliable")]
    OpenAI = 0,
    
    [Description("DeepSeek's advanced language models optimized for reasoning")]
    // [GenericMeta(SystemLLMMetaKeys.Provider, "deepseek")]
    // [GenericMeta(SystemLLMMetaKeys.Type, "reasoning-optimized llm")]
    // [GenericMeta(SystemLLMMetaKeys.Strengths, "advanced reasoning, mathematical thinking, logical analysis, deep problem-solving")]
    // [GenericMeta(SystemLLMMetaKeys.BestFor, "complex reasoning, mathematical problems, analytical tasks, research assistance")]
    // [GenericMeta(SystemLLMMetaKeys.Speed, "moderate, optimized for accuracy over speed")]
    DeepSeek = 1,
    
    [Description("Azure-hosted OpenAI models with enterprise security")]
    // [GenericMeta(SystemLLMMetaKeys.Provider, "azure_openai")]
    // [GenericMeta(SystemLLMMetaKeys.Type, "enterprise llm")]
    // [GenericMeta(SystemLLMMetaKeys.Strengths, "enterprise security, compliance, scalability, data privacy, regional deployment")]
    // [GenericMeta(SystemLLMMetaKeys.BestFor, "enterprise applications, production systems, secure environments, regulated industries")]
    // [GenericMeta(SystemLLMMetaKeys.Speed, "fast with enterprise-grade reliability")]
    AzureOpenAI = 2,
    
    [Description("Azure OpenAI embedding models for semantic search")]
    // [GenericMeta(SystemLLMMetaKeys.Provider, "azure_openai")]
    // [GenericMeta(SystemLLMMetaKeys.Type, "embedding model")]
    // [GenericMeta(SystemLLMMetaKeys.Strengths, "semantic understanding, enterprise security, high-quality embeddings, data privacy")]
    // [GenericMeta(SystemLLMMetaKeys.BestFor, "semantic search, document similarity, enterprise RAG systems, secure vector operations")]
    // [GenericMeta(SystemLLMMetaKeys.Speed, "fast embedding generation with enterprise features")]
    AzureOpenAIEmbeddings = 3,
    
    [Description("OpenAI embedding models for semantic understanding")]
    // [GenericMeta(SystemLLMMetaKeys.Provider, "openai")]
    // [GenericMeta(SystemLLMMetaKeys.Type, "embedding model")]
    // [GenericMeta(SystemLLMMetaKeys.Strengths, "semantic understanding, high-quality embeddings, versatile text representation")]
    // [GenericMeta(SystemLLMMetaKeys.BestFor, "semantic search, similarity tasks, RAG applications, content recommendation")]
    // [GenericMeta(SystemLLMMetaKeys.Speed, "fast embedding generation")]
    OpenAIEmbeddings = 4
}

public static class SystemLLMMetaKeys
{
    public const string Provider = "provider";
    public const string Type = "type";
    public const string Strengths = "strengths";
    public const string BestFor = "best_for";
    public const string Speed = "speed";
}