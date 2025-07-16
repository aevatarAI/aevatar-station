using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Service
{
    /// <summary>
    /// Agent索引池服务实现 - 负责Agent信息的缓存管理和快速检索
    /// </summary>
    public class AgentIndexPoolService : IAgentIndexPool
    {
        private readonly IAgentScannerService _agentScannerService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<AgentIndexPoolService> _logger;
        
        private readonly ConcurrentDictionary<string, AgentIndexInfo> _agentIndex;
        private readonly SemaphoreSlim _refreshSemaphore;
        private readonly string CacheKey = "AgentIndexPool_AllAgents";
        private volatile bool _isInitialized = false;
        private DateTime _lastRefreshTime = DateTime.MinValue;

        public AgentIndexPoolService(
            IAgentScannerService agentScannerService,
            IMemoryCache memoryCache,
            ILogger<AgentIndexPoolService> logger)
        {
            _agentScannerService = agentScannerService ?? throw new ArgumentNullException(nameof(agentScannerService));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _agentIndex = new ConcurrentDictionary<string, AgentIndexInfo>();
            _refreshSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// 获取所有可用的Agent信息
        /// </summary>
        public async Task<IEnumerable<AgentIndexInfo>> GetAllAgentsAsync()
        {
            await EnsureInitializedAsync();
            return _agentIndex.Values.Where(agent => !string.IsNullOrEmpty(agent.AgentId)).ToList();
        }

        /// <summary>
        /// 根据ID获取特定Agent信息
        /// </summary>
        public async Task<AgentIndexInfo?> GetAgentByIdAsync(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
                return null;

            await EnsureInitializedAsync();
            _agentIndex.TryGetValue(agentId, out var agent);
            return agent;
        }

        /// <summary>
        /// 根据ID查找特定Agent信息（别名方法）
        /// </summary>
        public async Task<AgentIndexInfo?> FindByIdAsync(string agentId)
        {
            return await GetAgentByIdAsync(agentId);
        }

        /// <summary>
        /// 搜索Agent（支持关键词、分类过滤）
        /// </summary>
        public async Task<IEnumerable<AgentIndexInfo>> SearchAgentsAsync(string? query = null, string[]? categories = null, int limit = 50)
        {
            await EnsureInitializedAsync();
            
            var agents = _agentIndex.Values.Where(agent => !string.IsNullOrEmpty(agent.AgentId));

            // 按搜索词过滤
            if (!string.IsNullOrEmpty(query))
            {
                var lowerSearchTerm = query.ToLower();
                agents = agents.Where(agent =>
                    agent.Name.ToLower().Contains(lowerSearchTerm) ||
                    agent.L1Description.ToLower().Contains(lowerSearchTerm) ||
                    agent.L2Description.ToLower().Contains(lowerSearchTerm) ||
                    agent.Categories.Any(category => category.ToLower().Contains(lowerSearchTerm)));
            }

            // 按分类过滤
            if (categories != null && categories.Length > 0)
            {
                agents = agents.Where(agent => 
                    categories.Any(cat => agent.Categories.Any(agentCategory => 
                        string.Equals(agentCategory, cat, StringComparison.OrdinalIgnoreCase))));
            }

            var result = agents.Take(limit).ToList();
            
            _logger.LogDebug("Agent search completed. Query: '{Query}', Categories: '{Categories}', Results: {Count}",
                query, categories != null ? string.Join(",", categories) : "null", result.Count);

            return result;
        }

        /// <summary>
        /// 搜索符合条件的Agent（扩展方法）
        /// </summary>
        public async Task<List<AgentIndexInfo>> SearchAgentsAsync(string searchTerm = null, string category = null, int? complexityLevel = null)
        {
            await EnsureInitializedAsync();
            
            var agents = _agentIndex.Values.Where(agent => !string.IsNullOrEmpty(agent.AgentId));

            // 按搜索词过滤
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                agents = agents.Where(agent =>
                    agent.Name.ToLower().Contains(lowerSearchTerm) ||
                    agent.L1Description.ToLower().Contains(lowerSearchTerm) ||
                    agent.L2Description.ToLower().Contains(lowerSearchTerm) ||
                    agent.Categories.Any(category => category.ToLower().Contains(lowerSearchTerm)));
            }

            // 按分类过滤
            if (!string.IsNullOrEmpty(category))
            {
                agents = agents.Where(agent => 
                    agent.Categories.Any(cat => string.Equals(cat, category, StringComparison.OrdinalIgnoreCase)));
            }

            // 按复杂度过滤
            if (complexityLevel.HasValue)
            {
                agents = agents.Where(agent => agent.ComplexityLevel == complexityLevel.Value);
            }

            var result = agents.ToList();
            
            _logger.LogDebug("Agent search completed. Query: '{SearchTerm}', Category: '{Category}', Complexity: '{Complexity}', Results: {Count}",
                searchTerm, category, complexityLevel, result.Count);

            return result;
        }

        /// <summary>
        /// 刷新Agent索引
        /// </summary>
        public async Task<AgentIndexRefreshResult> RefreshIndexAsync()
        {
            var startTime = DateTime.UtcNow;
            await _refreshSemaphore.WaitAsync();
            
            try
            {
                _logger.LogInformation("Starting Agent index refresh...");

                var oldCount = _agentIndex.Count;
                var agents = await _agentScannerService.ScanAgentsAsync();
                
                // 清空当前索引
                _agentIndex.Clear();
                
                // 重新构建索引
                foreach (var agent in agents)
                {
                    _agentIndex.TryAdd(agent.AgentId, agent);
                }

                // 更新缓存
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(1),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                    Priority = CacheItemPriority.High
                };
                
                _memoryCache.Set(CacheKey, agents, cacheOptions);
                
                _lastRefreshTime = DateTime.UtcNow;
                _isInitialized = true;

                _logger.LogInformation("Agent index refresh completed. Total agents: {AgentCount}", agents.Count);

                // 记录统计信息
                LogIndexStatistics();

                var endTime = DateTime.UtcNow;
                return new AgentIndexRefreshResult
                {
                    Success = true,
                    TotalScanned = agents.Count,
                    NewAgents = Math.Max(0, agents.Count - oldCount),
                    UpdatedAgents = Math.Min(oldCount, agents.Count),
                    RefreshDuration = (long)(endTime - startTime).TotalMilliseconds,
                    RefreshTime = endTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh Agent index");
                var endTime = DateTime.UtcNow;
                return new AgentIndexRefreshResult
                {
                    Success = false,
                    TotalScanned = 0,
                    NewAgents = 0,
                    UpdatedAgents = 0,
                    RefreshDuration = (long)(endTime - startTime).TotalMilliseconds,
                    ErrorMessage = ex.Message,
                    RefreshTime = endTime
                };
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        /// <summary>
        /// 确保索引已初始化
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            await _refreshSemaphore.WaitAsync();
            try
            {
                // 双重检查锁定模式
                if (_isInitialized)
                    return;

                _logger.LogInformation("Initializing Agent index pool for the first time...");

                // 尝试从缓存加载
                if (_memoryCache.TryGetValue(CacheKey, out List<AgentIndexInfo> cachedAgents))
                {
                    _logger.LogDebug("Loading agents from cache. Count: {CachedAgentCount}", cachedAgents.Count);
                    
                    foreach (var agent in cachedAgents)
                    {
                        _agentIndex.TryAdd(agent.AgentId, agent);
                    }
                    
                    _lastRefreshTime = DateTime.UtcNow;
                    _isInitialized = true;
                    
                    _logger.LogInformation("Agent index initialized from cache. Total agents: {AgentCount}", cachedAgents.Count);
                }
                else
                {
                    // 缓存未命中，执行完整的扫描和索引构建
                    await RefreshIndexInternalAsync();
                }
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        /// <summary>
        /// 内部刷新方法（无锁保护）
        /// </summary>
        private async Task RefreshIndexInternalAsync()
        {
            _logger.LogInformation("Starting full Agent index scan...");

            var agents = await _agentScannerService.ScanAgentsAsync();
            
            // 清空当前索引
            _agentIndex.Clear();
            
            // 重新构建索引
            foreach (var agent in agents)
            {
                _agentIndex.TryAdd(agent.AgentId, agent);
            }

            // 更新缓存
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(1),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                Priority = CacheItemPriority.High
            };
            
            _memoryCache.Set(CacheKey, agents, cacheOptions);
            
            _lastRefreshTime = DateTime.UtcNow;
            _isInitialized = true;

            _logger.LogInformation("Full Agent index scan completed. Total agents: {AgentCount}", agents.Count);
        }

        /// <summary>
        /// 记录索引统计信息
        /// </summary>
        private void LogIndexStatistics()
        {
            try
            {
                var totalAgents = _agentIndex.Count;
                var activeAgents = _agentIndex.Values.Count(a => !string.IsNullOrEmpty(a.AgentId));
                var inactiveAgents = totalAgents - activeAgents;

                var categoryStats = _agentIndex.Values
                    .Where(a => !string.IsNullOrEmpty(a.AgentId))
                    .SelectMany(a => a.Categories.DefaultIfEmpty("Uncategorized"))
                    .GroupBy(category => category)
                    .ToDictionary(g => g.Key, g => g.Count());

                var complexityStats = _agentIndex.Values
                    .Where(a => !string.IsNullOrEmpty(a.AgentId))
                    .GroupBy(a => a.ComplexityLevel)
                    .ToDictionary(g => g.Key, g => g.Count());

                _logger.LogInformation(
                    "Agent Index Statistics - Total: {Total}, Active: {Active}, Inactive: {Inactive}, " +
                    "Categories: {CategoryCount}, Last Refresh: {LastRefresh}",
                    totalAgents, activeAgents, inactiveAgents, categoryStats.Count, _lastRefreshTime);

                _logger.LogDebug("Category Distribution: {CategoryStats}", 
                    string.Join(", ", categoryStats.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

                _logger.LogDebug("Complexity Distribution: {ComplexityStats}", 
                    string.Join(", ", complexityStats.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log index statistics");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _refreshSemaphore?.Dispose();
        }
    }
} 