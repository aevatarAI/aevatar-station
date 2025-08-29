# Agent Information Management System - Integration Guide

## 📋 Overview

The Agent Information Management System provides a standardized way to mark, discover, and extract metadata from Agent classes for HTTP services and LLM consumption. It enables automatic Agent discovery and information aggregation without manual configuration.

## 🚀 Quick Start

### 1. Add Package Reference

```bash
dotnet add package Aevatar.GAgents.AI.Abstractions --version 1.0.0
```

Or add to your `.csproj` file:
```xml
<PackageReference Include="Aevatar.GAgents.AI.Abstractions" Version="1.0.0" />
```

### 2. Mark Your Agent

```csharp
using Aevatar.GAgents.AI.Common;

[AgentDescription(
    "Social Chat Agent",
    "AI-powered social platform chat agent with multi-turn conversation and emotion understanding capabilities for social scenarios.",
    "This is an AI agent specifically designed for social scenarios, supporting various social platform chat interactions. It integrates mainstream models like GPT/Claude, supports context understanding, style adaptation, multi-turn dialogue and other functions. It can generate tweets, articles, summaries and other text formats, providing intelligent social interaction capabilities."
)]
public class SocialGAgent : AIGAgentBase<SocialGAgentState, SocialGAgentSEvent>
{
    // Your agent implementation
}
```

### 3. Scan Agents

```csharp
using Aevatar.GAgents.AI.Common;

// Scan current assembly
var agents = SimpleAgentScanner.ScanAgentsInAssembly(Assembly.GetExecutingAssembly());

// Scan all loaded assemblies
var allAgents = SimpleAgentScanner.ScanAllLoadedAssemblies();

// Use in HTTP service
foreach (var agent in agents)
{
    Console.WriteLine($"Found Agent: {agent.Name} - {agent.L1Description}");
}
```

## 🏷️ Agent Marking Guide

### AgentDescriptionAttribute Parameters

```csharp
[AgentDescription(
    name: string,                    // Agent display name
    l1Description: string,           // 100-150 characters for quick matching  
    l2Description: string,           // 300-500 characters for detailed capability
    Category: string,                // Optional: Agent category
    Capabilities: string[],          // Optional: Capability list
    Tags: string[],                  // Optional: Tags for classification
    InputFormat: string,             // Optional: Input format (default: "text")
    OutputFormat: string,            // Optional: Output format (default: "text")
    UsageExample: string             // Optional: Usage example
)]
```

### Complete Example

```csharp
[AgentDescription(
    "AI Content Generator",
    "Multi-format AI content generator supporting tweets, articles, summaries with context understanding and style adaptation.",
    "Advanced AI-powered content generation agent that integrates multiple language models including GPT and Claude. Supports various content formats including social media posts, technical articles, and executive summaries. Features context-aware generation, style adaptation, multi-turn conversations, and template-based content creation with comprehensive error handling and output validation.",
    Category = "Content",
    Capabilities = new[] { "content-generation", "multi-format", "context-aware", "style-adaptation" },
    Tags = new[] { "ai", "content", "generation", "nlp" },
    InputFormat = "json",
    OutputFormat = "json",
    UsageExample = "await GenerateContentAsync(new ContentRequest { Type = \"article\", Topic = \"AI trends\" })"
)]
public class AIContentAgent : AIGAgentBase<ContentAgentState, ContentAgentSEvent>
{
    // Implementation
}
```

## 📊 Data Structure

### AgentIndexInfo Properties

```csharp
public class AgentIndexInfo
{
    public string Id { get; set; }              // Auto-generated unique identifier
    public string Name { get; set; }            // Agent display name
    public string Category { get; set; }        // Agent category
    public string L1Description { get; set; }   // Quick matching description
    public string L2Description { get; set; }   // Detailed capability description
    public List<string> Capabilities { get; set; }  // Capability list
    public List<string> Tags { get; set; }      // Classification tags
    public string InputFormat { get; set; }     // Input format specification
    public string OutputFormat { get; set; }    // Output format specification
    public string UsageExample { get; set; }    // Usage example
    public string AgentType { get; set; }       // Agent class type name
    public string InterfaceType { get; set; }   // Agent interface type name
}
```

