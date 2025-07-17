using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Service
{
    /// <summary>
    /// Agent索引服务 - 统一的Agent发现、索引和检索服务
    /// </summary>
    public class AgentIndexService : BackgroundService, IAgentIndexService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentIndexService> _logger;
        private readonly ConcurrentDictionary<string, AgentIndexInfo> _agentIndex = new();
        private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
        private DateTime _lastFullScan = DateTime.MinValue;

        public AgentIndexService(
            IServiceProvider serviceProvider,
            ILogger<AgentIndexService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 启动时初始化索引
            await RefreshIndexAsync();

            // 定期刷新索引（每10分钟）
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await RefreshIndexAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "定期刷新Agent索引时发生错误");
                }
            }
        }

        public async Task<IEnumerable<AgentIndexInfo>> GetAllAgentsAsync()
        {
            await EnsureIndexInitialized();
            return _agentIndex.Values.ToList();
        }

        public async Task<AgentIndexInfo?> GetAgentByIdAsync(string agentId)
        {
            await EnsureIndexInitialized();
            return _agentIndex.TryGetValue(agentId, out var agent) ? agent : null;
        }

        public async Task<IEnumerable<AgentIndexInfo>> SearchAgentsAsync(string? query = null, string[]? categories = null, int limit = 50)
        {
            await EnsureIndexInitialized();
            
            var results = _agentIndex.Values.AsQueryable();

            // 关键字过滤
            if (!string.IsNullOrWhiteSpace(query))
            {
                var queryLower = query.ToLowerInvariant();
                results = results.Where(a => 
                    a.Name.ToLowerInvariant().Contains(queryLower) ||
                    a.L1Description.ToLowerInvariant().Contains(queryLower) ||
                    a.L2Description.ToLowerInvariant().Contains(queryLower) ||
                    a.Categories.Any(c => c.ToLowerInvariant().Contains(queryLower)));
            }

            // 分类过滤
            if (categories != null && categories.Length > 0)
            {
                var categorySet = new HashSet<string>(categories, StringComparer.OrdinalIgnoreCase);
                results = results.Where(a => a.Categories.Any(c => categorySet.Contains(c)));
            }

            return results.Take(limit).ToList();
        }

        public async Task<List<AgentIndexInfo>> SearchAgentsAsync(string searchTerm = null, string category = null)
        {
            await EnsureIndexInitialized();
            
            var results = _agentIndex.Values.AsQueryable();

            // 搜索词过滤
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var termLower = searchTerm.ToLowerInvariant();
                results = results.Where(a => 
                    a.Name.ToLowerInvariant().Contains(termLower) ||
                    a.L1Description.ToLowerInvariant().Contains(termLower) ||
                    a.L2Description.ToLowerInvariant().Contains(termLower));
            }

            // 分类过滤
            if (!string.IsNullOrWhiteSpace(category))
            {
                results = results.Where(a => a.Categories.Contains(category, StringComparer.OrdinalIgnoreCase));
            }

            return results.ToList();
        }

        public async Task<AgentIndexRefreshResult> RefreshIndexAsync()
        {
            await _refreshSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("开始刷新Agent索引...");
                var startTime = DateTime.UtcNow;
                var discoveredAgents = new List<AgentIndexInfo>();
                var errors = new List<string>();
                var newAgents = 0;
                var updatedAgents = 0;

                try
                {
                    // 扫描所有程序集中的Agent类
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                        .ToList();

                    foreach (var assembly in assemblies)
                    {
                        try
                        {
                            var agentTypes = assembly.GetTypes()
                                .Where(t => t.GetCustomAttribute<AgentDescriptionAttribute>() != null)
                                .ToList();

                            foreach (var agentType in agentTypes)
                            {
                                try
                                {
                                    var agentInfo = CreateAgentIndexInfo(agentType);
                                    if (agentInfo != null)
                                    {
                                        discoveredAgents.Add(agentInfo);
                                        if (_agentIndex.ContainsKey(agentInfo.TypeName))
                                        {
                                            updatedAgents++;
                                        }
                                        else
                                        {
                                            newAgents++;
                                        }
                                        _agentIndex.AddOrUpdate(agentInfo.TypeName, agentInfo, (k, v) => agentInfo);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var error = $"处理Agent类型 {agentType.FullName} 时发生错误: {ex.Message}";
                                    errors.Add(error);
                                    _logger.LogError(ex, error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = $"扫描程序集 {assembly.FullName} 时发生错误: {ex.Message}";
                            errors.Add(error);
                            _logger.LogError(ex, error);
                        }
                    }

                    _lastFullScan = DateTime.UtcNow;
                    var duration = DateTime.UtcNow - startTime;

                    _logger.LogInformation($"Agent索引刷新完成。发现 {discoveredAgents.Count} 个Agent，耗时 {duration.TotalMilliseconds:F2}ms");

                    return new AgentIndexRefreshResult
                    {
                        Success = true,
                        TotalScanned = discoveredAgents.Count,
                        NewAgents = newAgents,
                        UpdatedAgents = updatedAgents,
                        RefreshDuration = (long)duration.TotalMilliseconds,
                        RefreshTime = _lastFullScan
                    };
                }
                catch (Exception ex)
                {
                    var error = $"Agent索引刷新失败: {ex.Message}";
                    errors.Add(error);
                    _logger.LogError(ex, error);

                    return new AgentIndexRefreshResult
                    {
                        Success = false,
                        TotalScanned = 0,
                        NewAgents = 0,
                        UpdatedAgents = 0,
                        RefreshDuration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                        ErrorMessage = string.Join("; ", errors),
                        RefreshTime = DateTime.UtcNow
                    };
                }
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        public AgentScanStatistics GetScanStatistics()
        {
            var agents = _agentIndex.Values.ToList();
            
            return new AgentScanStatistics
            {
                LastScanTime = _lastFullScan,
                AssembliesScanned = AppDomain.CurrentDomain.GetAssemblies().Length,
                TypesScanned = 0, // 这里可以在扫描时计算
                TotalAgentsFound = agents.Count,
                ScanDuration = TimeSpan.Zero, // 这里可以存储上次扫描的持续时间
                AgentsByCategory = agents
                    .SelectMany(a => a.Categories)
                    .GroupBy(c => c)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ErrorMessages = new List<string>()
            };
        }

        private async Task EnsureIndexInitialized()
        {
            if (_agentIndex.IsEmpty)
            {
                await RefreshIndexAsync();
            }
        }

        private AgentIndexInfo? CreateAgentIndexInfo(Type agentType)
        {
            var attribute = agentType.GetCustomAttribute<AgentDescriptionAttribute>();
            if (attribute == null) return null;

            var agentInfo = new AgentIndexInfo
            {
                Name = attribute.Name,
                TypeName = agentType.FullName ?? agentType.Name,
                L1Description = attribute.L1Description,
                L2Description = attribute.L2Description,
                Categories = attribute.Categories?.ToList() ?? new List<string>(),
                EstimatedExecutionTime = attribute.EstimatedExecutionTime,
                CreatedAt = DateTime.UtcNow,
                LastScannedAt = DateTime.UtcNow
            };

            // 分析输入输出参数
            AnalyzeAgentParameters(agentType, agentInfo);

            return agentInfo;
        }

        private void AnalyzeAgentParameters(Type agentType, AgentIndexInfo agentInfo)
        {
            // 这里可以通过反射分析Agent的方法参数
            // 暂时使用简单的实现
            var methods = agentType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.Contains("Execute") || m.Name.Contains("Process"))
                .ToList();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                foreach (var param in parameters)
                {
                    if (!agentInfo.InputParameters.ContainsKey(param.Name ?? "unknown"))
                    {
                        agentInfo.InputParameters[param.Name ?? "unknown"] = new AgentParameterInfo
                        {
                            Name = param.Name ?? "unknown",
                            Type = param.ParameterType.Name,
                            Description = $"参数 {param.Name}",
                            IsRequired = !param.HasDefaultValue
                        };
                    }
                }

                // 分析返回类型
                if (method.ReturnType != typeof(void) && !agentInfo.OutputParameters.ContainsKey("result"))
                {
                    agentInfo.OutputParameters["result"] = new AgentParameterInfo
                    {
                        Name = "result",
                        Type = method.ReturnType.Name,
                        Description = "执行结果",
                        IsRequired = true
                    };
                }
            }
        }

        public override void Dispose()
        {
            _refreshSemaphore?.Dispose();
            base.Dispose();
        }
    }
} 