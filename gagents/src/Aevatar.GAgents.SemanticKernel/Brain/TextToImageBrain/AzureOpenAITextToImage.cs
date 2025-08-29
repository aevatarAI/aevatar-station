using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToImage;
using Newtonsoft.Json;
using OpenAI.Images;

namespace Aevatar.AI.Brain.TextToImageBrain;

public class AzureOpenAITextToImage : ITextToImageBrain
{
    public LLMProviderEnum ProviderEnum => LLMProviderEnum.Azure;
    public ModelIdEnum ModelIdEnum => ModelIdEnum.OpenAITextToImage;

    protected Kernel? Kernel;

    public Task InitializeAsync(LLMConfig llmConfig, string id, string description)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        var clientOptions = new AzureOpenAIClientOptions()
        {
            NetworkTimeout = TimeSpan.FromSeconds(llmConfig.NetworkTimeoutInSeconds)
        };

        var azureOpenAi = new AzureOpenAIClient(
            new Uri(llmConfig.Endpoint),
            new AzureKeyCredential(llmConfig.ApiKey),
            clientOptions
        );

        var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(llmConfig.NetworkTimeoutInSeconds);
        httpClient.BaseAddress = new Uri(llmConfig.Endpoint);

        kernelBuilder.AddAzureOpenAITextToImage(llmConfig.ModelName, azureOpenAi);
        Kernel = kernelBuilder.Build();

        return Task.CompletedTask;
    }

    public Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null)
    {
        return Task.FromResult(true);
    }

    public async Task<List<TextToImageResponse>?> GenerateTextToImageAsync(string prompt, TextToImageOption option,
        CancellationToken cancellationToken = default)
    {
        if (Kernel == null)
        {
            return null;
        }

        var result = new List<TextToImageResponse>();
        var textToImage = Kernel.GetRequiredService<ITextToImageService>();
        var executeSettings = ConvertToOpenAITextContent(option);
        var llmResponse = await textToImage.GetImageContentsAsync(
            new TextContent(prompt), executeSettings, cancellationToken: cancellationToken);
        foreach (var item in llmResponse)
        {
            var response = new TextToImageResponse();
            var innerContent = item.InnerContent as GeneratedImage;
            if (innerContent == null)
            {
                throw new ArgumentException("[GenerateTextToImageAsync] item.InnerContent as GeneratedImage == null");
            }

            if (innerContent.ImageBytes is { IsEmpty: false })
            {
                response.ResponseType = TextToImageResponseType.Base64Content;
                response.Base64Content = innerContent.ImageBytes.ToString();
                response.ImageType = item.MimeType;

                result.Add(response);
                continue;
            }

            var url = innerContent.ImageUri == null ? null : innerContent.ImageUri.ToString();
            if (url.IsNullOrEmpty() == false)
            {
                response.ResponseType = TextToImageResponseType.Url;
                response.Url = url;
                result.Add(response);
                continue;
            }

            throw new UnsupportedContentTypeException(
                $"[GenerateTextToImageAsync] can not handle data:{JsonConvert.SerializeObject(item)} ");
        }

        return result;
    }

    private OpenAITextToImageExecutionSettings ConvertToOpenAITextContent(TextToImageOption option)
    {
        var result = new OpenAITextToImageExecutionSettings();

        result.ModelId = option.ModelId;
        result.Size = (option.With, option.Height);
        result.ResponseFormat = option.ResponseType == TextToImageResponseType.Url
            ? GeneratedImageFormat.Uri
            : GeneratedImageFormat.Bytes;
        result.Style = option.StyleEnum == TextToImageStyleEnum.Vivid ? "VIVID" : "NATURAL";
        result.Quality = option.QualityEnum == TextToImageQualityEnum.Standard ? "STANDARD" : "HIGH";

        return result;
    }
}