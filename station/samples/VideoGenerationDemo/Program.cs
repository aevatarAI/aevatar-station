using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.Core.Streaming.Extensions;
using Spectre.Console;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VideoGenerationDemo;

class Program
{
    private static IHost? _host;
    private static IClusterClient? _client;

    static async Task Main(string[] args)
    {
        Console.Title = "🎬 Aevatar Video Generation Demo";
        
        AnsiConsole.Write(
            new FigletText("Video Generation Demo")
                .LeftJustified()
                .Color(Color.Cyan1));

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]🎬 Welcome to the Aevatar Video Generation Agent Demo![/]");
        AnsiConsole.MarkupLine("[grey]This demo showcases AI-powered video generation capabilities[/]");
        AnsiConsole.WriteLine();

        try
        {
            await InitializeOrleansClientAsync();
            await RunDemoAsync();
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
        IHostBuilder builder = Host.CreateDefaultBuilder()
        .UseOrleansClient(client =>
        {
            //client.UseLocalhostClustering();
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
                // client.UseLocalhostClustering(gatewayPort: 20001)
                // .AddMemoryStreams(AevatarCoreConstants.StreamProvider);
                .AddAevatarKafkaStreaming("Aevatar", options =>
                {
                    options.BrokerList = new List<string> { "localhost:9092" };
                    options.ConsumerGroupId = "Aevatar";
                    options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                    var partitions = 8; // Multiple partitions for load distribution
                    var replicationFactor = (short)1;  // ReplicationFactor should be short
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
        .ConfigureLogging(logging => logging.AddConsole())
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

    private static async Task RunDemoAsync()
    {
        if (_client == null)
        {
            AnsiConsole.MarkupLine("[red]❌ Orleans client not initialized[/]");
            return;
        }

        var agent = _client.GetGrain<IVideoGenerationGAgent>(Guid.NewGuid());

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]🎬 What would you like to do?[/]")
                    .AddChoices(new[]
                    {
                        "📝 Generate video from text",
                        "🖼️ Generate video from image",
                        "📊 Show agent description",
                        "🏃 Run performance test",
                        "❌ Exit"
                    }));

            switch (choice)
            {
                case "📝 Generate video from text":
                    await DemoTextToVideo(agent);
                    break;
                case "🖼️ Generate video from image":
                    await DemoImageToVideo(agent);
                    break;
                case "📊 Show agent description":
                    await ShowAgentDescription(agent);
                    break;
                case "🏃 Run performance test":
                    await RunPerformanceTest(agent);
                    break;
                case "❌ Exit":
                    AnsiConsole.MarkupLine("[yellow]👋 Thanks for using the Video Generation Demo![/]");
                    return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private static async Task DemoTextToVideo(IVideoGenerationGAgent agent)
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]📝 Text-to-Video Generation[/]");
            AnsiConsole.WriteLine();

            var prompt = AnsiConsole.Ask<string>("[green]Enter your video prompt:[/]");
            
            var customizeOptions = AnsiConsole.Confirm("[yellow]Would you like to customize generation options?[/]");
            
            VideoGenerationConfigDto? options = null;
            if (customizeOptions)
            {
                options = await GetCustomVideoOptions();
            }

            AnsiConsole.WriteLine();

            // Start the video generation task
            var taskId = await agent.GenerateVideoFromTextAsync(prompt, options);
            AnsiConsole.MarkupLine($"[green]✅ Video generation task started with ID: {taskId}[/]");
            AnsiConsole.WriteLine();

            // Poll for completion
            await PollForVideoCompletion(agent, taskId, "Text-to-Video");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error during text-to-video generation: {ex.Message.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine();
        }
    }

    private static async Task DemoImageToVideo(IVideoGenerationGAgent agent)
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]🖼️ Image-to-Video Generation[/]");
            AnsiConsole.WriteLine();

            var imageUrl = AnsiConsole.Ask<string>("[green]Enter image URL:[/]");
            var prompt = AnsiConsole.Ask<string>("[green]Enter animation prompt (optional):[/]", "");
            
            var customizeOptions = AnsiConsole.Confirm("[yellow]Would you like to customize generation options?[/]");
            
            VideoGenerationConfigDto? options = null;
            if (customizeOptions)
            {
                options = await GetCustomVideoOptions();
            }

            AnsiConsole.WriteLine();

            // Start the video generation task
            var taskId = await agent.GenerateVideoFromImageAsync(imageUrl, prompt, options);
            AnsiConsole.MarkupLine($"[green]✅ Video generation task started with ID: {taskId}[/]");
            AnsiConsole.WriteLine();

            // Poll for completion
            await PollForVideoCompletion(agent, taskId, "Image-to-Video");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error during image-to-video generation: {ex.Message.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine();
        }
    }

    private static async Task<VideoGenerationConfigDto> GetCustomVideoOptions()
    {
        var options = new VideoGenerationConfigDto();

        options.Duration = AnsiConsole.Prompt(
            new TextPrompt<int>("[cyan]Duration (seconds):[/]")
                .DefaultValue(5)
                .Validate(d => d is > 0 and <= 60 ? ValidationResult.Success() : ValidationResult.Error("Duration must be between 1-60 seconds")));

        options.Resolution = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Resolution:[/]")
                .AddChoices("720p", "1080p", "4K")
                .UseConverter(r => r switch
                {
                    "720p" => "1280x720",
                    "1080p" => "1920x1080", 
                    "4K" => "3840x2160",
                    _ => r
                }));

        options.AspectRatio = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Aspect Ratio:[/]")
                .AddChoices("16:9", "9:16", "1:1", "4:3"));

