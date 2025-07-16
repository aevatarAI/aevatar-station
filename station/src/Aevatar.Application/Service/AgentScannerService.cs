using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Service
{
    /// <summary>
    /// Agent扫描服务实现 - 基于反射自动发现和索引Agent类
    /// </summary>
    public class AgentScannerService : IAgentScannerService
    {
        private readonly ILogger<AgentScannerService> _logger;
        private AgentScanStatistics _lastScanStatistics;

        public AgentScannerService(ILogger<AgentScannerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lastScanStatistics = new AgentScanStatistics();
        }

        /// <summary>
        /// 扫描所有可用的Agent并构建索引信息
        /// </summary>
        public async Task<List<AgentIndexInfo>> ScanAgentsAsync(IEnumerable<Assembly> assembliesToScan = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var statistics = new AgentScanStatistics
            {
                LastScanTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting Agent scanning process...");

                // 确定要扫描的程序集
                var assemblies = assembliesToScan?.ToList() ?? GetDefaultAssembliesToScan();
                statistics.AssembliesScanned = assemblies.Count;

                _logger.LogDebug("Scanning {AssemblyCount} assemblies for Agent classes", assemblies.Count);

                var allAgents = new List<AgentIndexInfo>();

                // 扫描每个程序集
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var agentsInAssembly = await ScanAssemblyAsync(assembly, statistics);
                        allAgents.AddRange(agentsInAssembly);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to scan assembly {AssemblyName}", assembly.FullName);
                        statistics.ErrorMessages.Add($"Assembly scan failed: {assembly.FullName} - {ex.Message}");
                    }
                }

                // 生成统计信息
                statistics.TotalAgentsFound = allAgents.Count;
                statistics.ScanDuration = stopwatch.Elapsed;
                GenerateStatistics(allAgents, statistics);

                _lastScanStatistics = statistics;

                _logger.LogInformation(
                    "Agent scanning completed. Found {AgentCount} agents in {Duration}ms across {AssemblyCount} assemblies",
                    allAgents.Count,
                    stopwatch.ElapsedMilliseconds,
                    assemblies.Count);

                return allAgents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent scanning process failed");
                statistics.ErrorMessages.Add($"Scan process failed: {ex.Message}");
                statistics.ScanDuration = stopwatch.Elapsed;
                _lastScanStatistics = statistics;
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// 从指定类型提取Agent信息
        /// </summary>
        public AgentIndexInfo ExtractAgentInfo(Type agentType)
        {
            if (agentType == null)
                return null;

            try
            {
                var attribute = agentType.GetCustomAttribute<AgentDescriptionAttribute>();
                if (attribute == null)
                    return null;

                // 验证属性有效性
                if (string.IsNullOrEmpty(attribute.AgentId) || string.IsNullOrEmpty(attribute.Name))
                {
                    _logger.LogWarning("Agent {AgentType} has invalid AgentDescriptionAttribute", agentType.Name);
                    return null;
                }

                var agentInfo = new AgentIndexInfo
                {
                    AgentId = attribute.AgentId,
                    Name = attribute.Name,
                    L1Description = attribute.L1Description,
                    L2Description = attribute.L2Description,
                    Categories = attribute.Categories?.ToList() ?? new List<string>(),
                    ComplexityLevel = attribute.ComplexityLevel,
                    EstimatedExecutionTime = attribute.EstimatedExecutionTime,
                    InputParameters = ExtractParameters(agentType, attribute),
                    OutputParameters = new Dictionary<string, AgentParameterInfo>(),
                    Dependencies = new List<string>(),
                    Version = "1.0.0",
                    SupportParallelExecution = attribute.SupportParallelExecution,
                    TypeName = agentType.FullName ?? string.Empty,
                    // 注意：AgentIndexInfo没有AssemblyName属性，这信息存储在TypeName中
                };

                _logger.LogDebug("Extracted agent info for {AgentName} from {AgentType}", agentInfo.Name, agentType.Name);
                return agentInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract agent info from type {AgentType}", agentType.Name);
                return null;
            }
        }

        /// <summary>
        /// 验证类型是否为有效的Agent
        /// </summary>
        public bool IsValidAgent(Type type)
        {
            if (type == null || type.IsAbstract || type.IsInterface)
                return false;

            var attribute = type.GetCustomAttribute<AgentDescriptionAttribute>();
            return attribute != null && !string.IsNullOrEmpty(attribute.AgentId) && !string.IsNullOrEmpty(attribute.Name);
        }

        /// <summary>
        /// 获取扫描统计信息
        /// </summary>
        public AgentScanStatistics GetScanStatistics()
        {
            return _lastScanStatistics;
        }

        /// <summary>
        /// 扫描单个程序集
        /// </summary>
        private async Task<List<AgentIndexInfo>> ScanAssemblyAsync(Assembly assembly, AgentScanStatistics statistics)
        {
            var agents = new List<AgentIndexInfo>();

            try
            {
                var types = assembly.GetTypes();
                statistics.TypesScanned += types.Length;

                foreach (var type in types)
                {
                    try
                    {
                        if (IsValidAgent(type))
                        {
                            var agentInfo = ExtractAgentInfo(type);
                            if (agentInfo != null)
                            {
                                agents.Add(agentInfo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to process type {TypeName} in assembly {AssemblyName}", 
                            type.Name, assembly.GetName().Name);
                        statistics.ErrorMessages.Add($"Type processing failed: {type.Name} - {ex.Message}");
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning(ex, "ReflectionTypeLoadException in assembly {AssemblyName}", assembly.GetName().Name);
                statistics.ErrorMessages.Add($"Type loading failed for assembly: {assembly.GetName().Name}");
                
                // 尝试处理成功加载的类型
                if (ex.Types != null)
                {
                    foreach (var type in ex.Types.Where(t => t != null))
                    {
                        try
                        {
                            if (IsValidAgent(type))
                            {
                                var agentInfo = ExtractAgentInfo(type);
                                if (agentInfo != null)
                                {
                                    agents.Add(agentInfo);
                                }
                            }
                        }
                        catch (Exception typeEx)
                        {
                            _logger.LogDebug(typeEx, "Failed to process partially loaded type {TypeName}", type.Name);
                        }
                    }
                }
            }

            return agents;
        }

        /// <summary>
        /// 获取默认要扫描的程序集列表
        /// </summary>
        private List<Assembly> GetDefaultAssembliesToScan()
        {
            var assemblies = new List<Assembly>();

            try
            {
                // 获取当前应用域中所有已加载的程序集
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => !assembly.IsDynamic)
                    .ToList();

                // 过滤掉系统程序集，只扫描应用程序相关的程序集
                foreach (var assembly in loadedAssemblies)
                {
                    var assemblyName = assembly.GetName().Name;
                    
                    // 包含Aevatar相关的程序集
                    if (assemblyName.StartsWith("Aevatar", StringComparison.OrdinalIgnoreCase) ||
                        assemblyName.StartsWith("Microsoft.Orleans", StringComparison.OrdinalIgnoreCase) ||
                        (!assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                         !assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                         !assemblyName.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase)))
                    {
                        assemblies.Add(assembly);
                    }
                }

                _logger.LogDebug("Selected {AssemblyCount} assemblies for Agent scanning", assemblies.Count);
                return assemblies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get default assemblies to scan");
                return new List<Assembly>();
            }
        }

        /// <summary>
        /// 生成Agent唯一标识符
        /// </summary>
        private string GenerateAgentId(Type agentType)
        {
            // 使用类型全名和程序集名称生成稳定的ID
            var assemblyName = agentType.Assembly.GetName().Name;
            return $"{assemblyName}.{agentType.FullName}".Replace(" ", "").Replace(".", "_");
        }

        /// <summary>
        /// 提取Agent参数信息
        /// </summary>
        private Dictionary<string, AgentParameterInfo> ExtractParameters(Type agentType, AgentDescriptionAttribute attribute)
        {
            var parameters = new Dictionary<string, AgentParameterInfo>();

            // TODO: 实现参数提取逻辑
            // 可以从方法签名、属性等自动提取参数信息
            // 这里可以根据具体的Agent接口规范来实现参数自动发现

            return parameters;
        }

        /// <summary>
        /// 生成详细统计信息
        /// </summary>
        private void GenerateStatistics(List<AgentIndexInfo> agents, AgentScanStatistics statistics)
        {
            // 按分类统计
            statistics.AgentsByCategory = agents
                .SelectMany(a => a.Categories.DefaultIfEmpty("Uncategorized"))
                .GroupBy(category => category)
                .ToDictionary(g => g.Key, g => g.Count());

            // 按复杂度统计
            statistics.AgentsByComplexity = agents
                .GroupBy(a => a.ComplexityLevel)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
} 