## 🔧 Usage Scenarios

### HTTP Service Integration

```csharp
public class AgentDiscoveryService
{
    private readonly List<AgentIndexInfo> _agents;
    
    public AgentDiscoveryService()
    {
        // Scan agents at startup
        _agents = SimpleAgentScanner.ScanAllLoadedAssemblies();
        LogAgentDiscovery();
    }
    
    public List<AgentIndexInfo> GetAllAgents() => _agents;
    
    public List<AgentIndexInfo> GetAgentsByCategory(string category)
    {
        return _agents.Where(a => a.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true).ToList();
    }
    
    public List<AgentIndexInfo> SearchAgents(string query)
    {
        return _agents.Where(a => 
            a.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            a.L1Description.Contains(query, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }
    
    private void LogAgentDiscovery()
    {
        foreach (var agent in _agents)
        {
            Console.WriteLine($"Discovered Agent: {agent.Name} ({agent.Category})");
            Console.WriteLine($"  L1: {agent.L1Description}");
            Console.WriteLine($"  Capabilities: {string.Join(", ", agent.Capabilities)}");
        }
    }
}
```

### LLM Integration

```csharp
public class LLMAgentProvider
{
    public string GenerateAgentListForLLM()
    {
        var agents = SimpleAgentScanner.ScanAllLoadedAssemblies();
        
        var agentInfo = agents.Select(a => new {
            name = a.Name,
            category = a.Category,
            description = a.L1Description,
            capabilities = a.Capabilities,
            usage = a.UsageExample
        });
        
        return JsonSerializer.Serialize(agentInfo, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }
    
    public List<AgentIndexInfo> FilterAgentsForTask(string taskDescription)
    {
        var agents = SimpleAgentScanner.ScanAllLoadedAssemblies();
        
        // Simple keyword matching (can be enhanced with semantic similarity)
        return agents.Where(a => 
            ContainsRelevantKeywords(a.L2Description, taskDescription) ||
            a.Capabilities.Any(c => taskDescription.Contains(c, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }
}
```

### ASP.NET Core Integration

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<AgentDiscoveryService>();
}

// AgentController.cs
[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly AgentDiscoveryService _agentService;
    
    public AgentsController(AgentDiscoveryService agentService)
    {
        _agentService = agentService;
    }
    
    [HttpGet]
    public ActionResult<List<AgentIndexInfo>> GetAllAgents()
    {
        return _agentService.GetAllAgents();
    }
    
    [HttpGet("category/{category}")]
    public ActionResult<List<AgentIndexInfo>> GetAgentsByCategory(string category)
    {
        return _agentService.GetAgentsByCategory(category);
    }
    
    [HttpGet("search")]
    public ActionResult<List<AgentIndexInfo>> SearchAgents([FromQuery] string query)
    {
        return _agentService.SearchAgents(query);
    }
}
```

## 📏 Best Practices

### Description Length Guidelines

- **L1Description**: 100-150 characters
  - Focus on core functionality
  - Use for quick matching and filtering
  - Keep concise and clear
  
- **L2Description**: 300-500 characters
  - Provide detailed capability explanation
  - Include usage scenarios and features
  - Use for LLM understanding and analysis

### Naming Conventions

```csharp
// Good
[AgentDescription("AI Content Generator", ...)]
[AgentDescription("Social Media Manager", ...)]
[AgentDescription("Data Analysis Agent", ...)]