        options.Style = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Style:[/]")
                .AddChoices("realistic", "artistic", "cartoon", "cinematic", "abstract"));

        options.MotionStrength = AnsiConsole.Prompt(
            new TextPrompt<float>("[cyan]Motion Strength (0.1-1.0):[/]")
                .DefaultValue(0.7f)
                .Validate(m => m is >= 0.1f and <= 1.0f ? ValidationResult.Success() : ValidationResult.Error("Motion strength must be between 0.1-1.0")));

        return options;
    }

    private static async Task ShowAgentDescription(IVideoGenerationGAgent agent)
    {
        AnsiConsole.MarkupLine("[bold cyan]📊 Agent Description[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .StartAsync("📖 Fetching agent description...", async ctx =>
            {
                var description = await agent.GetDescriptionAsync();
                
                ctx.Status = "✅ Description loaded!";
                await Task.Delay(500);
                
                var panel = new Panel(description)
                    .Header("[bold yellow]🤖 Video Generation Agent[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow);
                
                AnsiConsole.Write(panel);
            });
    }

    private static async Task RunPerformanceTest(IVideoGenerationGAgent agent)
    {
        AnsiConsole.MarkupLine("[bold cyan]🏃 Performance Test[/]");
        AnsiConsole.WriteLine();

        var testCount = AnsiConsole.Prompt(
            new TextPrompt<int>("[yellow]Number of concurrent requests:[/]")
                .DefaultValue(5)
                .Validate(n => n is > 0 and <= 20 ? ValidationResult.Success() : ValidationResult.Error("Must be between 1-20")));

        var testPrompts = new[]
        {
            "A serene ocean wave at sunset",
            "A bustling city street at night",
            "A peaceful mountain landscape",
            "A colorful garden with butterflies",
            "A futuristic spaceship in space"
        };

        var results = new List<(TimeSpan Duration, bool Success)>();

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Running performance test...[/]", maxValue: testCount);
                
                var tasks = Enumerable.Range(0, testCount)
                    .Select(async i =>
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        var prompt = testPrompts[i % testPrompts.Length];
                        
                        try
                        {
                            await agent.GenerateVideoFromTextAsync(prompt);
                            stopwatch.Stop();
                            
                            lock (results)
                            {
                                results.Add((stopwatch.Elapsed, true));
                                task.Increment(1);
                            }
                        }
                        catch
                        {
                            stopwatch.Stop();
                            lock (results)
                            {
                                results.Add((stopwatch.Elapsed, false));
                                task.Increment(1);
                            }
                        }
                    });

                await Task.WhenAll(tasks);
            });

        DisplayPerformanceResults(results);
    }

    private static async Task PollForVideoCompletion(IVideoGenerationGAgent agent, string taskId, string operationType)
    {
        var progress = AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(),
            });

        await progress.StartAsync(async ctx =>
        {
            var task = ctx.AddTask($"[green]{operationType} Generation[/]", maxValue: 100);
            
            VideoGenerationStatus? status = null;
            var maxPollingTime = TimeSpan.FromMinutes(10); // Max 10 minutes
            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < maxPollingTime)
            {
                status = await agent.GetVideoStatusAsync(taskId);
                
                task.Value = status.Progress;
                task.Description = $"[green]{operationType} Generation[/] - Status: {status.Status}";
                
                if (status.Status == "completed")
                {
                    task.Value = 100;
                    task.Description = $"[green]{operationType} Generation[/] - [bold green]Completed![/]";
                    break;
                }
                else if (status.Status == "failed")
                {
                    task.Description = $"[green]{operationType} Generation[/] - [bold red]Failed![/]";
                    break;
                }
                
                await Task.Delay(2000); // Poll every 2 seconds
            }
            
            if (DateTime.UtcNow - startTime >= maxPollingTime && status?.Status == "processing")
            {
                task.Description = $"[green]{operationType} Generation[/] - [bold yellow]Timeout![/]";
            }
        });

        // Get final status and display results
        var finalStatus = await agent.GetVideoStatusAsync(taskId);
        DisplayVideoStatus(finalStatus, operationType);
    }

    private static void DisplayVideoStatus(VideoGenerationStatus status, string operationType)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold green]✅ {operationType} Result:[/]");
        
        var statusColor = status.Status switch
        {
            "completed" => "green",
            "failed" => "red",
            "processing" => "yellow",
            _ => "white"
        };

        var panel = new Panel($@"
[bold]Task ID:[/] {status.TaskId}
[bold]Status:[/] [{statusColor}]{status.Status}[/]
[bold]Progress:[/] {status.Progress}%
[bold]Created At:[/] {status.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}
{(status.CompletedAt.HasValue ? $"[bold]Completed At:[/] {status.CompletedAt:yyyy-MM-dd HH:mm:ss UTC}" : "")}
{(!string.IsNullOrEmpty(status.VideoUrl) && status.Status == "completed" ? $"[bold]Video URL:[/] [link]{status.VideoUrl}[/]" : "")}
{(!string.IsNullOrEmpty(status.ErrorMessage) ? $"[bold red]Error:[/] {status.ErrorMessage}" : "")}
")
            .Header("[bold yellow]📄 Generation Status[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(status.Status == "completed" ? Color.Green : status.Status == "failed" ? Color.Red : Color.Yellow);
        
        AnsiConsole.Write(panel);
        
        if (status.Status == "completed" && !string.IsNullOrEmpty(status.VideoUrl))
        {
            AnsiConsole.MarkupLine($"[bold green]🎬 Video is ready! URL: [link]{status.VideoUrl}[/][/]");
        }
        else if (status.Status == "failed")
        {
            AnsiConsole.MarkupLine("[bold red]❌ Video generation failed[/]");
        }
        else if (status.Status == "processing")
        {
            AnsiConsole.MarkupLine("[bold yellow]⏳ Video generation is still in progress. You can check status later with the task ID.[/]");
        }
    }

    private static void DisplayVideoResult(string result, string operationType)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold green]✅ {operationType} Result:[/]");
        
        try
        {
            var json = JObject.Parse(result);
            var formattedJson = json.ToString(Formatting.Indented);
            
            var panel = new Panel(formattedJson)
                .Header("[bold yellow]📄 Generation Result[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green);
            
            AnsiConsole.Write(panel);
            
            // Extract key information
            var status = json["status"]?.ToString();
            var taskId = json["taskId"]?.ToString();
            var videoUrl = json["videoUrl"]?.ToString();
            
            if (status == "completed" && !string.IsNullOrEmpty(videoUrl))
            {
                AnsiConsole.MarkupLine($"[bold green]🎬 Video URL: [link]{videoUrl}[/][/]");
            }
            else if (status == "failed")
            {
                AnsiConsole.MarkupLine("[bold red]❌ Video generation failed[/]");
                var errorMessage = json["error"]?["message"]?.ToString();
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    AnsiConsole.MarkupLine($"[red]Error: {errorMessage.EscapeMarkup()}[/]");
                }
            }
        }
        catch (JsonException)
        {
            var panel = new Panel(result)
                .Header("[bold yellow]📄 Raw Result[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow);
            
            AnsiConsole.Write(panel);
        }
    }

    private static void DisplayPerformanceResults(List<(TimeSpan Duration, bool Success)> results)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]📊 Performance Test Results[/]");
        
        var successCount = results.Count(r => r.Success);
        var totalCount = results.Count;
        var successRate = (double)successCount / totalCount * 100;
        
        var avgDuration = results.Where(r => r.Success).Select(r => r.Duration).DefaultIfEmpty().Average(ts => ts.TotalMilliseconds);
        var maxDuration = results.Where(r => r.Success).Select(r => r.Duration).DefaultIfEmpty().Max();
        var minDuration = results.Where(r => r.Success).Select(r => r.Duration).DefaultIfEmpty().Min();

        var table = new Table()
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Total Requests", totalCount.ToString());
        table.AddRow("Successful", $"{successCount} ({successRate:F1}%)");
        table.AddRow("Failed", (totalCount - successCount).ToString());
        table.AddRow("Average Duration", $"{avgDuration:F0} ms");
        table.AddRow("Min Duration", $"{minDuration.TotalMilliseconds:F0} ms");
        table.AddRow("Max Duration", $"{maxDuration.TotalMilliseconds:F0} ms");

        AnsiConsole.Write(table);
    }
}


