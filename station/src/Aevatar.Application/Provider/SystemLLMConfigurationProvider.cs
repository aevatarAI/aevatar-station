using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.Configuration;
using Aevatar.GAgents.AI.Options;
using Aevatar.Options;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Provider;

/// <summary>
/// SystemLLM配置提供者 - 从GAgent获取SystemLLM配置并提供给动态下拉框
/// </summary>
public class SystemLLMConfigurationProvider : DynamicConfigurationProviderBase, ITransientDependency
{
    private const string OPTION_NAME = "SystemLLMConfigs";

    public override async Task ProcessSchemaAsync(ConcurrentDictionary<string, object> concurrentData,
        IClusterClient clusterClient)
    {
        var configurationGAgent = clusterClient.GetGrain<ISchemaConfigurationGAgent>(Guid.NewGuid().ToString());

        var systemLLMOptions = await configurationGAgent.GetConfigOptionsAsync<SystemLLMConfigOptions>();

        // 转换为SystemLLMConfigDto列表
        var aiModelConfigs = ConvertToSystemLLMConfigDtos(systemLLMOptions);

        // 线程安全地插入配置到并发字典中
        concurrentData[OPTION_NAME] = aiModelConfigs;
    }

    private List<SystemLLMConfigDto> ConvertToSystemLLMConfigDtos(SystemLLMConfigOptions options)
    {
        var configDtos = new List<SystemLLMConfigDto>();

        if (options.SystemLLMConfigs != null)
        {
            foreach (var kvp in options.SystemLLMConfigs)
            {
                var config = kvp.Value;
                var configDto = new SystemLLMConfigDto
                {
                    Name = kvp.Key,
                    Provider = config.ProviderEnum.ToString(),
                    Type = config.ModelName,
                    Strengths = new List<string> { $"Provider: {config.ProviderEnum}", $"Model: {config.ModelIdEnum}" },
                    BestFor = new List<string> { "AI chat functionality", "Model inference" },
                    Speed = "Variable"
                };

                configDtos.Add(configDto);
            }
        }

        return configDtos;
    }
}