// Avoid
[AgentDescription("Agent1", ...)]
[AgentDescription("MyAgent", ...)]
[AgentDescription("TestingAgent", ...)]
```

### Category Standards

Recommended categories:
- `Content` - Content generation and editing
- `Social` - Social media and communication
- `Data` - Data processing and analysis
- `Workflow` - Process orchestration and routing
- `AI` - Core AI capabilities
- `Integration` - External system integration

### Capability Naming

Use kebab-case for consistency:
```csharp
Capabilities = new[] { 
    "content-generation", 
    "multi-language", 
    "real-time-processing",
    "batch-operations"
}
```

## 🧪 Testing Your Implementation

### Unit Test Example

```csharp
[Fact]
public void MyAgent_ShouldBeDiscoverable()
{
    // Act
    var agents = SimpleAgentScanner.ScanAgentsInAssembly(Assembly.GetExecutingAssembly());
    
    // Assert
    var myAgent = agents.FirstOrDefault(a => a.Name == "My Agent Name");
    Assert.NotNull(myAgent);
    Assert.Equal("Expected Category", myAgent.Category);
    Assert.True(myAgent.L1Description.Length >= 100 && myAgent.L1Description.Length <= 150);
    Assert.True(myAgent.L2Description.Length >= 300 && myAgent.L2Description.Length <= 500);
}
```

### Performance Testing

```csharp
[Fact]
public void AgentScanning_ShouldBePerformant()
{
    var stopwatch = Stopwatch.StartNew();
    var agents = SimpleAgentScanner.ScanAllLoadedAssemblies();
    stopwatch.Stop();
    
    Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Scanning should complete within 1 second");
    Assert.NotEmpty(agents);
}
```

## ❓ Common Issues

### Issue: Agent Not Found

**Problem**: Agent class is marked but not discovered during scanning.

**Solutions**:
1. Ensure the assembly containing the agent is loaded
2. Verify `AgentDescriptionAttribute` is correctly applied
3. Check that the class is public and not abstract

```csharp
// Correct
[AgentDescription("Test Agent", "Description...", "Detailed description...")]
public class TestAgent : SomeBaseClass { }

// Incorrect - won't be discovered
internal class TestAgent { }  // Not public
abstract class TestAgent { } // Abstract class
```

### Issue: Description Length Validation

**Problem**: L1 or L2 descriptions don't meet length requirements.

**Solution**: Use the validation in your tests:

```csharp
Assert.True(agent.L1Description.Length >= 100 && agent.L1Description.Length <= 150);
Assert.True(agent.L2Description.Length >= 300 && agent.L2Description.Length <= 500);
```

### Issue: Performance Concerns

**Problem**: Scanning takes too long with many assemblies.

**Solutions**:
1. Cache results at application startup
2. Scan specific assemblies instead of all loaded assemblies
3. Use background scanning for non-critical paths

```csharp
// Cache at startup
private static readonly Lazy<List<AgentIndexInfo>> _cachedAgents = 
    new Lazy<List<AgentIndexInfo>>(() => SimpleAgentScanner.ScanAllLoadedAssemblies());

public static List<AgentIndexInfo> GetCachedAgents() => _cachedAgents.Value;
```

## 🔄 Integration Workflow

1. **Development Phase**
   - Mark Agent classes with `AgentDescriptionAttribute`
   - Write unit tests to verify discoverability
   - Validate description lengths and content

2. **Testing Phase**
   - Run assembly scanning tests
   - Verify all expected agents are discovered
   - Check performance metrics

3. **Deployment Phase**
   - HTTP service scans agents at startup
   - Cache agent information for performance
   - Expose agent data via REST APIs

4. **Runtime Phase**
   - LLM systems query agent information
   - Dynamic agent selection based on capabilities
   - Workflow orchestration using agent metadata

## 📞 Support

For issues and questions:
- Check the unit tests in `test/Aevatar.GAgents.AI.Abstractions.Test/`
- Review existing agent implementations for examples
- Ensure all dependencies are properly referenced

---

**Happy Agent Development! 🤖** 