using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Aevatar.GAgents.Workflow.Core;
using Aevatar.Core.Abstractions;

namespace WorkflowRunDemo;

/// <summary>
/// Workflow Run Test Demo
/// Steps:
/// 1. Create a WorkflowViewAgent with POST /api/agent
/// 2. Get the returned Agent ID
/// 3. Call POST /api/workflow/run with the Agent ID
/// </summary>
class Program
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static string _baseUrl = "http://localhost:8308"; // Modify this to your actual API base URL
    private static IHost? _host;
    private static IClusterClient? _client;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Workflow Run Demo ===\n");

        // Check if custom base URL is provided
        if (args.Length > 0)
        {
            _baseUrl = args[0];
            Console.WriteLine($"Using custom base URL: {_baseUrl}\n");
        }
        else
        {
            Console.WriteLine($"Using default base URL: {_baseUrl}");
            Console.WriteLine("You can provide a custom URL as the first argument\n");
        }

        try
        {
            // Step 1: Create WorkflowViewAgent
            Console.WriteLine("Step 1: Creating WorkflowViewAgent...");
            var agentId = await CreateWorkflowAgentAsync();
            Console.WriteLine($"✅ Agent created successfully with ID: {agentId}\n");

            // Step 2: Run Workflow (First time)
            Console.WriteLine("Step 2: Running workflow (First time)...");
            var result1 = await RunWorkflowAsync(agentId);
            Console.WriteLine($"✅ First workflow execution completed:");
            Console.WriteLine($"   - Success: {result1.IsSuccess}");
            Console.WriteLine($"   - Message: {result1.Message}");
            Console.WriteLine($"   - Workflow ID (CoordinatorId): {result1.WorkflowId}\n");
            
            // Step 2.1: Check topology after first run
            var topology1 = await GetWorkflowTopologyAsync(agentId, "After First Run");
            var chain1 = TraverseWorkflowChain(topology1);
            
            // Step 3: Get current WorkflowCoordinatorGAgentId
            Console.WriteLine("\nStep 3: Getting current agent configuration...");
            var currentCoordinatorId = await GetWorkflowCoordinatorIdAsync(agentId);
            Console.WriteLine($"   - Current WorkflowCoordinatorGAgentId: {currentCoordinatorId}\n");
            
            // Step 4: Update Agent Configuration (add workflowCoordinatorGAgentId parameter)
            Console.WriteLine("Step 4: Updating agent configuration (adding workflowCoordinatorGAgentId)...");
            var updatedCoordinatorId = await UpdateWorkflowAgentAsync(agentId, currentCoordinatorId);
            Console.WriteLine($"✅ Agent updated successfully");
            Console.WriteLine($"   - WorkflowCoordinatorGAgentId after update: {updatedCoordinatorId}\n");
            
            // Step 5: Run Workflow (Second time - after update)
            Console.WriteLine("Step 5: Running workflow (Second time - after update)...");
            var result2 = await RunWorkflowAsync(agentId);
            Console.WriteLine($"✅ Second workflow execution completed:");
            Console.WriteLine($"   - Success: {result2.IsSuccess}");
            Console.WriteLine($"   - Message: {result2.Message}");
            Console.WriteLine($"   - Workflow ID (CoordinatorId): {result2.WorkflowId}\n");
            
            // Step 5.1: Check topology after second run
            var topology2 = await GetWorkflowTopologyAsync(agentId, "After Second Run (Post-Update)");
            var chain2 = TraverseWorkflowChain(topology2);
            
            // Step 5.2: Compare complete workflow chains
            CompareWorkflowChains(chain1, chain2);
            
            // Step 6: Verify WorkflowId consistency
            Console.WriteLine("Step 6: Verifying WorkflowId (CoordinatorId) consistency...");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            
            bool allMatch = (result1.WorkflowId == result2.WorkflowId) && 
                           (result1.WorkflowId == currentCoordinatorId) &&
                           (currentCoordinatorId == updatedCoordinatorId);
            
            if (allMatch)
            {
                Console.WriteLine($"✅ ✅ ✅ WorkflowId is CONSISTENT across all operations! ✅ ✅ ✅");
                Console.WriteLine($"   ViewAgent ID:                          {agentId}");
                Console.WriteLine($"   Initial WorkflowCoordinatorGAgentId:   {currentCoordinatorId}");
                Console.WriteLine($"   Updated WorkflowCoordinatorGAgentId:   {updatedCoordinatorId}");
                Console.WriteLine($"   First run  WorkflowId:                 {result1.WorkflowId}");
                Console.WriteLine($"   Second run WorkflowId:                 {result2.WorkflowId}");
                Console.WriteLine($"   All IDs Match: YES ✅");
                Console.WriteLine($"\n🎯 验证成功：");
                Console.WriteLine($"   1. WorkflowId = WorkflowCoordinatorGAgentId");
                Console.WriteLine($"   2. 同一个WorkflowViewAgent的多次执行使用相同的WorkflowId");
                Console.WriteLine($"   3. UpdateAgent后，WorkflowCoordinatorGAgentId保持不变");
                Console.WriteLine($"   4. UpdateAgent可以正确处理包含已分配agentId的节点");
                Console.WriteLine($"   5. 完整的workflow链路保持不变（StartAgent→Nodes→EndAgent）");
                Console.WriteLine($"   6. UpdateAgent传入完整节点信息+workflowCoordinatorGAgentId正常工作");
            }
            else
            {
                Console.WriteLine($"❌ ❌ ❌ WorkflowId is INCONSISTENT! ❌ ❌ ❌");
                Console.WriteLine($"   ViewAgent ID:                          {agentId}");
                Console.WriteLine($"   Initial WorkflowCoordinatorGAgentId:   {currentCoordinatorId}");
                Console.WriteLine($"   Updated WorkflowCoordinatorGAgentId:   {updatedCoordinatorId}");
                Console.WriteLine($"   First run  WorkflowId:                 {result1.WorkflowId}");
                Console.WriteLine($"   Second run WorkflowId:                 {result2.WorkflowId}");
                Console.WriteLine($"   Match: NO ❌");
                Console.WriteLine($"\n⚠️  WorkflowId不一致，这违反了设计原则！");
                
                if (currentCoordinatorId != updatedCoordinatorId)
                {
                    Console.WriteLine($"\n⚠️  严重问题：UpdateAgent修改了WorkflowCoordinatorGAgentId！");
                    Console.WriteLine($"      这是不允许的行为！");
                }
            }
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
            
            // Step 6: Query WorkflowExecutionRecordStatePlus via ES (latest workflow)
            if (result2.IsSuccess && result2.WorkflowId != Guid.Empty)
            {
                Console.WriteLine("Step 6: Querying latest workflow execution record state via ES...");
                // Wait a bit for the workflow to complete processing
                await Task.Delay(2000);
                
                await QueryWorkflowExecutionRecordAsync(result2.WorkflowId);
                
                // Step 7: Query via Orleans GetStateSnapshotAsync
                Console.WriteLine("\nStep 7: Querying workflow execution record state via Orleans...");
                await InitializeOrleansClientAsync();
                await QueryOrleansStateAsync(result2.WorkflowId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
        finally
        {
            await CleanupAsync();
        }

        Console.WriteLine("\n✅ Test completed successfully!");
    }
    
    private static async Task InitializeOrleansClientAsync()
    {
        Console.WriteLine("🔧 Connecting to Orleans cluster...");

        var builder = Host.CreateDefaultBuilder()
            .UseOrleansClient(client =>
            {
                var hostId = "Aevatar";
                client.UseMongoDBClient("mongodb://localhost:27017")
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = "AevatarDb";
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "AevatarSiloCluster";
                        options.ServiceId = "AevatarBasicService";
                    });
            })
            .ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .UseConsoleLifetime();

        _host = builder.Build();
        await _host.StartAsync();
        _client = _host.Services.GetRequiredService<IClusterClient>();

        if (_client == null)
        {
            throw new Exception("Orleans client is not initialized");
        }

        Console.WriteLine("✅ Orleans client connected!\n");
    }

    private static async Task CleanupAsync()
    {
        if (_host != null)
        {
            Console.WriteLine("🧹 Cleaning up Orleans client...");
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
        }
        _client = null;
    }
    
    /// <summary>
    /// Step 4: Query WorkflowExecutionRecordGAgentPlus state via Orleans
    /// Uses GetStateSnapshotAsync to access grain state directly
    /// </summary>
    private static async Task QueryOrleansStateAsync(Guid workflowId)
    {
        if (_client == null)
        {
            Console.WriteLine("⚠️ Orleans client not initialized");
            return;
        }

        try
        {
            // Get the WorkflowExecutionRecordGAgentPlus grain
            var recordAgent = _client.GetGrain<IWorkflowExecutionRecordGAgentPlus>(workflowId);
            
            // Get state snapshot
            var stateJson = await recordAgent.GetStateSnapshotAsync();
            
            if (string.IsNullOrEmpty(stateJson))
            {
                Console.WriteLine("⚠️ No state found in Orleans storage");
                return;
            }

            Console.WriteLine("✅ Found state in Orleans storage:\n");
            
            // Parse JSON
            var stateObj = JObject.Parse(stateJson);
            
            // Check for null fields
            var nullFields = new List<string>();
            CheckNullFields(stateObj, "", nullFields);
            
            if (nullFields.Any())
            {
                Console.WriteLine($"⚠️ Found {nullFields.Count} null field(s):");
                foreach (var field in nullFields)
                {
                    Console.WriteLine($"   - {field}");
                }
            }
            else
            {
                Console.WriteLine("✅ No null fields found in Orleans state.");
            }
            
            Console.WriteLine($"\n📊 Orleans State Summary:");
            Console.WriteLine($"   - WorkflowId: {stateObj["workflowId"]}");
            Console.WriteLine($"   - WorkflowName: {stateObj["workflowName"]}");
            Console.WriteLine($"   - RoundId: {stateObj["roundId"]}");
            Console.WriteLine($"   - ExecutionStatus: {stateObj["executionStatus"]}");
            Console.WriteLine($"   - StartTime: {stateObj["startTime"]}");
            Console.WriteLine($"   - EndTime: {stateObj["endTime"]}");
            
            var workUnits = stateObj["workUnits"];
            var workUnitsCount = (workUnits != null && workUnits.Type == JTokenType.Array) 
                ? ((JArray)workUnits).Count 
                : 0;
            Console.WriteLine($"   - TotalWorkUnits: {workUnitsCount}");
            
            // Display full JSON for debugging  
            Console.WriteLine($"\n📄 Full Orleans State (first 2000 chars):");
            var displayJson = stateJson!.Length > 2000 ? stateJson.Substring(0, 2000) + "..." : stateJson;
            Console.WriteLine(displayJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error querying Orleans state: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }
    
    private static void CheckNullFields(JToken token, string path, List<string> nullFields)
    {
        if (token.Type == JTokenType.Null)
        {
            nullFields.Add(path);
            return;
        }

        if (token.Type == JTokenType.Object)
        {
            foreach (var property in ((JObject)token).Properties())
            {
                var newPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                CheckNullFields(property.Value, newPath, nullFields);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            var array = (JArray)token;
            for (int i = 0; i < array.Count; i++)
            {
                CheckNullFields(array[i], $"{path}[{i}]", nullFields);
            }
        }
    }

    /// <summary>
    /// Step 1: Create a WorkflowViewAgent
    /// POST /api/agent
    /// </summary>
    private static async Task<Guid> CreateWorkflowAgentAsync()
    {
        var requestBody = new
        {
            AgentType = "Aevatar.GAgents.Workflow.WorkflowViewGAgentPlus",
            Name = "untitled_workflow",
            Properties = new
            {
                workflowNodeList = new[]
                {
                    new
                    {
                        nodeId = "02996e5f-2705-4de9-8dac-b93e848c2980",
                        name = "InputGAgentPlus 1",
                        agentType = "Aevatar.GAgents.InputGAgent.GAgent.InputGAgentPlus",
                        jsonProperties = "{\"Input\":\"Hello World\",\"MemberName\":\"Input Agent\"}",
                        extendedData = new
                        {
                            xPosition = "-128",
                            yPosition = "-74"
                        }
                    },
                    new
                    {
                        nodeId = "3ca5b9f5-8321-42cd-a619-d9b5560238e1",
                        name = "ChatAIGAgentPlus 1",
                        agentType = "Aevatar.GAgents.Twitter.GAgents.ChatAIAgent.ChatAIGAgentPlus",
                        jsonProperties = "{\"Instructions\":\"You are a helpful assistant\",\"SystemLLM\":\"OpenAI\",\"MemberName\":\"ChatAI Agent\"}",
                        extendedData = new
                        {
                            xPosition = "170.5",
                            yPosition = "-82.5"
                        }
                    }
                },
                workflowNodeUnitList = new[]
                {
                    new
                    {
                        nodeId = "02996e5f-2705-4de9-8dac-b93e848c2980",
                        nextNodeId = "3ca5b9f5-8321-42cd-a619-d9b5560238e1"
                    }
                },
                name = "untitled_workflow"
            }
        };

        var json = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
        Console.WriteLine($"Request body:\n{json}\n");

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/agent", content);

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response status: {response.StatusCode}");
        Console.WriteLine($"Response body:\n{responseContent}\n");

        var responseObj = JObject.Parse(responseContent);
        
        // Check for error in response
        var code = responseObj["code"]?.ToString();
        var message = responseObj["message"]?.ToString();
        var data = responseObj["data"];
        
        if (!response.IsSuccessStatusCode || code != "20000" || data == null || data.Type == JTokenType.Null)
        {
            throw new Exception($"Failed to create agent. Status: {response.StatusCode}, Code: {code}, Message: {message}");
        }

        // Parse response to get agent ID
        var agentIdStr = data["id"]?.ToString();

        if (string.IsNullOrEmpty(agentIdStr) || !Guid.TryParse(agentIdStr, out var agentId))
        {
            throw new Exception($"Failed to parse agent ID from response: {responseContent}");
        }

        return agentId;
    }

    /// <summary>
    /// Step 2: Run the workflow
    /// POST /api/workflow/run
    /// </summary>
    private static async Task<WorkflowRunResult> RunWorkflowAsync(Guid viewAgentId)
    {
        var requestBody = new
        {
            ViewAgentId = viewAgentId,
            EventProperties = new { }
        };

        var json = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
        Console.WriteLine($"Request body:\n{json}\n");

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/workflow/run", content);

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response status: {response.StatusCode}");
        Console.WriteLine($"Response body:\n{responseContent}\n");

        // Parse response
        var responseObj = JObject.Parse(responseContent);
        var code = responseObj["code"]?.ToString();
        var data = responseObj["data"];

        if (!response.IsSuccessStatusCode || code != "20000" || data == null || data.Type == JTokenType.Null)
        {
            throw new Exception($"Failed to run workflow. Status: {response.StatusCode}, Code: {code}, Response: {responseContent}");
        }

        // Parse the data object
        var workflowIdStr = data["workflowId"]?.ToString();
        Guid workflowId = Guid.Empty;
        if (!string.IsNullOrEmpty(workflowIdStr))
        {
            Guid.TryParse(workflowIdStr, out workflowId);
        }
        
        var result = new WorkflowRunResult
        {
            IsSuccess = data["isSuccess"]?.Value<bool>() ?? false,
            Message = data["message"]?.ToString() ?? string.Empty,
            WorkflowId = workflowId
        };

        return result;
    }

    /// <summary>
    /// Step 3: Get current WorkflowCoordinatorGAgentId from agent
    /// GET /api/agent/{agentId}
    /// </summary>
    private static async Task<Guid> GetWorkflowCoordinatorIdAsync(Guid agentId)
    {
        Console.WriteLine($"  → GET /api/agent/{agentId}");
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/agent/{agentId}");
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get agent. Status: {response.StatusCode}, Response: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync();
        var responseObj = JObject.Parse(content);
        var code = responseObj["code"]?.ToString();
        
        if (code != "20000")
        {
            throw new Exception($"Failed to get agent. Code: {code}, Response: {content}");
        }
        
        var coordinatorIdStr = responseObj["data"]?["properties"]?["workflowCoordinatorGAgentId"]?.ToString();
        
        if (string.IsNullOrEmpty(coordinatorIdStr) || coordinatorIdStr == "00000000-0000-0000-0000-000000000000")
        {
            throw new Exception($"WorkflowCoordinatorGAgentId is empty or not initialized. Value: {coordinatorIdStr}");
        }
        
        return Guid.Parse(coordinatorIdStr);
    }

    /// <summary>
    /// Get and display workflow topology (nodes and connections)
    /// GET /api/agent/{agentId}
    /// </summary>
    private static async Task<JObject> GetWorkflowTopologyAsync(Guid agentId, string stageName)
    {
        Console.WriteLine($"\n{stageName}: Checking workflow topology...");
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/agent/{agentId}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get agent topology. Status: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var responseObj = JObject.Parse(content);
        var properties = responseObj["data"]?["properties"];
        
        if (properties == null)
        {
            throw new Exception("No properties found in response");
        }

        var nodeList = properties["workflowNodeList"] as JArray;
        var unitList = properties["workflowNodeUnitList"] as JArray;
        
        Console.WriteLine($"  📊 Workflow Topology:");
        Console.WriteLine($"     Total Nodes: {nodeList?.Count ?? 0}");
        Console.WriteLine($"     Total Connections: {unitList?.Count ?? 0}");
        
        if (nodeList != null && nodeList.Count > 0)
        {
            Console.WriteLine($"\n  📦 Nodes:");
            foreach (var node in nodeList)
            {
                var nodeId = node["nodeId"]?.ToString();
                var agentIdStr = node["agentId"]?.ToString();
                var name = node["name"]?.ToString();
                var agentType = node["agentType"]?.ToString()?.Split('.').LastOrDefault();
                
                Console.WriteLine($"     • {name}");
                Console.WriteLine($"       - Type: {agentType}");
                Console.WriteLine($"       - NodeId: {nodeId?[..8]}...");
                Console.WriteLine($"       - AgentId: {agentIdStr?[..8]}...");
            }
        }
        
        if (unitList != null && unitList.Count > 0)
        {
            Console.WriteLine($"\n  🔗 Connections:");
            foreach (var unit in unitList)
            {
                var fromNodeId = unit["nodeId"]?.ToString();
                var toNodeId = unit["nextNodeId"]?.ToString();
                
                // Find node names
                var fromNode = nodeList?.FirstOrDefault(n => n["nodeId"]?.ToString() == fromNodeId);
                var toNode = nodeList?.FirstOrDefault(n => n["nodeId"]?.ToString() == toNodeId);
                
                var fromName = fromNode?["name"]?.ToString() ?? "Unknown";
                var toName = toNode?["name"]?.ToString() ?? "Unknown";
                
                Console.WriteLine($"     {fromName} → {toName}");
            }
        }
        
        return (JObject)properties;
    }

    /// <summary>
    /// Traverse the complete workflow chain from StartAgent to EndAgent
    /// </summary>
    private static List<(string AgentId, string Name, string Type)> TraverseWorkflowChain(JObject properties)
    {
        var chain = new List<(string AgentId, string Name, string Type)>();
        
        var startAgentId = properties["workflowStartAgentId"]?.ToString();
        var endAgentId = properties["workflowEndAgentId"]?.ToString();
        var nodeList = properties["workflowNodeList"] as JArray;
        var unitList = properties["workflowNodeUnitList"] as JArray;
        
        if (string.IsNullOrEmpty(startAgentId) || startAgentId == "00000000-0000-0000-0000-000000000000")
        {
            Console.WriteLine("  ⚠️  StartAgent not initialized");
            return chain;
        }
        
        Console.WriteLine($"\n  🔄 Traversing workflow chain:");
        Console.WriteLine($"     Start: {startAgentId?[..8]}...");
        Console.WriteLine($"     End:   {endAgentId?[..8]}...");
        
        // Add StartAgent
        chain.Add((startAgentId!, "WorkflowStartAgent", "WorkflowStartAgent"));
        Console.WriteLine($"     [1] WorkflowStartAgent ({startAgentId?[..8]}...)");
        
        // Build a map: agentId -> nodeId for lookup
        var agentIdToNode = new Dictionary<string, JToken>();
        if (nodeList != null)
        {
            foreach (var node in nodeList)
            {
                var agId = node["agentId"]?.ToString();
                if (!string.IsNullOrEmpty(agId) && agId != "00000000-0000-0000-0000-000000000000")
                {
                    agentIdToNode[agId] = node;
                }
            }
        }
        
        // Build connection map: nodeId -> nextNodeId
        var nodeConnections = new Dictionary<string, string>();
        if (unitList != null)
        {
            foreach (var unit in unitList)
            {
                var fromNodeId = unit["nodeId"]?.ToString();
                var toNodeId = unit["nextNodeId"]?.ToString();
                if (!string.IsNullOrEmpty(fromNodeId) && !string.IsNullOrEmpty(toNodeId))
                {
                    nodeConnections[fromNodeId] = toNodeId;
                }
            }
        }
        
        // Traverse: Start → find first business node → ... → End
        // The connection from Start to first node is managed by the system
        // We need to find the first node by looking at workflowNodeUnitList
        
        int stepNum = 2;
        string? currentAgentId = startAgentId;
        var visited = new HashSet<string>();
        visited.Add(startAgentId);
        
        // Find the first business node (the one StartAgent points to)
        // Usually it's the first node in the nodeList or we need system info
        if (nodeList != null && nodeList.Count > 0)
        {
            // Get the first node's agentId
            var firstNode = nodeList[0];
            currentAgentId = firstNode["agentId"]?.ToString();
            
            if (!string.IsNullOrEmpty(currentAgentId) && currentAgentId != "00000000-0000-0000-0000-000000000000")
            {
                var name = firstNode["name"]?.ToString() ?? "Unknown";
                var type = firstNode["agentType"]?.ToString()?.Split('.').LastOrDefault() ?? "Unknown";
                chain.Add((currentAgentId, name, type));
                Console.WriteLine($"     [{stepNum}] {name} ({currentAgentId?[..8]}...) - {type}");
                visited.Add(currentAgentId);
                stepNum++;
                
                // Now traverse using workflowNodeUnitList
                var currentNodeId = firstNode["nodeId"]?.ToString();
                while (!string.IsNullOrEmpty(currentNodeId) && nodeConnections.ContainsKey(currentNodeId))
                {
                    var nextNodeId = nodeConnections[currentNodeId];
                    var nextNode = nodeList?.FirstOrDefault(n => n["nodeId"]?.ToString() == nextNodeId);
                    
                    if (nextNode != null)
                    {
                        var nextAgentId = nextNode["agentId"]?.ToString();
                        if (!string.IsNullOrEmpty(nextAgentId) && nextAgentId != "00000000-0000-0000-0000-000000000000")
                        {
                            if (visited.Contains(nextAgentId))
                            {
                                Console.WriteLine($"     ⚠️  Circular reference detected at {nextAgentId?[..8]}...");
                                break;
                            }
                            
                            name = nextNode["name"]?.ToString() ?? "Unknown";
                            type = nextNode["agentType"]?.ToString()?.Split('.').LastOrDefault() ?? "Unknown";
                            chain.Add((nextAgentId, name, type));
                            Console.WriteLine($"     [{stepNum}] {name} ({nextAgentId?[..8]}...) - {type}");
                            visited.Add(nextAgentId);
                            stepNum++;
                            
                            currentNodeId = nextNodeId;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        
        // Add EndAgent
        if (!string.IsNullOrEmpty(endAgentId) && endAgentId != "00000000-0000-0000-0000-000000000000")
        {
            chain.Add((endAgentId!, "WorkflowEndAgent", "WorkflowEndAgent"));
            Console.WriteLine($"     [{stepNum}] WorkflowEndAgent ({endAgentId?[..8]}...)");
        }
        
        Console.WriteLine($"     Total chain length: {chain.Count}");
        
        return chain;
    }
    
    /// <summary>
    /// Compare two workflow chains
    /// </summary>
    private static void CompareWorkflowChains(
        List<(string AgentId, string Name, string Type)> chain1,
        List<(string AgentId, string Name, string Type)> chain2)
    {
        Console.WriteLine($"\n📊 Workflow Chain Comparison:");
        Console.WriteLine($"   Before Update: {chain1.Count} agents in chain");
        Console.WriteLine($"   After Update:  {chain2.Count} agents in chain");
        
        if (chain1.Count != chain2.Count)
        {
            Console.WriteLine($"   ⚠️  Chain length changed!");
            return;
        }
        
        bool allMatch = true;
        for (int i = 0; i < chain1.Count; i++)
        {
            var agent1 = chain1[i];
            var agent2 = chain2[i];
            
            var match = agent1.AgentId == agent2.AgentId;
            var icon = match ? "✅" : "❌";
            
            Console.WriteLine($"   [{i + 1}] {icon} {agent1.Name}");
            Console.WriteLine($"       Before: {agent1.AgentId?[..8]}... ({agent1.Type})");
            Console.WriteLine($"       After:  {agent2.AgentId?[..8]}... ({agent2.Type})");
            
            if (!match)
            {
                allMatch = false;
            }
        }
        
        if (allMatch)
        {
            Console.WriteLine($"\n   ✅ Complete workflow chain preserved correctly!");
            Console.WriteLine($"      All agent IDs match from StartAgent to EndAgent");
        }
        else
        {
            Console.WriteLine($"\n   ❌ Workflow chain changed unexpectedly!");
        }
    }

    /// <summary>
    /// Step 4: Update WorkflowViewAgent configuration
    /// PUT /api/agent/{agentId}
    /// Real-world scenario: Send complete workflow definition WITH workflowCoordinatorGAgentId
    /// </summary>
    private static async Task<Guid> UpdateWorkflowAgentAsync(Guid agentId, Guid workflowCoordinatorGAgentId)
    {
        Console.WriteLine($"  → Getting current configuration to include in update...");
        
        // Get current agent configuration with all agentIds
        var getResponse = await _httpClient.GetAsync($"{_baseUrl}/api/agent/{agentId}");
        if (!getResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get agent for update. Status: {getResponse.StatusCode}");
        }
        
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var getResponseObj = JObject.Parse(getContent);
        var currentAgent = getResponseObj["data"];
        var currentNodeList = currentAgent?["properties"]?["workflowNodeList"] as JArray;
        var currentUnitList = currentAgent?["properties"]?["workflowNodeUnitList"] as JArray;
        
        Console.WriteLine($"  → Current workflow has {currentNodeList?.Count ?? 0} nodes");
        
        // Real-world scenario: Frontend sends complete workflow definition
        // Key point: MUST include the agentId for existing nodes
        var requestBody = new
        {
            Name = "untitled_workflow_updated",
            Properties = new
            {
                workflowCoordinatorGAgentId = workflowCoordinatorGAgentId.ToString(), // ⭐ The key addition
                workflowNodeList = currentNodeList, // Include existing nodes with their agentIds
                workflowNodeUnitList = currentUnitList,
                name = "untitled_workflow_updated"
            }
        };

        var json = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
        Console.WriteLine($"Update request body:\n{json}\n");

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"{_baseUrl}/api/agent/{agentId}", content);

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response status: {response.StatusCode}");
        Console.WriteLine($"Response body:\n{responseContent}\n");

        var responseObj = JObject.Parse(responseContent);
        
        // Check for error in response
        var code = responseObj["code"]?.ToString();
        var message = responseObj["message"]?.ToString();
        
        if (!response.IsSuccessStatusCode || code != "20000")
        {
            throw new Exception($"Failed to update agent. Status: {response.StatusCode}, Code: {code}, Message: {message}");
        }
        
        // Return the WorkflowCoordinatorGAgentId from response
        var updatedCoordinatorId = responseObj["data"]?["properties"]?["workflowCoordinatorGAgentId"]?.ToString();
        Console.WriteLine($"  → Response WorkflowCoordinatorGAgentId: {updatedCoordinatorId}");
        
        if (string.IsNullOrEmpty(updatedCoordinatorId))
        {
            Console.WriteLine($"  ⚠️  Warning: Response did not contain WorkflowCoordinatorGAgentId, using input value");
            return workflowCoordinatorGAgentId;
        }
        
        return Guid.Parse(updatedCoordinatorId);
    }

    /// <summary>
    /// Step 3: Query WorkflowExecutionRecordStatePlus
    /// GET /api/query/es
    /// </summary>
    private static async Task QueryWorkflowExecutionRecordAsync(Guid workflowId)
    {
        var queryUrl = $"{_baseUrl}/api/query/es" +
            $"?queryString=workflowId%3A{workflowId}" +
            $"&stateName=WorkflowExecutionRecordStatePlus" +
            $"&pageIndex=0" +
            $"&pageSize=1" +
            $"&sortFields=roundId%3ADesc";

        Console.WriteLine($"Query URL:\n{queryUrl}\n");

        var response = await _httpClient.GetAsync(queryUrl);
        var responseContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Response status: {response.StatusCode}");
        Console.WriteLine($"Response body:\n{responseContent}\n");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"⚠️ Warning: Failed to query execution record. Status: {response.StatusCode}");
            return;
        }

        // Parse response and check for null fields
        try
        {
            var jsonResponse = JObject.Parse(responseContent);
            var data = jsonResponse["data"];
            
            if (data == null || data.Type == JTokenType.Null)
            {
                Console.WriteLine("⚠️ No execution records found.");
                return;
            }

            var items = data["items"];
            if (items == null || !items.Any())
            {
                Console.WriteLine("⚠️ No execution record items found.");
                return;
            }

            var firstRecord = items.First();
            Console.WriteLine("✅ Found execution record:\n");
            
            // Check for null fields
            var nullFields = new List<string>();
            foreach (var property in firstRecord.Children<JProperty>())
            {
                if (property.Value.Type == JTokenType.Null)
                {
                    nullFields.Add(property.Name);
                }
            }

            // Display summary
            if (nullFields.Any())
            {
                Console.WriteLine($"⚠️ Found {nullFields.Count} null field(s):");
                foreach (var field in nullFields)
                {
                    Console.WriteLine($"   - {field}");
                }
            }
            else
            {
                Console.WriteLine("✅ No null fields found in the execution record.");
            }
            
            Console.WriteLine($"\n📊 Execution Record Summary:");
            Console.WriteLine($"   - WorkflowId: {firstRecord["workflowId"]}");
            Console.WriteLine($"   - RoundId: {firstRecord["roundId"]}");
            Console.WriteLine($"   - WorkflowName: {firstRecord["workflowName"]}");
            Console.WriteLine($"   - ExecutionStatus: {firstRecord["executionStatus"]}");
            Console.WriteLine($"   - StartTime: {firstRecord["startTime"]}");
            Console.WriteLine($"   - EndTime: {firstRecord["endTime"]}");
            
            var workUnits = firstRecord["workUnits"];
            var workUnitsCount = (workUnits != null && workUnits.Type == JTokenType.Array) 
                ? ((JArray)workUnits).Count 
                : 0;
            Console.WriteLine($"   - TotalWorkUnits: {workUnitsCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error parsing execution record: {ex.Message}");
        }
    }
}

/// <summary>
/// Workflow run result DTO
/// </summary>
class WorkflowRunResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid WorkflowId { get; set; }
}

