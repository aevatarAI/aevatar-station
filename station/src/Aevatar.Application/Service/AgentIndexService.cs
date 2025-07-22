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
    /// Agent index service - unified service for Agent discovery, indexing, and retrieval
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
            // Initialize index on startup
            await RefreshIndexAsync();

            // Refresh index periodically (every 10 minutes)
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await RefreshIndexAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during periodic Agent index refresh");
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

            // Keyword filtering
            if (!string.IsNullOrWhiteSpace(query))
            {
                var queryLower = query.ToLowerInvariant();
                results = results.Where(a => 
                    a.Name.ToLowerInvariant().Contains(queryLower) ||
                    a.L1Description.ToLowerInvariant().Contains(queryLower) ||
                    a.L2Description.ToLowerInvariant().Contains(queryLower) ||
                    a.Categories.Any(c => c.ToLowerInvariant().Contains(queryLower)));
            }

            // Category filtering
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

            // Search term filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var termLower = searchTerm.ToLowerInvariant();
                results = results.Where(a => 
                    a.Name.ToLowerInvariant().Contains(termLower) ||
                    a.L1Description.ToLowerInvariant().Contains(termLower) ||
                    a.L2Description.ToLowerInvariant().Contains(termLower));
            }

            // Category filtering
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
                _logger.LogInformation("Starting Agent index refresh...");
                var startTime = DateTime.UtcNow;
                var discoveredAgents = new List<AgentIndexInfo>();
                var errors = new List<string>();
                var newAgents = 0;
                var updatedAgents = 0;

                try
                {
                    // Scan all assemblies for Agent classes
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
                                    var error = $"Error occurred while processing Agent type {agentType.FullName}: {ex.Message}";
                                    errors.Add(error);
                                    _logger.LogError(ex, error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = $"Error occurred while scanning assembly {assembly.FullName}: {ex.Message}";
                            errors.Add(error);
                            _logger.LogError(ex, error);
                        }
                    }

                    _lastFullScan = DateTime.UtcNow;
                    var duration = DateTime.UtcNow - startTime;

                    _logger.LogInformation($"Agent index refresh completed. Found {discoveredAgents.Count} Agents, took {duration.TotalMilliseconds:F2}ms");

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
                    var error = $"Agent index refresh failed: {ex.Message}";
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
                TypesScanned = 0, // This can be calculated during scanning
                TotalAgentsFound = agents.Count,
                ScanDuration = TimeSpan.Zero, // This can store the last scan duration
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

            // Analyze input and output parameters
            AnalyzeAgentParameters(agentType, agentInfo);

            return agentInfo;
        }

        private void AnalyzeAgentParameters(Type agentType, AgentIndexInfo agentInfo)
        {
            // Agent method parameters can be analyzed through reflection here
            // Using simple implementation for now
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
                            Description = $"Parameter {param.Name}",
                            IsRequired = !param.HasDefaultValue
                        };
                    }
                }

                // Analyze return type
                if (method.ReturnType != typeof(void) && !agentInfo.OutputParameters.ContainsKey("result"))
                {
                    agentInfo.OutputParameters["result"] = new AgentParameterInfo
                    {
                        Name = "result",
                        Type = method.ReturnType.Name,
                        Description = "Execution result",
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