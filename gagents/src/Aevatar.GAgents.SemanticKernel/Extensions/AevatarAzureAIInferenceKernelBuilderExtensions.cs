using System;
using System.Net.Http;
using Azure.AI.Inference;
using Azure.Core.Pipeline;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aevatar.AI.Extensions;

public static class AevatarAzureAIInferenceKernelBuilderExtensions
{
    public static IKernelBuilder AddAzureAIInferenceChatCompletion(
        this IKernelBuilder builder,
        string modelId,
        AzureAIInferenceClientOptions options,
        string? apiKey = null,
        Uri? endpoint = null,
        HttpClient? httpClient = null,
        string? serviceId = null)
    {
        if(builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddAzureAIInferenceChatCompletion(modelId, options, apiKey, endpoint, httpClient, serviceId);

        return builder;
    }
    
    public static IServiceCollection AddAzureAIInferenceChatCompletion(
        this IServiceCollection services,
        string modelId,
        AzureAIInferenceClientOptions options,
        string? apiKey = null,
        Uri? endpoint = null,
        HttpClient? httpClient = null,
        string? serviceId = null)
    {
        if(services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.AddKeyedSingleton<IChatCompletionService>(serviceId, (serviceProvider, _) =>
        {
            httpClient ??= serviceProvider.GetService<HttpClient>();
            if (httpClient is not null)
            {
                options.Transport = new HttpClientTransport(httpClient);
            }

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            var builder = new Azure.AI.Inference.ChatCompletionsClient(endpoint, new Azure.AzureKeyCredential(apiKey ?? SingleSpace), options)
                .AsIChatClient(modelId)
                .AsBuilder()
                .UseFunctionInvocation(loggerFactory, f => f.MaximumIterationsPerRequest = MaxInflightAutoInvokes);
            
            if (loggerFactory is not null)
            {
                builder.UseLogging(loggerFactory);
            }

            return builder.Build(serviceProvider).AsChatCompletionService(serviceProvider);
        });
    }
    
    private const int MaxInflightAutoInvokes = 128;
    private const string SingleSpace = " ";
}