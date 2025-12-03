using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;
using Aevatar.Core.Streaming.Extensions;
using Aevatar.Core.Abstractions;
using Serilog;
using Serilog.Events;
using Aevatar.GAgents.InputGAgent.GAgent;
using Aevatar.GAgents.InputGAgent.Dto;
using Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;
using Aevatar.GAgents.Core;
using Aevatar.GAgents.Workflow;
using Aevatar.GAgents.Workflow.Core;
using Aevatar.GAgents.Workflow.Core.Configs;
using Aevatar.GAgents.Workflow.Core.Models;
using Spectre.Console;
using System.Linq;
using System.Reflection;

namespace CalculatorWorkflowDemo;

public class Program
{
    private static IHost? _host;
    private static IClusterClient? _client;
    
    // Workflow agents - shared across menu operations
    private static IWorkflowStartAgent? _startAgent;
    private static IInputGAgentPlus? _inputX;
    private static IInputGAgentPlus? _inputY;
    private static IChatAIGAgentPlus? _chatAI;
    private static IWorkflowEndAgent? _endAgent;
    
    // NEW: Workflow monitoring agents for status collection
    private static IWorkflowCoordinatorGAgentPlus? _workflowCoordinator;
    // NOTE: WorkflowExecutionRecord is created per workflow execution, not shared
    
    // NEW: Proxy agent for P2P event publishing
    private static IWorkflowProxyAgent? _proxyAgent;
    
    // Agent IDs for consistency
    private static Guid _startAgentId;
    private static Guid _inputXId;
    private static Guid _inputYId;
    private static Guid _chatAIId;
    private static Guid _endAgentId;
    
    // NEW: Workflow monitoring agent IDs
    private static Guid _workflowCoordinatorId;
    private static Guid _proxyAgentId;
    // NOTE: ExecutionRecord ID is generated per workflow execution
    
    // Current configuration values
    private static string _currentInputX = "x = 10";
    private static string _currentInputY = "y = 20";
    private static string _currentChatInstructions = "Calculate the Fibonacci number use x as base, y as iteration count. You will receive two inputs: x (base value) and y (iteration count). Calculate the Fibonacci sequence starting from x for y iterations and return the final result.";

    // ✅ NEW: No longer need local tracking - query execution records directly from WorkflowCoordinatorGAgent
    
    // ✅ NEW: ExecutionInfo class removed - execution tracking now handled by WorkflowCoordinatorGAgent dictionary

    public static async Task Main(string[] args)
    {
        Console.Title = "🧮 Calculator Workflow Demo";
        
        AnsiConsole.Write(
            new FigletText("Calculator Workflow")
                .LeftJustified()
                .Color(Color.Green));

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]🧮 Welcome to the Calculator Workflow Demo![/]");
        AnsiConsole.MarkupLine("[grey]This demo showcases workflow event forwarding with proper start/end agents[/]");
        AnsiConsole.WriteLine();

