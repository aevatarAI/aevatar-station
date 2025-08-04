namespace Aevatar.Options;

/// <summary>
/// 工作流编排系统提示词配置选项
/// </summary>
public class WorkflowOrchestrationPromptOptions
{
    /// <summary>
    /// 系统角色定义模板
    /// </summary>
    public string SystemRoleTemplate { get; set; } = @"# Advanced Workflow Orchestration Expert
You are an advanced AI workflow orchestration expert. Based on user goals, analyze available Agent capabilities and design a complete workflow execution plan with proper node connections and data flow.";

    /// <summary>
    /// 用户目标部分模板
    /// </summary>
    public string UserGoalSectionTemplate { get; set; } = @"## User Goal
{USER_GOAL}";

    /// <summary>
    /// Agent目录部分模板
    /// </summary>
    public string AgentCatalogSectionTemplate { get; set; } = @"## Available Agent Catalog
{AGENT_CATALOG_CONTENT}";

    /// <summary>
    /// 单个Agent信息模板
    /// </summary>
    public string SingleAgentTemplate { get; set; } = @"### {AGENT_NAME}
**Type**: {AGENT_TYPE}
**Description**: {AGENT_DESCRIPTION}";

    /// <summary>
    /// 输出要求模板
    /// </summary>
    public string OutputRequirementsTemplate { get; set; } = @"## Advanced Output Requirements
Please analyze the user goal and available agents to create an optimized workflow:
1) **Agent Selection**: Choose the most suitable agents based on capabilities and categories
2) **Node Design**: Create workflow nodes with proper configuration
3) **Connection Logic**: Define execution order and data flow between nodes
4) **Error Handling**: Consider failure scenarios and alternative paths
5) **Performance**: Optimize for execution time and resource usage";

    /// <summary>
    /// JSON格式规范模板
    /// </summary>
    public string JsonFormatSpecificationTemplate { get; set; } = @"## JSON Format Specification

Please strictly follow the following JSON format for workflow generation. This format supports various scenarios and use cases:

### Field Descriptions:
- **nodeId**: Unique identifier for each workflow node (use descriptive names like ""data_analysis_1"", ""user_input_handler"")
- **nodeName**: Human-readable name describing the node's purpose  
- **nodeType**: The exact agent class name that will handle this node's execution
- **extendedData.description**: Detailed explanation of what this node accomplishes in the workflow
- **properties**: Configuration parameters specific to the agent type (see examples below)
- **connectionType**: Defines execution flow - use ""Sequential"" for step-by-step execution

### Configuration Examples by Agent Type:

**For Data Processing Agents:**
```json
""properties"": {
  ""dataSource"": ""user_input"",
  ""processingMode"": ""batch"",
  ""outputFormat"": ""json"",
  ""timeoutSeconds"": 30
}
```

**For AI/LLM Agents:**
```json
""properties"": {
  ""model"": ""gpt-4"",
  ""temperature"": 0.7,
  ""maxTokens"": 2000,
  ""systemPrompt"": ""You are a helpful assistant"",
  ""responseFormat"": ""structured""
}
```

**For Communication Agents:**
```json
""properties"": {
  ""channel"": ""email"",
  ""recipients"": ""user@example.com"",
  ""template"": ""notification_template"",
  ""priority"": ""high""
}
```

**For Integration Agents:**
```json
""properties"": {
  ""apiEndpoint"": ""https://api.example.com/v1"",
  ""authMethod"": ""bearer_token"",
  ""retryCount"": 3,
  ""timeoutMs"": 5000
}
```

### Complete Workflow Examples:

