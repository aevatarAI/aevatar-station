using System.Collections.Generic;
using Aevatar.GAgents.AI.Options;
using Orleans;

namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// Base class for AI model configurations
/// Focuses solely on model parameters and behavior
/// Uses existing ModelIdEnum for compatibility
/// </summary>
[GenerateSerializer]
public abstract class ModelConfiguration : ILLMModelConfig
{
    /// <summary>
    /// The AI model type (uses existing enum for compatibility)
    /// </summary>
    public abstract ModelIdEnum Model { get; }
    
    /// <summary>
    /// Human-readable description of this model configuration
    /// </summary>
    [Id(0)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets model-specific parameters for API calls
    /// </summary>
    public abstract Dictionary<string, object> GetParameters();
    
    /// <summary>
    /// Gets provider-specific model identifier (deployment name, model ID, etc.)
    /// </summary>
    public abstract string GetModelIdentifier();
    
    /// <summary>
    /// Validates model-specific configuration
    /// </summary>
    public abstract bool IsValid();
}

/// <summary>
/// OpenAI model configuration (ModelIdEnum.OpenAI)
/// </summary>
[GenerateSerializer]
public class OpenAIModelConfiguration : ModelConfiguration
{
    public override ModelIdEnum Model => ModelIdEnum.OpenAI;
    
    /// <summary>
    /// Sampling temperature (0.0 to 2.0)
    /// </summary>
    [Id(1)] public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    [Id(2)] public int MaxTokens { get; set; } = 4000;
    
    /// <summary>
    /// Specific OpenAI model name (gpt-4, gpt-3.5-turbo, etc.)
    /// </summary>
    [Id(3)] public string ModelName { get; set; } = "gpt-4";
    
    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            ["temperature"] = Temperature,
            ["max_tokens"] = MaxTokens,
            ["model"] = ModelName
        };
    }
    
    public override string GetModelIdentifier() => ModelName;
    
    public override bool IsValid()
    {
        return Temperature >= 0 && Temperature <= 2 && MaxTokens > 0 && !string.IsNullOrEmpty(ModelName);
    }
}

/// <summary>
/// DeepSeek model configuration (ModelIdEnum.DeepSeek)
/// </summary>
[GenerateSerializer]
public class DeepSeekModelConfiguration : ModelConfiguration
{
    public override ModelIdEnum Model => ModelIdEnum.DeepSeek;
    
    /// <summary>
    /// Sampling temperature
    /// </summary>
    [Id(1)] public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    [Id(2)] public int MaxTokens { get; set; } = 4000;
    
    /// <summary>
    /// DeepSeek model name
    /// </summary>
    [Id(3)] public string ModelName { get; set; } = "deepseek-chat";
    
    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            ["temperature"] = Temperature,
            ["max_tokens"] = MaxTokens,
            ["model"] = ModelName
        };
    }
    
    public override string GetModelIdentifier() => ModelName;
    
    public override bool IsValid()
    {
        return Temperature >= 0 && Temperature <= 2 && MaxTokens > 0;
    }
}

/// <summary>
/// Gemini model configuration (ModelIdEnum.Gemini)
/// </summary>
[GenerateSerializer]
public class GeminiModelConfiguration : ModelConfiguration
{
    public override ModelIdEnum Model => ModelIdEnum.Gemini;
    
    /// <summary>
    /// Sampling temperature
    /// </summary>
    [Id(1)] public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    [Id(2)] public int MaxTokens { get; set; } = 4000;
    
    /// <summary>
    /// Gemini model name
    /// </summary>
    [Id(3)] public string ModelName { get; set; } = "gemini-pro";
    
    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            ["temperature"] = Temperature,
            ["maxOutputTokens"] = MaxTokens,
            ["model"] = ModelName
        };
    }
    
    public override string GetModelIdentifier() => ModelName;
    
    public override bool IsValid()
    {
        return Temperature >= 0 && Temperature <= 1 && MaxTokens > 0;
    }
}

/// <summary>
/// OpenAI Text-to-Image model configuration (ModelIdEnum.OpenAITextToImage)
/// </summary>
[GenerateSerializer]
public class OpenAITextToImageModelConfiguration : ModelConfiguration
{
    public override ModelIdEnum Model => ModelIdEnum.OpenAITextToImage;
    
    /// <summary>
    /// Image size (256x256, 512x512, 1024x1024)
    /// </summary>
    [Id(1)] public string Size { get; set; } = "1024x1024";
    
    /// <summary>
    /// Number of images to generate
    /// </summary>
    [Id(2)] public int NumberOfImages { get; set; } = 1;
    
    /// <summary>
    /// Image quality (standard, hd)
    /// </summary>
    [Id(3)] public string Quality { get; set; } = "standard";
    
    /// <summary>
    /// DALL-E model version
    /// </summary>
    [Id(4)] public string ModelName { get; set; } = "dall-e-3";
    
    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            ["size"] = Size,
            ["n"] = NumberOfImages,
            ["quality"] = Quality,
            ["model"] = ModelName
        };
    }
    
    public override string GetModelIdentifier() => ModelName;
    
    public override bool IsValid()
    {
        return NumberOfImages > 0 && NumberOfImages <= 10 && 
               (Size == "256x256" || Size == "512x512" || Size == "1024x1024");
    }
}

/// <summary>
/// BytePlus Video Generation model configuration (ModelIdEnum.BytePlusVideoGeneration)
/// </summary>
[GenerateSerializer]
public class BytePlusVideoGenerationModelConfiguration : ModelConfiguration
{
    public override ModelIdEnum Model => ModelIdEnum.BytePlusVideoGeneration;
    
    /// <summary>
    /// Video duration in seconds
    /// </summary>
    [Id(1)] public int Duration { get; set; } = 10;
    
    /// <summary>
    /// Video resolution
    /// </summary>
    [Id(2)] public string Resolution { get; set; } = "720p";
    
    /// <summary>
    /// Frame rate
    /// </summary>
    [Id(3)] public int FrameRate { get; set; } = 30;
    
    /// <summary>
    /// BytePlus model name
    /// </summary>
    [Id(4)] public string ModelName { get; set; } = "video-generation-v1";
    
    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            ["duration"] = Duration,
            ["resolution"] = Resolution,
            ["frame_rate"] = FrameRate,
            ["model"] = ModelName
        };
    }
    
    public override string GetModelIdentifier() => ModelName;
    
    public override bool IsValid()
    {
        return Duration > 0 && Duration <= 60 && FrameRate > 0;
    }
}