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

            // Step 2: Run Workflow
            Console.WriteLine("Step 2: Running workflow...");
            var result = await RunWorkflowAsync(agentId);
            Console.WriteLine($"✅ Workflow execution completed:");
            Console.WriteLine($"   - Success: {result.IsSuccess}");
            Console.WriteLine($"   - Message: {result.Message}");
            Console.WriteLine($"   - Workflow ID: {result.WorkflowId}\n");
            
            // Step 3: Query WorkflowExecutionRecordStatePlus via ES
            if (result.IsSuccess && result.WorkflowId != Guid.Empty)
            {
                Console.WriteLine("Step 3: Querying workflow execution record state via ES...");
                // Wait a bit for the workflow to complete processing
                await Task.Delay(2000);
                
                await QueryWorkflowExecutionRecordAsync(result.WorkflowId);
                
                // Step 4: Query via Orleans GetStateSnapshotAsync
                Console.WriteLine("\nStep 4: Querying workflow execution record state via Orleans...");
                await InitializeOrleansClientAsync();
                await QueryOrleansStateAsync(result.WorkflowId);
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