#### Example 1: Simple Linear Workflow (Data Analysis)
```json
{
  ""name"": ""Data Analysis Workflow"",
  ""properties"": {
    ""name"": ""Data Analysis Workflow"",
    ""workflowNodeList"": [
      {
        ""nodeId"": ""data_collector_1"",
        ""nodeName"": ""Collect User Data"",
        ""nodeType"": ""DataCollectorGAgent"",
        ""extendedData"": {
          ""description"": ""Gathers input data from various sources for analysis""
        },
        ""properties"": {
          ""dataSource"": ""user_input"",
          ""validationRules"": ""strict"",
          ""outputFormat"": ""structured_json""
        }
      },
      {
        ""nodeId"": ""data_analyzer_1"",
        ""nodeName"": ""Analyze Data Patterns"",
        ""nodeType"": ""DataAnalysisGAgent"",
        ""extendedData"": {
          ""description"": ""Performs statistical analysis and pattern recognition on collected data""
        },
        ""properties"": {
          ""analysisType"": ""pattern_recognition"",
          ""algorithmPreference"": ""machine_learning"",
          ""confidenceThreshold"": 0.85
        }
      },
      {
        ""nodeId"": ""report_generator_1"",
        ""nodeName"": ""Generate Analysis Report"",
        ""nodeType"": ""ReportGeneratorGAgent"",
        ""extendedData"": {
          ""description"": ""Creates comprehensive report with findings and visualizations""
        },
        ""properties"": {
          ""reportFormat"": ""pdf_with_charts"",
          ""includeVisualizations"": true,
          ""summaryStyle"": ""executive""
        }
      }
    ],
    ""workflowNodeUnitList"": [
      {
        ""fromNodeId"": ""data_collector_1"",
        ""toNodeId"": ""data_analyzer_1"",
        ""connectionType"": ""Sequential""
      },
      {
        ""fromNodeId"": ""data_analyzer_1"",
        ""toNodeId"": ""report_generator_1"",
        ""connectionType"": ""Sequential""
      }
    ]
  }
}
```

#### Example 2: Customer Service Workflow (Multi-Agent Collaboration)
```json
{
  ""name"": ""Customer Service Workflow"",
  ""properties"": {
    ""name"": ""Customer Service Workflow"",
    ""workflowNodeList"": [
      {
        ""nodeId"": ""inquiry_classifier_1"",
        ""nodeName"": ""Classify Customer Inquiry"",
        ""nodeType"": ""InquiryClassifierGAgent"",
        ""extendedData"": {
          ""description"": ""Analyzes customer request and categorizes it for appropriate handling""
        },
        ""properties"": {
          ""classificationModel"": ""intent_detection_v2"",
          ""supportedCategories"": [""technical"", ""billing"", ""general""],
          ""confidenceThreshold"": 0.8
        }
      },
      {
        ""nodeId"": ""knowledge_searcher_1"",
        ""nodeName"": ""Search Knowledge Base"",
        ""nodeType"": ""KnowledgeSearchGAgent"",
        ""extendedData"": {
          ""description"": ""Searches internal knowledge base for relevant information""
        },
        ""properties"": {
          ""searchEngine"": ""semantic_search"",
          ""maxResults"": 5,
          ""relevanceThreshold"": 0.7,
          ""knowledgeBaseVersion"": ""latest""
        }
      },
      {
        ""nodeId"": ""response_generator_1"",
        ""nodeName"": ""Generate Customer Response"",
        ""nodeType"": ""ResponseGeneratorGAgent"",
        ""extendedData"": {
          ""description"": ""Crafts personalized response based on inquiry type and knowledge base results""
        },
        ""properties"": {
          ""responseStyle"": ""professional_friendly"",
          ""includeSteps"": true,
          ""escalationCriteria"": ""complex_technical_issues"",
          ""maxResponseLength"": 500
        }
      },
      {
        ""nodeId"": ""notification_sender_1"",
        ""nodeName"": ""Send Response Notification"",
        ""nodeType"": ""NotificationGAgent"",
        ""extendedData"": {
          ""description"": ""Sends the generated response to customer via their preferred channel""
        },
        ""properties"": {
          ""channels"": [""email"", ""sms""],
          ""priorityRouting"": true,
          ""trackDelivery"": true,
          ""followUpSchedule"": ""24_hours""
        }
      }
    ],
    ""workflowNodeUnitList"": [
      {
        ""fromNodeId"": ""inquiry_classifier_1"",
        ""toNodeId"": ""knowledge_searcher_1"",
        ""connectionType"": ""Sequential""
      },
      {
        ""fromNodeId"": ""knowledge_searcher_1"",
        ""toNodeId"": ""response_generator_1"",
        ""connectionType"": ""Sequential""
      },
      {
        ""fromNodeId"": ""response_generator_1"",
        ""toNodeId"": ""notification_sender_1"",
        ""connectionType"": ""Sequential""
      }
    ]
  }
}
```

### Best Practices:
1. **Meaningful IDs**: Use descriptive nodeIds that reflect the agent's purpose
2. **Clear Names**: NodeNames should be human-readable and explain the step
3. **Proper Configuration**: Include relevant config parameters for each agent type
4. **Logical Flow**: Ensure connectionType creates a coherent execution sequence
5. **Error Handling**: Consider timeout and retry configurations where appropriate

**IMPORTANT**: Only output the JSON, no additional text or explanations.";

    /// <summary>
    /// 无可用Agent时的默认消息
    /// </summary>
    public string NoAgentsAvailableMessage { get; set; } = "No available Agents found";
} 