        try
        {
            await InitializeOrleansClientAsync();
            await RunInteractiveMenuAsync();
        }
        catch (Exception ex)
        {
            var errorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Unknown error occurred" : ex.Message;
            AnsiConsole.MarkupLine($"[red]✗[/] [bold red]Fatal error:[/] {errorMessage.EscapeMarkup()}");
            AnsiConsole.WriteException(ex);
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private static async Task InitializeOrleansClientAsync()
    {
        AnsiConsole.MarkupLine("[yellow]🔧 Connecting to Orleans cluster...[/]");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Orleans", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

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
                    })
                    .AddActivityPropagation()
                    .AddAevatarKafkaStreaming("Aevatar", options =>
                    {
                        options.BrokerList = new List<string> { "localhost:9092" };
                        options.ConsumerGroupId = "Aevatar";
                        options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                        var partitions = 8;
                        var replicationFactor = (short)1;
                        var topics = "Aevatar,AevatarStateProjection,AevatarBroadcast";
                        foreach (var topic in topics.Split(','))
                        {
                            options.AddTopic(topic.Trim(), new TopicCreationConfig
                            {
                                AutoCreate = true,
                                Partitions = partitions,
                                ReplicationFactor = replicationFactor
                            });
                        }
                    });
            })
            .ConfigureLogging(logging => logging.AddSerilog().SetMinimumLevel(LogLevel.Information))
            .UseConsoleLifetime();

        _host = builder.Build();
        await _host.StartAsync();
        _client = _host.Services.GetRequiredService<IClusterClient>();
        
        if (_client == null)
        {
            AnsiConsole.MarkupLine("[red]❌ Orleans client is not initialized[/]");
            throw new Exception("Orleans client is not initialized");
        }

        AnsiConsole.MarkupLine("[green]✅ Orleans client connected![/]");
        await Task.Delay(1000);
    }

    private static async Task CleanupAsync()
    {
        if (_host != null)
        {
            AnsiConsole.MarkupLine("[yellow]🧹 Cleaning up Orleans client...[/]");
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
        }
        _client = null;
    }

    private static async Task RunInteractiveMenuAsync()
    {
        if (_client == null)
        {
            AnsiConsole.MarkupLine("[red]❌ Orleans client not initialized[/]");
            return;
        }

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]🧮 What would you like to do?[/]")
                    .AddChoices(new[]
                    {
                        "🏗️ Initialize workflow agents",
                        "⚙️ Update agent configurations",
                        "🔗 Setup workflow relationships", 
                        "🧪 Setup simple test relationship (Start → Input only)",
                        "📊 Check relationship status",
                        "🚀 Publish workflow events",
                        "📈 Monitor workflow progress",
                        "🎯 Monitor per-execution progress",
                        "🗃️ Demo execution record queries",
                        "🔄 Run complete workflow",
                        "❌ Exit"
                    }));

            switch (choice)
            {
                case "🏗️ Initialize workflow agents":
                    await InitializeWorkflowAgentsAsync();
                    break;
                case "⚙️ Update agent configurations":
                    await UpdateAgentConfigurationsAsync();
                    break;
                case "🔗 Setup workflow relationships":
                    await SetupWorkflowRelationshipsAsync();
                    break;
                case "🧪 Setup simple test relationship (Start → Input only)":
                    await SetupSimpleTestRelationshipAsync();
                    break;
                case "📊 Check relationship status":
                    await CheckRelationshipStatusAsync();
                    break;
                case "🚀 Publish workflow events":
                    await PublishWorkflowEventsAsync();
                    break;
                case "📈 Monitor workflow progress":
                    await MonitorWorkflowProgressAsync();
                    break;
                case "🎯 Monitor per-execution progress":
                    await MonitorPerExecutionProgressAsync();
                    break;
                    
                case "🗃️ Demo execution record queries":
                    await DemoExecutionRecordQueriesAsync();
                    break;
                case "🔄 Run complete workflow":
                    await RunCompleteWorkflowAsync();
                    break;
                case "❌ Exit":
                    AnsiConsole.MarkupLine("[yellow]👋 Thanks for using the Calculator Workflow Demo![/]");
                    return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private static async Task InitializeWorkflowAgentsAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]🏗️ Initializing Workflow Agents[/]");
            AnsiConsole.WriteLine();

            // Only generate new IDs if agents haven't been initialized yet
            bool isFirstInitialization = _startAgent == null;
            
            if (isFirstInitialization)
            {
                // Generate unique IDs for this demo run
                _startAgentId = Guid.NewGuid();
                _inputXId = Guid.NewGuid();
                _inputYId = Guid.NewGuid();
                _chatAIId = Guid.NewGuid();
                _endAgentId = Guid.NewGuid();
                
                // NEW: Generate ID for shared workflow coordinator  
                _workflowCoordinatorId = Guid.NewGuid();
                // ExecutionRecord IDs are generated per workflow execution
                
                // NEW: Generate ID for proxy agent
                _proxyAgentId = Guid.NewGuid();
                
                AnsiConsole.MarkupLine("[yellow]🆕 Generating new agent IDs (first initialization)[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[blue]♻️ Reusing existing agent IDs (preserving relationships)[/]");
            }

            AnsiConsole.MarkupLine($"[green]Generated Agent IDs:[/]");
            AnsiConsole.MarkupLine($"  • WorkflowStartAgent: [yellow]{_startAgentId}[/]");
            AnsiConsole.MarkupLine($"  • InputX: [yellow]{_inputXId}[/]");
            AnsiConsole.MarkupLine($"  • InputY: [yellow]{_inputYId}[/]");
            AnsiConsole.MarkupLine($"  • ChatAI: [yellow]{_chatAIId}[/]");
            AnsiConsole.MarkupLine($"  • WorkflowEndAgent: [yellow]{_endAgentId}[/]");
            
            // NEW: Display workflow monitoring agent IDs
            AnsiConsole.MarkupLine($"  • WorkflowCoordinator: [cyan]{_workflowCoordinatorId}[/] (shared across executions)");
            AnsiConsole.MarkupLine($"  • ExecutionRecord: [yellow]Created per workflow execution[/]");
            
            // NEW: Display proxy agent ID
            AnsiConsole.MarkupLine($"  • WorkflowProxyAgent: [magenta]{_proxyAgentId}[/] (P2P event publisher)");
            AnsiConsole.WriteLine();

            // Create grain references (always refresh references to ensure they're current)
            AnsiConsole.MarkupLine("[yellow]📦 Creating/refreshing grain references...[/]");
            _startAgent = _client!.GetGrain<IWorkflowStartAgent>(_startAgentId);
            _inputX = _client.GetGrain<IInputGAgentPlus>(_inputXId);
            _inputY = _client.GetGrain<IInputGAgentPlus>(_inputYId);
            _chatAI = _client.GetGrain<IChatAIGAgentPlus>(_chatAIId);
            _endAgent = _client.GetGrain<IWorkflowEndAgent>(_endAgentId);
            
            // NEW: Create grain reference for shared workflow coordinator
            _workflowCoordinator = _client.GetGrain<IWorkflowCoordinatorGAgentPlus>(_workflowCoordinatorId);
            // ExecutionRecord agents are created per workflow execution
            
            // NEW: Create grain reference for proxy agent
            _proxyAgent = _client.GetGrain<IWorkflowProxyAgent>(_proxyAgentId);

            AnsiConsole.MarkupLine("[green]✅ All grain references created/refreshed (including shared workflow coordinator)![/]");
            AnsiConsole.WriteLine();

            // Configure agents
            AnsiConsole.MarkupLine("[yellow]⚙️ Configuring agents...[/]");
            
            var startAgentConfig = new WorkflowStartConfigDto { AgentName = "CalculatorWorkflowStart" };
            await _startAgent.ConfigAsync(startAgentConfig);
            AnsiConsole.MarkupLine("[green]  ✅ WorkflowStartAgent configured[/]");

            var inputXConfig = new InputConfigDto { Input = _currentInputX };
            await _inputX.ConfigAsync(inputXConfig);
            AnsiConsole.MarkupLine($"[green]  ✅ InputX configured with default: {_currentInputX.EscapeMarkup()}[/]");

            var inputYConfig = new InputConfigDto { Input = _currentInputY };
            await _inputY.ConfigAsync(inputYConfig);
            AnsiConsole.MarkupLine($"[green]  ✅ InputY configured with default: {_currentInputY.EscapeMarkup()}[/]");

            // Configure ChatAI agent with Fibonacci calculation instructions
            var chatAIConfigDto = new ChatAIGAgentConfigDtoPlus
            {
                Instructions = _currentChatInstructions,
                SystemLLM = "OpenAI",
                MemberName = "ChatAI Calculator"
            };
            await _chatAI.ConfigAsync(chatAIConfigDto);
            AnsiConsole.MarkupLine("[green]  ✅ ChatAI configured with default instructions[/]");

            var endAgentConfig = new WorkflowEndConfigDto { AgentName = "CalculatorWorkflowEnd" };
            await _endAgent.ConfigAsync(endAgentConfig);
            AnsiConsole.MarkupLine("[green]  ✅ WorkflowEndAgent configured[/]");

            // NEW: Configure workflow monitoring agents
            var workflowCoordinatorConfig = new WorkflowCoordinatorConfigDto
            {
                InitContent = "Calculator Workflow System",
                EnableExecutionRecord = true,
                WorkflowUnitList = new List<WorkflowUnitDto>() // Empty - topology auto-discovered from agent relationships at runtime
            };
            await _workflowCoordinator!.ConfigAsync(workflowCoordinatorConfig);
            AnsiConsole.MarkupLine("[green]  ✅ WorkflowCoordinator configured[/]");

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]🎯 All workflow agents initialized successfully (including shared coordinator)![/]");
            AnsiConsole.MarkupLine("[yellow]💡 Tip: Use 'Update agent configurations' to customize input values and instructions[/]");
            AnsiConsole.MarkupLine("[cyan]💡 Note: ExecutionRecord agents are created dynamically per workflow execution[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error initializing agents: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static async Task UpdateAgentConfigurationsAsync()
    {
        try
        {
            if (_inputX == null || _inputY == null || _chatAI == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Agents not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold cyan]⚙️ Update Agent Configurations[/]");
            AnsiConsole.WriteLine();

            // Display current values and get new ones
            AnsiConsole.MarkupLine("[bold yellow]📋 Current Configuration:[/]");
            AnsiConsole.MarkupLine($"  • Input X: [cyan]{_currentInputX.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  • Input Y: [cyan]{_currentInputY.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  • ChatAI Task: [cyan]{(_currentChatInstructions.Length > 50 ? _currentChatInstructions.Substring(0, 50) + "..." : _currentChatInstructions).EscapeMarkup()}[/]");
            AnsiConsole.WriteLine();

            // Get Input X value (using current as default)
            var inputXValue = AnsiConsole.Ask<string>("[yellow]Enter new value for Input X:[/]", _currentInputX);
            
            // Get Input Y value (using current as default)
            var inputYValue = AnsiConsole.Ask<string>("[yellow]Enter new value for Input Y:[/]", _currentInputY);
            
            // Get ChatAI instructions (using current as default)
            AnsiConsole.MarkupLine("[yellow]Enter new instructions for ChatAI Agent:[/]");
            AnsiConsole.MarkupLine("[grey]Press Enter to keep current instructions, or type new ones:[/]");
            
            var chatInstructions = AnsiConsole.Ask<string>("[yellow]ChatAI Instructions:[/]", _currentChatInstructions);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]📝 Updating agent configurations...[/]");

            // Update InputX
            var inputXConfig = new InputConfigDto { Input = inputXValue };
            await _inputX.ConfigAsync(inputXConfig);
            _currentInputX = inputXValue; // Update tracking variable
            AnsiConsole.MarkupLine($"[green]  ✅ InputX updated: {inputXValue.EscapeMarkup()}[/]");

            // Update InputY
            var inputYConfig = new InputConfigDto { Input = inputYValue };
            await _inputY.ConfigAsync(inputYConfig);
            _currentInputY = inputYValue; // Update tracking variable
            AnsiConsole.MarkupLine($"[green]  ✅ InputY updated: {inputYValue.EscapeMarkup()}[/]");

            // Update ChatAI
            var chatAIConfigDtoPlus = new ChatAIGAgentConfigDto
            {
                Instructions = chatInstructions,
                SystemLLM = "OpenAI",
                MemberName = "ChatAI Calculator"
            };
            await _chatAI.ConfigAsync(chatAIConfigDtoPlus);
            _currentChatInstructions = chatInstructions; // Update tracking variable
            AnsiConsole.MarkupLine("[green]  ✅ ChatAI instructions updated[/]");

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]🎯 All agent configurations updated successfully![/]");
            
            // Show summary
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]📋 Configuration Summary:[/]");
            AnsiConsole.MarkupLine($"  • Input X: [cyan]{inputXValue.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  • Input Y: [cyan]{inputYValue.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  • ChatAI Task: [cyan]{(chatInstructions.Length > 50 ? chatInstructions.Substring(0, 50) + "..." : chatInstructions).EscapeMarkup()}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error updating configurations: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static async Task SetupWorkflowRelationshipsAsync()
    {
        try
        {
            if (_startAgent == null || _inputX == null || _inputY == null || _chatAI == null || _endAgent == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Agents not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }
            
            // NEW: Check workflow coordinator (ExecutionRecord is created per execution)
            if (_workflowCoordinator == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Workflow coordinator not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold cyan]🔗 Setting Up Workflow Relationships[/]");
            AnsiConsole.WriteLine();

            // WorkflowStartAgent -> InputX, InputY (parallel processing)
            var startAgentChildrenBefore = await _startAgent.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[blue]📊 StartAgent children before setup: {startAgentChildrenBefore.Count}[/]");
            
            AnsiConsole.MarkupLine("[yellow]🔄 Registering InputX as child of StartAgent...[/]");
            await _startAgent.RegisterAsync(_inputX);
            var startAgentChildrenAfterX = await _startAgent.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[green]  ✅ StartAgent children after InputX: {startAgentChildrenAfterX.Count}[/]");

            AnsiConsole.MarkupLine("[yellow]🔄 Registering InputY as child of StartAgent...[/]");
            await _startAgent.RegisterAsync(_inputY);
            var startAgentChildrenAfterY = await _startAgent.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[green]  ✅ StartAgent children after InputY: {startAgentChildrenAfterY.Count}[/]");

            // InputX, InputY -> ChatAI (convergence point)
            var inputXChildrenBefore = await _inputX.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[blue]📊 InputX children before setup: {inputXChildrenBefore.Count}[/]");
            
            AnsiConsole.MarkupLine("[yellow]🔄 Registering ChatAI as child of InputX...[/]");
            await _inputX.RegisterAsync(_chatAI);
            var inputXChildren = await _inputX.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[green]  ✅ InputX children after ChatAI: {inputXChildren.Count}[/]");

            var inputYChildrenBefore = await _inputY.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[blue]📊 InputY children before setup: {inputYChildrenBefore.Count}[/]");
            
            AnsiConsole.MarkupLine("[yellow]🔄 Registering ChatAI as child of InputY...[/]");
            await _inputY.RegisterAsync(_chatAI);
            var inputYChildren = await _inputY.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[green]  ✅ InputY children after ChatAI: {inputYChildren.Count}[/]");

            // ChatAI -> WorkflowEndAgent (completion)
            var chatAIChildrenBefore = await _chatAI.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[blue]📊 ChatAI children before setup: {chatAIChildrenBefore.Count}[/]");
            
            AnsiConsole.MarkupLine("[yellow]🔄 Registering EndAgent as child of ChatAI...[/]");
            await _chatAI.RegisterAsync(_endAgent);
            var chatAIChildren = await _chatAI.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[green]  ✅ ChatAI children after EndAgent: {chatAIChildren.Count}[/]");

            // NEW: Register shared WorkflowCoordinator as child of all business agents
            AnsiConsole.MarkupLine("[cyan]🔗 Setting up shared workflow coordinator relationships...[/]");
            AnsiConsole.MarkupLine("[yellow]📋 WorkflowCoordinator receives events from ALL business agents (shared across executions)[/]");
            AnsiConsole.MarkupLine("[yellow]📋 ExecutionRecord agents will be registered per workflow execution[/]");
            
            // Register shared WorkflowCoordinator as child of ALL business agents
            var businessAgents = new (string Name, IGAgentPlus Agent)[]
            {
                ("StartAgent", (IGAgentPlus)_startAgent!),
                ("InputX", (IGAgentPlus)_inputX!),
                ("InputY", (IGAgentPlus)_inputY!),
                ("ChatAI", (IGAgentPlus)_chatAI!),
                ("EndAgent", (IGAgentPlus)_endAgent!)
            };
            
            foreach (var (name, agent) in businessAgents)
            {
                AnsiConsole.MarkupLine($"[yellow]🔄 Registering shared WorkflowCoordinator as child of {name}...[/]");
                await agent.RegisterAsync(_workflowCoordinator);
                
                var childrenCount = await agent.GetChildrenAsync();
                AnsiConsole.MarkupLine($"[green]  ✅ {name} now has {childrenCount.Count} children (including shared coordinator)[/]");
            }

            // Allow time for subscription setup and state persistence
            AnsiConsole.MarkupLine("[yellow]⏳ Waiting for state persistence and subscription setup...[/]");
            await Task.Delay(3000);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]🎯 Workflow relationships established (shared coordinator registered)![/]");
            AnsiConsole.MarkupLine($"[bold green]Chain: Start → {Markup.Escape("[InputX, InputY]")} → ChatAI → End[/]");
            AnsiConsole.MarkupLine($"[cyan]📝 ExecutionRecord agents will be created and registered per workflow execution[/]");
            AnsiConsole.MarkupLine($"[bold cyan]Monitoring: Start → WorkflowCoordinator, End → ExecutionRecord[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error setting up relationships: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static async Task SetupSimpleTestRelationshipAsync()
    {
        try
        {
            if (_startAgent == null || _inputX == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Agents not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold cyan]🧪 Setting Up Simple Test Relationship[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[yellow]🔗 Setting up: StartAgent → InputX[/]");

            // Check current state
            var startAgentChildrenBefore = await _startAgent.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[blue]📊 StartAgent children before setup: {startAgentChildrenBefore.Count}[/]");

            // Setup the relationship
            AnsiConsole.MarkupLine("[yellow]🔄 Registering InputX as child of StartAgent...[/]");
            await _startAgent.RegisterAsync(_inputX);

            var startAgentChildrenAfter = await _startAgent.GetChildrenAsync();
            AnsiConsole.MarkupLine($"[green]  ✅ StartAgent children after setup: {startAgentChildrenAfter.Count}[/]");

            // Wait for state persistence
            AnsiConsole.MarkupLine("[yellow]⏳ Waiting for state persistence and subscription setup...[/]");
            await Task.Delay(2000);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]🎯 Simple test relationship established![/]");
            AnsiConsole.MarkupLine("[bold green]Chain: StartAgent → InputX[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error setting up simple test relationship: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static async Task CheckRelationshipStatusAsync()
    {
        try
        {
            if (_startAgent == null || _inputX == null || _inputY == null || _chatAI == null || _endAgent == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Agents not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }
            
            if (_workflowCoordinator == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Workflow coordinator not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold cyan]📊 Checking Relationship Status (including monitoring agents)[/]");
            AnsiConsole.WriteLine();

            // ✅ NEW: Print agent instance IDs for debugging
            AnsiConsole.MarkupLine("[bold yellow]🔍 Agent Instance IDs:[/]");
            AnsiConsole.MarkupLine($"  • StartAgent ID: [green]{_startAgentId}[/]");
            AnsiConsole.MarkupLine($"  • InputX ID: [green]{_inputXId}[/]");
            AnsiConsole.MarkupLine($"  • InputY ID: [green]{_inputYId}[/]");
            AnsiConsole.MarkupLine($"  • ChatAI ID: [green]{_chatAIId}[/]");
            AnsiConsole.MarkupLine($"  • EndAgent ID: [green]{_endAgentId}[/]");
            
            // NEW: Display workflow monitoring agent IDs
            AnsiConsole.MarkupLine($"  • WorkflowCoordinator ID: [cyan]{_workflowCoordinatorId}[/] (shared)");
            AnsiConsole.WriteLine();

            // Check all relationships
            var startAgentChildren = await _startAgent.GetChildrenAsync();
            var inputXChildren = await _inputX.GetChildrenAsync();
            var inputYChildren = await _inputY.GetChildrenAsync();
            var chatAIChildren = await _chatAI.GetChildrenAsync();
            var endAgentChildren = await _endAgent.GetChildrenAsync();
            var coordinatorChildren = await _workflowCoordinator.GetChildrenAsync();

            var table = new Table();
            table.AddColumn("[bold]Agent[/]");
            table.AddColumn("[bold]Children Count[/]");
            table.AddColumn("[bold]Children IDs[/]");

            // Helper function to identify agent type by extracting GUID from GrainId
            string GetAgentType(GrainId grainId)
            {
                try
                {
                    var grainIdStr = grainId.ToString();
                    // Extract GUID from Orleans GrainId format: "Type/GUID"
                    var parts = grainIdStr.Split('/');
                    if (parts.Length >= 2)
                    {
                        var guidStr = parts[1];
                        if (Guid.TryParse(guidStr, out var extractedGuid))
                        {
                            if (extractedGuid == _startAgentId) return "StartAgent";
                            if (extractedGuid == _inputXId) return "InputX";
                            if (extractedGuid == _inputYId) return "InputY";
                            if (extractedGuid == _chatAIId) return "ChatAI";
                            if (extractedGuid == _endAgentId) return "EndAgent";
                            // NEW: Add workflow coordinator
                            if (extractedGuid == _workflowCoordinatorId) return "WorkflowCoordinator";
                        }
                    }
                    
                    // Fallback: check by grain type name
                    if (grainIdStr.Contains("InputGAgent")) return "Input";
                    if (grainIdStr.Contains("ChatAIGAgent")) return "ChatAI";
                    if (grainIdStr.Contains("WorkflowStartAgent")) return "StartAgent";
                    if (grainIdStr.Contains("workflow-end-agent")) return "EndAgent";
                    if (grainIdStr.Contains("WorkflowCoordinatorGAgent")) return "WorkflowCoordinator";
                    if (grainIdStr.Contains("WorkflowExecutionRecordGAgent")) return "ExecutionRecord";
                    
                    return "Unknown";
                }
                catch
                {
                    return "Unknown";
                }
            }

            table.AddRow(
                "[yellow]StartAgent[/]", 
                $"[green]{startAgentChildren.Count}[/]",
                startAgentChildren.Count > 0 
                    ? string.Join(", ", startAgentChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            table.AddRow(
                "[yellow]InputX[/]", 
                $"[green]{inputXChildren.Count}[/]",
                inputXChildren.Count > 0 
                    ? string.Join(", ", inputXChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            table.AddRow(
                "[yellow]InputY[/]", 
                $"[green]{inputYChildren.Count}[/]",
                inputYChildren.Count > 0 
                    ? string.Join(", ", inputYChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            table.AddRow(
                "[yellow]ChatAI[/]", 
                $"[green]{chatAIChildren.Count}[/]",
                chatAIChildren.Count > 0 
                    ? string.Join(", ", chatAIChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            table.AddRow(
                "[yellow]EndAgent[/]", 
                $"[green]{endAgentChildren.Count}[/]",
                endAgentChildren.Count > 0 
                    ? string.Join(", ", endAgentChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            // NEW: Add workflow coordinator to the table
            table.AddRow(
                "[cyan]WorkflowCoordinator[/]", 
                $"[green]{coordinatorChildren.Count}[/]",
                coordinatorChildren.Count > 0 
                    ? string.Join(", ", coordinatorChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Expected vs Actual (business workflow relationships, ExecutionRecord created per workflow run)
            var expectedTotal = 3 + 2 + 2 + 2 + 1; // StartAgent(3: InputX, InputY, Coordinator) + InputX(2: ChatAI, Coordinator) + InputY(2: ChatAI, Coordinator) + ChatAI(2: EndAgent, Coordinator) + EndAgent(1: Coordinator) = 10 total
            var actualTotal = startAgentChildren.Count + inputXChildren.Count + inputYChildren.Count + chatAIChildren.Count + endAgentChildren.Count;

            if (actualTotal == expectedTotal)
            {
                AnsiConsole.MarkupLine("[bold green]✅ All relationships are correctly established![/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[bold red]❌ Relationship mismatch! Expected: {expectedTotal}, Actual: {actualTotal}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error checking relationships: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static async Task PublishWorkflowEventsAsync()
    {
        try
        {
            if (_startAgent == null || _proxyAgent == null)
            {
                AnsiConsole.MarkupLine("[red]❌ StartAgent or ProxyAgent not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold cyan]🚀 Publishing Workflow Events[/]");
            AnsiConsole.WriteLine();
            
            // ✅ FIX: Let WorkflowCoordinator create ExecutionRecord and read its ID from coordinator state
            var executionName = $"Calculator-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
            
            AnsiConsole.MarkupLine($"[cyan]🎯 Starting execution '[yellow]{executionName}[/][/]");
            AnsiConsole.MarkupLine("[yellow]💡 WorkflowCoordinator will create ExecutionRecord and store ID in its state[/]");
            
            // Create workflow initiation event - WorkflowCoordinator will create ExecutionRecord
            var workflowEvent = new WorkflowEvent
            {
                Direction = EventDirection.Down,
                WorkflowId = Guid.NewGuid(), // Temporary workflow ID, WorkflowCoordinator will create actual ExecutionRecord
                AgentId = _startAgentId,
                TaskResult = $"Initiating {executionName}: 25 × 4",
                WorkflowAgentStatus = WorkflowAgentStatus.Pending,
                Message = $"Calculator workflow '{executionName}' started - processing through chain",
                Metadata = new Dictionary<string, object>
                {
                    { "Operation", "Multiplication" },
                    { "ValueX", 25 },
                    { "ValueY", 4 },
                    { "ExpectedResult", 100 },
                    { "WorkflowChain", "Start → [InputX, InputY] → ChatAI → End" },
                    { "ExecutionName", executionName } // Pass execution name to WorkflowCoordinator
                }
            };

            AnsiConsole.MarkupLine($"[yellow]📤 Publishing WorkflowEvent for execution '[cyan]{executionName}[/]':[/]");
            AnsiConsole.MarkupLine($"  • WorkflowId (Temp): [green]{workflowEvent.WorkflowId}[/]");
            AnsiConsole.MarkupLine($"  • EventType: [green]{workflowEvent.WorkflowEventType}[/]");
            AnsiConsole.MarkupLine($"  • Direction: [green]{workflowEvent.Direction}[/]");
            AnsiConsole.MarkupLine($"  • Task: [green]{workflowEvent.TaskResult}[/]");
            AnsiConsole.MarkupLine($"  • Message: [grey]{workflowEvent.Message}[/]");

            // Use proxy agent's built-in P2P event sending to send event directly to start agent
            await _proxyAgent!.SendEventToAgentAsync(workflowEvent, _startAgent!.GetGrainId());

            AnsiConsole.MarkupLine("[bold green]✅ Workflow event sent via proxy agent successfully![/]");
            AnsiConsole.WriteLine();
            
            // ✅ FIX: Wait for WorkflowCoordinator to process event and create ExecutionRecord
            AnsiConsole.MarkupLine("[yellow]⏳ Waiting for WorkflowCoordinator to create ExecutionRecord...[/]");
            await Task.Delay(2000); // Allow time for event processing
            
            // ✅ NEW: Query execution record by name using new coordinator methods
            var actualExecutionId = await _workflowCoordinator!.GetExecutionRecordIdAsync(executionName);
            
            if (actualExecutionId == Guid.Empty)
            {
                AnsiConsole.MarkupLine("[red]❌ WorkflowCoordinator did not create ExecutionRecord for execution name '[cyan]{executionName}[/]'[/]");
                return;
            }
            
            AnsiConsole.MarkupLine($"[green]✅ WorkflowCoordinator created ExecutionRecord with ID: [cyan]{actualExecutionId}[/][/]");
            AnsiConsole.MarkupLine($"[green]✅ ExecutionRecord registered with name '[cyan]{executionName}[/]' in coordinator dictionary[/]");
            
            // Demonstrate querying execution by name
            var hasExecution = await _workflowCoordinator.HasExecutionAsync(executionName);
            AnsiConsole.MarkupLine($"[yellow]🔍 Verification - execution '[cyan]{executionName}[/]' exists in coordinator: [green]{hasExecution}[/][/]");
            
            // Show total executions count
            var allExecutions = await _workflowCoordinator.GetAllExecutionRecordsAsync();
            AnsiConsole.MarkupLine($"[cyan]📊 Total executions tracked by coordinator: [yellow]{allExecutions.Count}[/][/]");
            AnsiConsole.MarkupLine("[grey]Event sent via SendEventToAgentAsync P2P communication to StartAgent, now propagating through the workflow chain...[/]");
            AnsiConsole.MarkupLine($"[cyan]💡 Use 'Monitor workflow progress' to track this execution: {executionName}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error publishing events: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static async Task MonitorWorkflowProgressAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]📊 PER-EXECUTION WORKFLOW MONITOR[/]");
            AnsiConsole.WriteLine("═══════════════════════════════════════");
            AnsiConsole.WriteLine();

            // ✅ NEW: Check coordinator for executions
            var allExecutions = await _workflowCoordinator!.GetAllExecutionRecordsAsync();
            if (!allExecutions.Any())
            {
                AnsiConsole.MarkupLine("[yellow]⚠️ No workflow executions found in coordinator.[/]");
                AnsiConsole.MarkupLine("[grey]💡 Use 'Publish workflow events' to create an execution to monitor.[/]");
                return;
            }
            
            if (_workflowCoordinator == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Workflow coordinator not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold cyan]📊 WORKFLOW STATUS MONITOR[/]");
            AnsiConsole.WriteLine("═══════════════════════════════════════");
            AnsiConsole.WriteLine();

            // ✅ VALIDATION GRID - Basic Agent Information for Validation
            AnsiConsole.MarkupLine("[bold yellow]🔍 BASIC AGENT VALIDATION:[/]");
            
            // Print agent instance IDs for debugging
            AnsiConsole.MarkupLine("[bold yellow]Agent Instance IDs:[/]");
            AnsiConsole.MarkupLine($"  • StartAgent ID: [green]{_startAgentId}[/]");
            AnsiConsole.MarkupLine($"  • InputX ID: [green]{_inputXId}[/]");
            AnsiConsole.MarkupLine($"  • InputY ID: [green]{_inputYId}[/]");
            AnsiConsole.MarkupLine($"  • ChatAI ID: [green]{_chatAIId}[/]");
            AnsiConsole.MarkupLine($"  • EndAgent ID: [green]{_endAgentId}[/]");
            AnsiConsole.MarkupLine($"  • WorkflowCoordinator ID: [cyan]{_workflowCoordinatorId}[/] (shared)");
            AnsiConsole.MarkupLine($"  • ExecutionRecord: [yellow]Created per workflow execution[/]");
            AnsiConsole.WriteLine();

            // Check all relationships for validation
            var startAgentChildren = await _startAgent.GetChildrenAsync();
            var inputXChildren = await _inputX.GetChildrenAsync();
            var inputYChildren = await _inputY.GetChildrenAsync();
            var chatAIChildren = await _chatAI.GetChildrenAsync();
            var endAgentChildren = await _endAgent.GetChildrenAsync();
            var workflowCoordinatorChildren = await _workflowCoordinator.GetChildrenAsync();

            // Helper function to identify agent type by extracting GUID from GrainId
            string GetAgentType(GrainId grainId)
            {
                try
                {
                    var grainIdStr = grainId.ToString();
                    var parts = grainIdStr.Split('/');
                    if (parts.Length >= 2)
                    {
                        var guidStr = parts[1];
                        if (Guid.TryParse(guidStr, out var extractedGuid))
                        {
                            if (extractedGuid == _startAgentId) return "StartAgent";
                            if (extractedGuid == _inputXId) return "InputX";
                            if (extractedGuid == _inputYId) return "InputY";
                            if (extractedGuid == _chatAIId) return "ChatAI";
                            if (extractedGuid == _endAgentId) return "EndAgent";
                            if (extractedGuid == _workflowCoordinatorId) return "WorkflowCoordinator";
                        }
                    }
                    
                    if (grainIdStr.Contains("InputGAgent")) return "Input";
                    if (grainIdStr.Contains("ChatAIGAgent")) return "ChatAI";
                    if (grainIdStr.Contains("WorkflowStartAgent")) return "StartAgent";
                    if (grainIdStr.Contains("workflow-end-agent")) return "EndAgent";
                    if (grainIdStr.Contains("WorkflowCoordinatorGAgent")) return "WorkflowCoordinator";
                    if (grainIdStr.Contains("WorkflowExecutionRecordGAgent")) return "ExecutionRecord";
                    
                    return "Unknown";
                }
                catch
                {
                    return "Unknown";
                }
            }

            var validationTable = new Table();
            validationTable.AddColumn("[bold]Agent[/]");
            validationTable.AddColumn("[bold]Children Count[/]");
            validationTable.AddColumn("[bold]Children IDs[/]");
            validationTable.Border = TableBorder.Rounded;

            validationTable.AddRow(
                "[yellow]StartAgent[/]", 
                $"[green]{startAgentChildren.Count}[/]",
                startAgentChildren.Count > 0 
                    ? string.Join(", ", startAgentChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            validationTable.AddRow(
                "[yellow]InputX[/]", 
                $"[green]{inputXChildren.Count}[/]",
                inputXChildren.Count > 0 
                    ? string.Join(", ", inputXChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            validationTable.AddRow(
                "[yellow]InputY[/]", 
                $"[green]{inputYChildren.Count}[/]",
                inputYChildren.Count > 0 
                    ? string.Join(", ", inputYChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            validationTable.AddRow(
                "[yellow]ChatAI[/]", 
                $"[green]{chatAIChildren.Count}[/]",
                chatAIChildren.Count > 0 
                    ? string.Join(", ", chatAIChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            validationTable.AddRow(
                "[yellow]EndAgent[/]", 
                $"[green]{endAgentChildren.Count}[/]",
                endAgentChildren.Count > 0 
                    ? string.Join(", ", endAgentChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            validationTable.AddRow(
                "[cyan]WorkflowCoordinator[/]", 
                $"[green]{workflowCoordinatorChildren.Count}[/]",
                workflowCoordinatorChildren.Count > 0 
                    ? string.Join(", ", workflowCoordinatorChildren.Select(c => $"{GetAgentType(c)}({c})")).EscapeMarkup()
                    : "[grey]None[/]");

            validationTable.AddRow(
                "[yellow]ExecutionRecord[/]", 
                "[grey]Per execution[/]",
                "[grey]Created per workflow, cleared by Coordinator after completion[/]");

            AnsiConsole.Write(validationTable);
            AnsiConsole.WriteLine();

            // Expected vs Actual validation (ExecutionRecord cleared by WorkflowCoordinator after completion)
            var expectedTotal = 3 + 2 + 2 + 2 + 1 + 0; // StartAgent(3: InputX, InputY, Coordinator) + InputX(2: ChatAI, Coordinator) + InputY(2: ChatAI, Coordinator) + ChatAI(2: EndAgent, Coordinator) + EndAgent(1: Coordinator) + WorkflowCoordinator(0: ExecutionRecord cleared) = 10 total
            var actualTotal = startAgentChildren.Count + inputXChildren.Count + inputYChildren.Count + chatAIChildren.Count + endAgentChildren.Count + workflowCoordinatorChildren.Count;

            if (actualTotal == expectedTotal)
            {
                AnsiConsole.MarkupLine("[bold green]✅ All relationships are correctly established![/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[bold red]❌ Relationship mismatch! Expected: {expectedTotal}, Actual: {actualTotal}[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold cyan]━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━[/]");
            AnsiConsole.WriteLine();

            // ✅ ORIGINAL WORKFLOW STATUS MONITOR - Agent status after event processing  
            AnsiConsole.MarkupLine("[bold yellow]📈 ORIGINAL WORKFLOW STATUS MONITOR:[/]");
            AnsiConsole.WriteLine();

            // Wait for event processing to complete
            AnsiConsole.MarkupLine("[grey]⏳ Waiting 2 seconds for event processing...[/]");
            await Task.Delay(2000);

            var agents = new (string Name, IGAgentPlus Agent, string Type)[]
            {
                ("StartAgent", _startAgent as IGAgentPlus, "WorkflowStart"),
                ("InputX", _inputX as IGAgentPlus, "Input"),
                ("InputY", _inputY as IGAgentPlus, "Input"),
                ("ChatAI", _chatAI as IGAgentPlus, "ChatAI"),
                ("EndAgent", _endAgent as IGAgentPlus, "WorkflowEnd")
            };

            AnsiConsole.MarkupLine($"[bold cyan]📈 Workflow Status Monitor • {DateTime.Now:HH:mm:ss}[/]");

            // Create status table
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue);

            table.AddColumn(new TableColumn("[bold]Agent[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Type[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Children[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Details[/]").LeftAligned());

            foreach (var (name, agent, type) in agents)
            {
                try
                {
                    var children = await agent.GetChildrenAsync();
                    var childrenCount = children.Count;
                    var childrenDisplay = childrenCount > 0 
                        ? $"[green]{childrenCount}[/]" 
                        : $"[grey]{childrenCount}[/]";

                    string status = "[green]✅ Active[/]";
                    string details = "";

                    switch (agent)
                    {
                        case IChatAIGAgentPlus chatAI:
                            var chatState = await chatAI.GetStateAsync();
                            details = $"Interactions: {chatState.TotalInteractions}" +
                                     (chatState.LastActivityTime != default 
                                         ? $"\nLast Activity: {chatState.LastActivityTime:HH:mm:ss}"
                                         : "\nLast Activity: None") +
                                     (!string.IsNullOrEmpty(chatState.LastResponse) 
                                         ? $"\nLast Response: {chatState.LastResponse.EscapeMarkup()}" 
                                         : "\nLast Response: None");
                            break;

                        case IInputGAgentPlus inputAgent:
                            details = "Input configured\nReady for processing";
                            break;

                        case IWorkflowStartAgent startAgent:
                            details = $"Workflow initiator\nChildren: {childrenCount} agents";
                            break;

                        case IWorkflowEndAgent endAgent:
                            details = "Workflow terminator\nMonitoring completion";
                            break;

                        default:
                            details = "Standard agent";
                            break;
                    }

                    table.AddRow(
                        $"[bold yellow]{name}[/]",
                        $"[blue]{type}[/]",
                        status,
                        childrenDisplay,
                        details
                    );
                }
                catch (Exception ex)
                {
                    table.AddRow(
                        $"[bold yellow]{name}[/]",
                        $"[blue]{type}[/]",
                        "[red]❌ Error[/]",
                        "[red]?[/]",
                        $"[red]{ex.Message.EscapeMarkup()}[/]"
                    );
                }
            }

            AnsiConsole.Write(table);

            // Show workflow chain
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Workflow Chain:[/] [grey]Start → {Markup.Escape("[InputX, InputY]")} → ChatAI → End[/]");

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]✅ Workflow status monitoring completed![/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold cyan]━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━[/]");
            AnsiConsole.WriteLine();
            
            // Get WorkflowCoordinator state
            var coordinatorState = await _workflowCoordinator.GetStateAsync();
            
            // Display Overall Coordinator Status
            AnsiConsole.MarkupLine("[bold yellow]🎯 COORDINATOR STATUS:[/]");
            
            var coordinatorTable = new Table();
            coordinatorTable.AddColumn("Property");
            coordinatorTable.AddColumn("Value");
            coordinatorTable.Border = TableBorder.Rounded;
            
            coordinatorTable.AddRow("Overall Status", (coordinatorState.WorkflowStatus.ToString()));
            // REMOVED: Term system no longer used (direct AgentId correlation per design document)
            coordinatorTable.AddRow("Round ID", coordinatorState.RoundId.ToString());
            coordinatorTable.AddRow("Last Running Time", coordinatorState.LastRunningTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "[grey]Never[/]");
            coordinatorTable.AddRow("All Work Units Finished", coordinatorState.CheckAllWorkUnitFinished() ? "[green]✅ Yes[/]" : "[yellow]❌ No[/]");
            coordinatorTable.AddRow("Current Execution ID", coordinatorState.CurrentExecutionRecordId.ToString());
            coordinatorTable.AddRow("Content", !string.IsNullOrEmpty(coordinatorState.Content) ? coordinatorState.Content.EscapeMarkup() : "[grey]None[/]");
            
            AnsiConsole.Write(coordinatorTable);
            AnsiConsole.WriteLine();

            // Display Business Agent Status from Coordinator
            AnsiConsole.MarkupLine("[bold yellow]📈 BUSINESS AGENT STATUS (from Coordinator):[/]");
            AnsiConsole.MarkupLine("[grey]💡 Note: Agents may appear multiple times if they have multiple children (e.g., ChatAI → EndAgent, ChatAI → WorkflowCoordinator)[/]");
            AnsiConsole.WriteLine();
            
            // Debug: Show consolidated WorkUnitInfo structure (design document implementation)
            if (coordinatorState.CurrentWorkUnitInfos.Any())
            {
                AnsiConsole.MarkupLine("[grey]🔍 Debug - Consolidated WorkUnitInfo from Coordinator:[/]");
                foreach (var workUnit in coordinatorState.CurrentWorkUnitInfos)
                {
                    AnsiConsole.MarkupLine($"[grey]  • NodeId: {workUnit.NodeId}, AgentId: {workUnit.AgentId}[/]");
                    AnsiConsole.MarkupLine($"[grey]    Name: '{workUnit.Name}', Type: '{workUnit.AgentType}'[/]");
                    if (workUnit.NextAgentId != Guid.Empty)
                    {
                        AnsiConsole.MarkupLine($"[grey]    → NextNodeId: {workUnit.NextNodeId}, NextAgentId: {workUnit.NextAgentId}[/]");
                    }
                    if (workUnit.ExtendedData.Any())
                    {
                        AnsiConsole.MarkupLine($"[grey]    ExtendedData: {string.Join(", ", workUnit.ExtendedData.Select(kv => $"{kv.Key}={kv.Value}"))}[/]");
                    }
                }
                AnsiConsole.WriteLine();
            }
            
            var agentStatusTable = new Table();
            agentStatusTable.AddColumn("Agent");
            agentStatusTable.AddColumn("Status");
            agentStatusTable.AddColumn("Next Agent");
            agentStatusTable.AddColumn("Extended Data");
            agentStatusTable.Border = TableBorder.Rounded;
            
            if (coordinatorState.CurrentWorkUnitInfos.Any())
            {
                foreach (var workUnit in coordinatorState.CurrentWorkUnitInfos)
                {
                    var statusDisplay = GetWorkUnitStatusColor(workUnit.UnitStatusEnum);
                    
                    // Use consolidated WorkUnitInfo fields - much simpler!
                    var agentName = GetAgentDisplayName(workUnit);
                    
                    // Find next agent by NextAgentId for better display
                    var nextAgent = "[grey]-[/]";
                    if (workUnit.NextAgentId != Guid.Empty)
                    {
                        var nextWorkUnit = coordinatorState.CurrentWorkUnitInfos
                            .FirstOrDefault(w => w.AgentId == workUnit.NextAgentId);
                        nextAgent = nextWorkUnit != null 
                            ? GetAgentDisplayName(nextWorkUnit)
                            : workUnit.NextAgentId.ToString()[..8] + "...";
                    }
                    
                    var extendedData = workUnit.ExtendedData.Any() 
                        ? string.Join(", ", workUnit.ExtendedData.Select(kv => $"{kv.Key}={kv.Value}"))
                        : "[grey]None[/]";
                    
                    agentStatusTable.AddRow(
                        agentName,
                        statusDisplay,
                        nextAgent,
                        extendedData.EscapeMarkup()
                    );
                }
            }
            else
            {
                agentStatusTable.AddRow("[grey]No work units configured[/]", "[grey]-[/]", "[grey]-[/]", "[grey]-[/]");
            }
            
            AnsiConsole.Write(agentStatusTable);
            AnsiConsole.WriteLine();

            // Try to get ExecutionRecord state if available
            if (coordinatorState.CurrentExecutionRecordId != Guid.Empty)
            {
                try
                {
                    var executionRecord = _client!.GetGrain<Aevatar.GAgents.Workflow.Core.IWorkflowExecutionRecordGAgentPlus>(coordinatorState.CurrentExecutionRecordId);
                    var executionState = await executionRecord.GetStateAsync();
                    
                    AnsiConsole.MarkupLine($"[bold yellow]📝 EXECUTION RECORD: {GetExecutionName(executionState)}[/]");
                    
                    var executionTable = new Table();
                    executionTable.AddColumn("Property");
                    executionTable.AddColumn("Value");
                    executionTable.Border = TableBorder.Rounded;
                    
                    executionTable.AddRow("Execution Status", GetStatusColor(executionState.Status.ToString()));
                    executionTable.AddRow("Workflow ID", executionState.WorkflowId.ToString());
                    executionTable.AddRow("Started", executionState.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    executionTable.AddRow("Duration", GetDurationDisplay(executionState.StartTime, executionState.EndTime));
                    executionTable.AddRow("End Time", executionState.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "[grey]Not finished[/]");
                    executionTable.AddRow("Initial Content", !string.IsNullOrEmpty(executionState.InitContent) ? executionState.InitContent.EscapeMarkup() : "[grey]None[/]");
                    
                    AnsiConsole.Write(executionTable);
                    AnsiConsole.WriteLine();

                    // Display Detailed Agent Execution Records
                    AnsiConsole.MarkupLine("[bold yellow]🔍 DETAILED AGENT EXECUTION (from ExecutionRecord):[/]");
                    
                    var detailTable = new Table();
                    detailTable.AddColumn("Agent");
                    detailTable.AddColumn("Status");
                    detailTable.AddColumn("Duration");
                    detailTable.AddColumn("Failure Summary");
                    detailTable.Border = TableBorder.Rounded;
                    
                    if (executionState.WorkUnitRecords.Any())
                    {
                        foreach (var record in executionState.WorkUnitRecords)
                        {
                            var duration = GetDurationDisplay(record.StartTime, record.EndTime);
                            var failure = !string.IsNullOrEmpty(record.FailureSummary) ? record.FailureSummary.EscapeMarkup() : "[grey]-[/]";
                            
                            detailTable.AddRow(
                                GetAgentDisplayName(record.WorkUnitGrainId),
                                GetStatusColor(record.Status.ToString()),
                                duration,
                                failure
                            );
                        }
                    }
                    else
                    {
                        detailTable.AddRow("[grey]No execution records yet[/]", "[grey]-[/]", "[grey]-[/]", "[grey]-[/]");
                    }
                    
                    AnsiConsole.Write(detailTable);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠️ Could not fetch ExecutionRecord state: {ex.Message.EscapeMarkup()}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]📝 EXECUTION RECORD: [grey]No active execution record[/][/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]✅ Workflow status monitoring completed![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error monitoring progress: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static async Task MonitorPerExecutionProgressAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]📊 PER-EXECUTION WORKFLOW MONITOR[/]");
            AnsiConsole.WriteLine("═══════════════════════════════════════");
            AnsiConsole.WriteLine();

            // ✅ NEW: Query all executions from WorkflowCoordinator dictionary
            var allExecutions = await _workflowCoordinator!.GetAllExecutionRecordsAsync();
            
            if (!allExecutions.Any())
            {
                AnsiConsole.MarkupLine("[yellow]⚠️ No workflow executions found in coordinator.[/]");
                AnsiConsole.MarkupLine("[grey]💡 Use 'Publish workflow events' to create an execution to monitor.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[bold yellow]🎯 ALL EXECUTIONS OVERVIEW ({allExecutions.Count} total):[/]");
            AnsiConsole.WriteLine();

            // Display all active executions
            var executionsTable = new Table();
            executionsTable.AddColumn("Execution Name");
            executionsTable.AddColumn("Execution ID");
            executionsTable.AddColumn("Created At");
            executionsTable.AddColumn("Duration");
            executionsTable.AddColumn("Status");
            executionsTable.Border = TableBorder.Rounded;

            foreach (var execution in allExecutions)
            {
                try
                {
                    // ✅ NEW: Use execution name and ID from coordinator dictionary
                    var executionRecord = _client!.GetGrain<Aevatar.GAgents.Workflow.Core.IWorkflowExecutionRecordGAgentPlus>(execution.Value);
                    var executionState = await executionRecord.GetStateAsync();
                    var duration = GetDurationDisplay(executionState.StartTime, executionState.EndTime);
                    var status = GetStatusColor(executionState.Status.ToString());
                    
                    executionsTable.AddRow(
                        $"[cyan]{execution.Key}[/]", // Execution name from dictionary key
                        execution.Value.ToString()[..8] + "...", // Execution ID from dictionary value
                        executionState.StartTime.ToString("HH:mm:ss"),
                        duration,
                        status
                    );
                }
                catch (Exception ex)
                {
                    executionsTable.AddRow(
                        $"[cyan]{execution.Key}[/]", // Execution name from dictionary key
                        execution.Value.ToString()[..8] + "...", // Execution ID from dictionary value
                        "Unknown",
                        "[red]Error[/]",
                        $"[red]{ex.Message.EscapeMarkup()}[/]"
                    );
                }
            }

            AnsiConsole.Write(executionsTable);
            AnsiConsole.WriteLine();

            // Allow user to select which execution to monitor in detail
            AnsiConsole.MarkupLine("[bold yellow]📋 SELECT EXECUTION FOR DETAILED MONITORING:[/]");
            
            // ✅ NEW: Create choices from coordinator execution dictionary
            var executionChoices = allExecutions
                .Select(e => $"{e.Key} ({e.Value.ToString()[..8]}...)")
                .Concat(new[] { "Show all executions summary only" })
                .ToArray();
            
            var selectedChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Which execution would you like to monitor in detail?[/]")
                    .AddChoices(executionChoices));

            if (selectedChoice == "Show all executions summary only")
            {
                AnsiConsole.MarkupLine("[green]✅ Executions overview completed![/]");
                return;
            }

            // ✅ NEW: Find the selected execution from coordinator dictionary
            var selectedExecution = allExecutions
                .FirstOrDefault(e => selectedChoice.Contains(e.Value.ToString()[..8]));
                
            if (selectedExecution.Key == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Selected execution not found![/]");
                return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold cyan]━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━[/]");
            AnsiConsole.WriteLine();

            // Display detailed monitoring for selected execution
            AnsiConsole.MarkupLine($"[bold yellow]🔍 DETAILED MONITORING: {selectedExecution.Key}[/]");
            AnsiConsole.WriteLine();

            try
            {
                // ✅ NEW: Use execution ID from coordinator dictionary 
                var executionRecord = _client!.GetGrain<Aevatar.GAgents.Workflow.Core.IWorkflowExecutionRecordGAgentPlus>(selectedExecution.Value);
                var executionState = await executionRecord.GetStateAsync();
                
                // Execution summary
                var executionSummaryTable = new Table();
                executionSummaryTable.AddColumn("Property");
                executionSummaryTable.AddColumn("Value");
                executionSummaryTable.Border = TableBorder.Rounded;
                
                executionSummaryTable.AddRow("Execution Name", $"[cyan]{selectedExecution.Key}[/]");
                executionSummaryTable.AddRow("Execution ID", selectedExecution.Value.ToString());
                executionSummaryTable.AddRow("Status", GetStatusColor(executionState.Status.ToString()));
                executionSummaryTable.AddRow("Started", executionState.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                executionSummaryTable.AddRow("Duration", GetDurationDisplay(executionState.StartTime, executionState.EndTime));
                executionSummaryTable.AddRow("End Time", executionState.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "[grey]Not finished[/]");
                executionSummaryTable.AddRow("Initial Content", !string.IsNullOrEmpty(executionState.InitContent) ? executionState.InitContent.EscapeMarkup() : "[grey]None[/]");
                
                AnsiConsole.Write(executionSummaryTable);
                AnsiConsole.WriteLine();

                // Work Unit Records - THIS IS THE KEY CHANGE: Per-execution status tracking
                AnsiConsole.MarkupLine("[bold yellow]📈 WORK UNIT STATUS (Per-Execution Tracking):[/]");
                AnsiConsole.WriteLine();
                
                var workUnitTable = new Table();
                workUnitTable.AddColumn("Agent");
                workUnitTable.AddColumn("Status");
                workUnitTable.AddColumn("Duration");
                workUnitTable.AddColumn("Failure Summary");
                workUnitTable.Border = TableBorder.Rounded;

                if (executionState.WorkUnitRecords.Any())
                {
                    foreach (var record in executionState.WorkUnitRecords)
                    {
                        var duration = GetDurationDisplay(record.StartTime, record.EndTime);
                        var failure = !string.IsNullOrEmpty(record.FailureSummary) ? record.FailureSummary.EscapeMarkup() : "[grey]-[/]";
                        
                        // Find the corresponding WorkUnitInfo to get proper agent display name
                        var workUnitInfo = executionState.WorkUnitInfos
                            .FirstOrDefault(w => w.AgentId.ToString() == record.WorkUnitGrainId);
                        
                        // Use the same GetAgentDisplayName method as original monitoring
                        var agentName = workUnitInfo != null 
                            ? GetAgentDisplayName(workUnitInfo) 
                            : record.WorkUnitGrainId[..8] + "...";
                        
                        workUnitTable.AddRow(
                            agentName,
                            GetStatusColor(record.Status.ToString()),
                            duration,
                            failure
                        );
                    }
                }
                else
                {
                    workUnitTable.AddRow("[grey]No work unit records yet[/]", "[grey]-[/]", "[grey]-[/]", "[grey]-[/]");
                }
                
                AnsiConsole.Write(workUnitTable);
                AnsiConsole.WriteLine();

                AnsiConsole.MarkupLine($"[bold green]✅ Detailed monitoring for '{selectedExecution.Key}' completed![/]");
                AnsiConsole.MarkupLine("[cyan]💡 Each execution maintains its own work unit status - no more shared state confusion![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error getting execution details: {ex.Message.EscapeMarkup()}[/]");
            }

        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error monitoring per-execution progress: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    /// <summary>
    /// ✅ NEW: Demonstrate execution record dictionary query features
    /// </summary>
    private static async Task DemoExecutionRecordQueriesAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]🗃️ EXECUTION RECORD DICTIONARY DEMO[/]");
            AnsiConsole.WriteLine("═══════════════════════════════════════════════════");
            AnsiConsole.WriteLine();
            
            if (_workflowCoordinator == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Workflow coordinator not initialized. Please run 'Initialize workflow agents' first.[/]");
                return;
            }

            // 1. Get all execution records
            AnsiConsole.MarkupLine("[bold yellow]1️⃣ Getting all execution records from coordinator...[/]");
            var allExecutions = await _workflowCoordinator.GetAllExecutionRecordsAsync();
            
            if (allExecutions.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]⚠️ No executions found. Create some executions using 'Publish workflow events' first.[/]");
                return;
            }
            
            AnsiConsole.MarkupLine($"[green]✅ Found {allExecutions.Count} executions in coordinator dictionary:[/]");
            
            var allTable = new Table();
            allTable.AddColumn("Execution Name");
            allTable.AddColumn("Execution ID");
            allTable.Border = TableBorder.Rounded;
            
            foreach (var execution in allExecutions)
            {
                allTable.AddRow(
                    $"[cyan]{execution.Key}[/]",
                    execution.Value.ToString()[..8] + "..."
                );
            }
            AnsiConsole.Write(allTable);
            AnsiConsole.WriteLine();
            
            // 2. Query specific execution by name
            if (allExecutions.Any())
            {
                var firstExecutionName = allExecutions.First().Key;
                AnsiConsole.MarkupLine($"[bold yellow]2️⃣ Querying execution by name: '[cyan]{firstExecutionName}[/]'[/]");
                
                var executionId = await _workflowCoordinator.GetExecutionRecordIdAsync(firstExecutionName);
                if (executionId != Guid.Empty)
                {
                    AnsiConsole.MarkupLine($"[green]✅ Found execution ID: [cyan]{executionId}[/][/]");
                    
                    // Get execution record details
                    var executionRecord = _client!.GetGrain<Aevatar.GAgents.Workflow.Core.IWorkflowExecutionRecordGAgentPlus>(executionId);
                    var executionState = await executionRecord.GetStateAsync();
                    
                    AnsiConsole.MarkupLine($"[cyan]   Status: {GetStatusColor(executionState.Status.ToString())}[/]");
                    AnsiConsole.MarkupLine($"[cyan]   Start Time: {executionState.StartTime.ToString("HH:mm:ss")}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Execution not found by name query[/]");
                }
                AnsiConsole.WriteLine();
                
                // 3. Check execution existence
                AnsiConsole.MarkupLine($"[bold yellow]3️⃣ Checking if execution '[cyan]{firstExecutionName}[/]' exists...[/]");
                var exists = await _workflowCoordinator.HasExecutionAsync(firstExecutionName);
                AnsiConsole.MarkupLine($"[green]✅ Execution exists: [yellow]{exists}[/][/]");
                AnsiConsole.WriteLine();
                
                // 4. Query non-existent execution
                var fakeExecutionName = "NonExistentExecution_12345";
                AnsiConsole.MarkupLine($"[bold yellow]4️⃣ Querying non-existent execution: '[cyan]{fakeExecutionName}[/]'[/]");
                var fakeExecutionId = await _workflowCoordinator.GetExecutionRecordIdAsync(fakeExecutionName);
                var fakeExists = await _workflowCoordinator.HasExecutionAsync(fakeExecutionName);
                
                AnsiConsole.MarkupLine($"[yellow]   Execution ID: {(fakeExecutionId == Guid.Empty ? "[red]Empty (not found)[/]" : fakeExecutionId.ToString())}[/]");
                AnsiConsole.MarkupLine($"[yellow]   Exists: [red]{fakeExists}[/][/]");
            }
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]✅ Execution record dictionary demo completed![/]");
            AnsiConsole.MarkupLine("[cyan]💡 Key Benefits:[/]");
            AnsiConsole.MarkupLine("[grey]  • Query executions by meaningful names instead of GUIDs[/]");
            AnsiConsole.MarkupLine("[grey]  • Historical execution tracking (records persist after completion)[/]");
            AnsiConsole.MarkupLine("[grey]  • No more local tracking lists - coordinator manages everything[/]");
            AnsiConsole.MarkupLine("[grey]  • Support for multiple concurrent executions with unique names[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error in execution record demo: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static async Task RunCompleteWorkflowAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]🔄 Running Complete Workflow[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[yellow]This will run all steps in sequence:[/]");
            AnsiConsole.MarkupLine("  1. Initialize agents");
            AnsiConsole.MarkupLine("  2. Setup relationships");
            AnsiConsole.MarkupLine("  3. Check status");
            AnsiConsole.MarkupLine("  4. Publish events");
            AnsiConsole.MarkupLine("  5. Monitor progress");
            AnsiConsole.MarkupLine("  6. Monitor per-execution progress");
            AnsiConsole.WriteLine();

            var confirm = AnsiConsole.Confirm("[yellow]Continue with complete workflow?[/]");
            if (!confirm) return;

            await InitializeWorkflowAgentsAsync();
            AnsiConsole.WriteLine();

            await SetupWorkflowRelationshipsAsync();
            AnsiConsole.WriteLine();

            await CheckRelationshipStatusAsync();
            AnsiConsole.WriteLine();

            await PublishWorkflowEventsAsync();
            AnsiConsole.WriteLine();

            await MonitorWorkflowProgressAsync();
            AnsiConsole.WriteLine();

            await MonitorPerExecutionProgressAsync();

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]🎯 Complete workflow execution finished![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error running complete workflow: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    // NEW: Helper methods for status display formatting
    private static string GetStatusColor(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "[yellow]⏳ Pending[/]",
            "inprogress" => "[blue]🔄 InProgress[/]", 
            "running" => "[blue]🔄 Running[/]",
            "completed" => "[green]✅ Completed[/]",
            "finished" => "[green]✅ Finished[/]",
            "failed" => "[red]❌ Failed[/]",
            _ => $"[grey]{status}[/]"
        };
    }

    private static string GetWorkUnitStatusColor(Aevatar.GAgents.Workflow.Core.Models.WorkerUnitStatusEnum status)
    {
        return status switch
        {
            Aevatar.GAgents.Workflow.Core.Models.WorkerUnitStatusEnum.Pending => "[yellow]⏳ Pending[/]",
            Aevatar.GAgents.Workflow.Core.Models.WorkerUnitStatusEnum.InProgress => "[blue]🔄 InProgress[/]",
            Aevatar.GAgents.Workflow.Core.Models.WorkerUnitStatusEnum.Finished => "[green]✅ Finished[/]",
            _ => $"[grey]{status}[/]"
        };
    }

    private static string GetAgentDisplayName(Aevatar.GAgents.Workflow.Core.Models.WorkUnitInfo workUnit)
    {
        // Use the Name field from consolidated WorkUnitInfo (design document implementation)
        if (!string.IsNullOrEmpty(workUnit.Name))
            return workUnit.Name;

        // Fallback to AgentType if Name is not set
        if (!string.IsNullOrEmpty(workUnit.AgentType))
            return workUnit.AgentType;

        // Final fallback to AgentId
        return workUnit.AgentId.ToString()[..8] + "...";
    }

    private static string GetAgentDisplayName(string grainIdOrName)
    {
        // Legacy method for backward compatibility - simplified since WorkUnitInfo now has proper fields
        if (string.IsNullOrEmpty(grainIdOrName))
            return "[Empty]";

        // If it looks like a GUID, show shortened version
        if (Guid.TryParse(grainIdOrName, out var guid))
        {
            return guid.ToString()[..8] + "...";
        }

        // Otherwise return as-is (likely already a name)
        return grainIdOrName;
    }

    private static string GetExecutionName(Aevatar.GAgents.Workflow.Core.States.WorkflowExecutionRecordStatePlus state)
    {
        return $"Calculator-{state.StartTime:yyyy-MM-dd-HH-mm-ss}";
    }

    private static string GetDurationDisplay(DateTime startTime, DateTime? endTime)
    {
        if (endTime.HasValue)
        {
            var duration = endTime.Value - startTime;
            return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
        else
        {
            var duration = DateTime.UtcNow - startTime;
            return $"[yellow]{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2} (ongoing)[/]";
        }
    }

    // ✅ REMOVED: CreateAndRegisterExecutionRecordAsync - WorkflowCoordinator now handles ExecutionRecord lifecycle completely


}