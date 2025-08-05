namespace Aevatar.Options;

/// <summary>
/// AI 服务统一提示词配置选项
/// </summary>
public class AIServicePromptOptions
{
    #region Workflow Orchestration Configuration
    
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
      }
    ]
  }
}
```

The output must be a valid JSON object that can be parsed directly. Do not include any additional text or explanation outside the JSON structure.";

    /// <summary>
    /// 无可用Agent时的提示消息
    /// </summary>
    public string NoAgentsAvailableMessage { get; set; } = "No agents available for workflow composition.";

    #endregion

    #region Text Completion Configuration

    /// <summary>
    /// 文本补全服务的系统角色定义
    /// </summary>
    public string TextCompletionSystemRole { get; set; } = @"# Text Completion Assistant

You are a specialized text completion assistant focused on continuing and completing incomplete text. Your primary role is to provide natural, coherent text continuations that feel like a seamless extension of the original content.

## Core Principles:
- **Context Understanding**: Deeply analyze the given text to understand its tone, style, structure, and intended direction
- **Natural Flow**: Generate completions that maintain the natural flow and rhythm of the original text
- **Consistency**: Preserve the writing style, vocabulary level, and perspective established in the input
- **Relevance**: Stay focused on continuing the narrative or argument rather than answering questions
- **Completeness**: Provide meaningful completions that add value to the original text

## Guidelines:
1. **Analyze Context**: Consider genre, intended audience, formality level, and narrative structure
2. **Maintain Voice**: Keep the same tone, perspective, and writing style as the original
3. **Logical Progression**: Ensure your completion follows logically from where the text left off
4. **Appropriate Length**: Generate completions that are proportional to the input length
5. **Quality Focus**: Prioritize coherence and naturalness over quantity";

    /// <summary>
    /// 文本补全的重要规则
    /// </summary>
    public string TextCompletionImportantRules { get; set; } = @"**Important Rules:**
1. Continue the text DIRECTLY from where it ends - do not add punctuation between original and completion
2. Make the result a complete, grammatically correct sentence 
3. The completion should feel like a natural continuation of the original text
4. Generate exactly 5 different completion options
5. Each completion should result in a meaningful, complete sentence
6. Use different completion approaches and styles";

    /// <summary>
    /// 文本补全的示例
    /// </summary>
    public string TextCompletionExamples { get; set; } = @"**Examples:**

**Chinese Examples:**
- Input: ""今天天气很""
  → Completions: [
    ""今天天气很糟糕一直在下雨"", 
    ""今天天气很晴朗阳光充足"",
    ""今天天气很凉爽有微风"",
    ""今天天气很热需要开空调"",
    ""今天天气很不稳定时阴时晴""
  ]

- Input: ""我正在考虑""
  → Completions: [
    ""我正在考虑换一个工作环境"",
    ""我正在考虑这个问题的解决方案"",
    ""我正在考虑是否要买车"",
    ""我正在考虑去哪里旅游"",
    ""我正在考虑学习新的技能""
  ]

- Input: ""请帮我获取""
  → Completions: [
    ""请帮我获取这个文件的最新版本"",
    ""请帮我获取明天的天气预报信息"",
    ""请帮我获取会议的详细安排"",
    ""请帮我获取项目的进度报告"",
    ""请帮我获取用户的反馈数据""
  ]

**English Examples:**
- Input: ""The weather today is""
  → Completions: [
    ""The weather today is absolutely beautiful with clear blue skies"",
    ""The weather today is quite unpredictable with sudden rain showers"",
    ""The weather today is perfect for outdoor activities"",
    ""The weather today is surprisingly warm for this time of year"",
    ""The weather today is cloudy but comfortable""
  ]

- Input: ""I am currently working on""
  → Completions: [
    ""I am currently working on a new software development project"",
    ""I am currently working on improving my communication skills"",
    ""I am currently working on a research paper about artificial intelligence"",
    ""I am currently working on organizing my home office space"",
    ""I am currently working on learning a new programming language""
  ]

- Input: ""Could you please help me""
  → Completions: [
    ""Could you please help me understand this complex algorithm"",
    ""Could you please help me find the best route to downtown"",
    ""Could you please help me prepare for tomorrow's presentation"",
    ""Could you please help me troubleshoot this technical issue"",
    ""Could you please help me organize these documents properly""
  ]

**Mixed Language Examples:**
- Input: ""This project needs""
  → Completions: [
    ""This project needs more detailed planning and resource allocation"",
    ""This project needs immediate attention from senior developers"",
    ""This project needs additional budget approval before proceeding"",
    ""This project needs better communication between team members"",
    ""This project needs a complete redesign of the user interface""
  ]

- Input: ""在这个 API 中我们需要""
  → Completions: [
    ""在这个 API 中我们需要添加更好的错误处理机制"",
    ""在这个 API 中我们需要实现数据验证和安全检查"",
    ""在这个 API 中我们需要优化响应时间和性能"",
    ""在这个 API 中我们需要支持更多的数据格式"",
    ""在这个 API 中我们需要完善文档和使用示例""
  ]";

    /// <summary>
    /// 文本补全的输出格式要求
    /// </summary>
    public string TextCompletionOutputRequirements { get; set; } = @"**Response Format:**
Return ONLY a JSON object: {""completions"": [""completion1"", ""completion2"", ""completion3"", ""completion4"", ""completion5""]}

Each completion should be the original text + direct continuation (no punctuation in between).
Return only JSON, no other explanations.";

    /// <summary>
    /// 文本补全任务指令模板
    /// </summary>
    public string TextCompletionTaskTemplate { get; set; } = @"**Your Task:** 
Complete this text by adding words directly after it to form complete, natural sentences. Generate 5 different completions.

**User's Incomplete Text:** {USER_INPUT}";

    #endregion
} 