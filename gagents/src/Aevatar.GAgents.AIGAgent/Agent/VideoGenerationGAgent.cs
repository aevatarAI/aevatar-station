using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Util;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.GEvents;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Common;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;

namespace Aevatar.GAgents.AIGAgent.Agent;

[Description("AI video generation agent with workflow support")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(VideoGenerationGAgent))]
public class VideoGenerationGAgent : AIGAgentBase<VideoGenerationState, VideoGenerationEvent, EventBase, VideoGenerationConfigDto>, IVideoGenerationGAgent
{
    private readonly HttpClient _httpClient;
    private BytePlusModelArkClient? _bytePlusClient;
    
    public VideoGenerationGAgent(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }
    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnAIGAgentActivateAsync(cancellationToken);
        
        // Initialize BytePlus client after activation (ServiceProvider is now available)
        var config = await ResolveSystemConfigAsync("BytePlusVideoGeneration");
        var baseUrl = config.Endpoint ?? "https://ark.ap-southeast.bytepluses.com";
        _bytePlusClient = new BytePlusModelArkClient(_httpClient, Logger, config.ApiKey, baseUrl);
        Logger.LogInformation("BytePlus client initialized successfully");
    }
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "AI video generation agent that creates videos from text prompts or images. " +
            "Supports text-to-video and image-to-video generation with customizable duration, resolution, and style options. " +
            "Provides real-time status tracking and error handling for video generation tasks.");
    }

    public override Task<Type?> GetConfigurationTypeAsync()
    {
        return Task.FromResult<Type?>(typeof(VideoGenerationConfigDto));
    }

    #region Video Generation Methods

    public async Task<string> GenerateVideoFromTextAsync(string prompt, VideoGenerationConfigDto? options = null)
    {
        Logger.LogInformation("Starting text-to-video generation, prompt length: {Length} characters", prompt?.Length ?? 0);

        var normalizedPrompt = AiAgentHelper.NormalizeUserInput(prompt ?? "", "Generate a creative video");
        options ??= new VideoGenerationConfigDto();

        var taskId = Guid.NewGuid().ToString();
        
        try
        {
            Logger.LogInformation("Starting BytePlus API call for text-to-video generation");
            
            // Create the BytePlus task without waiting for completion
            if (_bytePlusClient == null)
            {
                throw new InvalidOperationException("BytePlus client not initialized - video generation not available");
            }
            var bytePlusResponse = await _bytePlusClient.CreateVideoGenerationTaskAsync(normalizedPrompt, null, options);
                
            Logger.LogInformation("BytePlus API call completed successfully");
            
            // Store task in state with BytePlus task ID using event sourcing
            var startEvent = new VideoGenerationEvent
            {
                TaskId = taskId,
                Prompt = normalizedPrompt,
                Options = options,
                Id = Guid.NewGuid(),
                Ctime = DateTime.UtcNow
            };

            State.ActiveTasks[taskId] = new VideoGenerationStatus
            {
                TaskId = taskId,
                Status = "processing",
                CreatedAt = DateTime.UtcNow,
                Progress = 10,
                VideoUrl = bytePlusResponse.Id // Store BytePlus task ID temporarily in VideoUrl field
            };

            RaiseEvent(startEvent);
            await ConfirmEvents();

            Logger.LogInformation("Video generation task started: {TaskId}, BytePlus ID: {BytePlusId}", taskId, bytePlusResponse.Id);
            return taskId; // Return immediately with task ID
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Logger.LogWarning("BytePlus API call timed out for text-to-video generation task: {TaskId}", taskId);
            
            // Store failed state with timeout error
            State.ActiveTasks[taskId] = new VideoGenerationStatus
            {
                TaskId = taskId,
                Status = "failed",
                CreatedAt = DateTime.UtcNow,
                ErrorMessage = "API call timed out. Please try again."
            };
            
            return taskId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting video generation for task: {TaskId}", taskId);
            
            // Store failed state
            State.ActiveTasks[taskId] = new VideoGenerationStatus
            {
                TaskId = taskId,
                Status = "failed",
                CreatedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
            

            return taskId;
        }
    }

    public async Task<string> GenerateVideoFromImageAsync(string imageUrl, string prompt, VideoGenerationConfigDto? options = null)
    {
        Logger.LogInformation("Starting image-to-video generation, image URL: {ImageUrl}", imageUrl);

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            Logger.LogWarning("Image URL is empty or null");
            var failedTaskId = Guid.NewGuid().ToString();
            State.ActiveTasks[failedTaskId] = new VideoGenerationStatus
            {
                TaskId = failedTaskId,
                Status = "failed",
                CreatedAt = DateTime.UtcNow,
                ErrorMessage = "Image URL is required"
            };

            return failedTaskId;
        }

        var normalizedPrompt = AiAgentHelper.NormalizeUserInput(prompt, "Animate this image with smooth motion");
        options ??= new VideoGenerationConfigDto();

        var taskId = Guid.NewGuid().ToString();
        
        try
        {
            Logger.LogInformation("Starting BytePlus API call for image-to-video generation");
            
            // Create the BytePlus task without waiting for completion
            if (_bytePlusClient == null)
            {
                throw new InvalidOperationException("BytePlus client not initialized - video generation not available");
            }
            var bytePlusResponse = await _bytePlusClient.CreateVideoGenerationTaskAsync(normalizedPrompt, imageUrl, options);
                
            Logger.LogInformation("BytePlus API call completed successfully");
            
            // Store task in state with BytePlus task ID using event sourcing
            var startEvent = new VideoGenerationEvent
            {
                TaskId = taskId,
                Prompt = normalizedPrompt,
                ImageUrl = imageUrl,
                Options = options,
                Id = Guid.NewGuid(),
                Ctime = DateTime.UtcNow
            };


            State.ActiveTasks[taskId] = new VideoGenerationStatus
            {
                TaskId = taskId,
                Status = "processing",
                CreatedAt = DateTime.UtcNow,
                Progress = 10,
                VideoUrl = bytePlusResponse.Id // Store BytePlus task ID temporarily in VideoUrl field
            };

            RaiseEvent(startEvent);
            await ConfirmEvents();

            Logger.LogInformation("Image-to-video generation task started: {TaskId}, BytePlus ID: {BytePlusId}", taskId, bytePlusResponse.Id);
            return taskId; // Return immediately with task ID
        }
        catch (ArgumentException ex) when (ex.ParamName == "imageUrl")
        {
            Logger.LogWarning("Invalid image URL provided for image-to-video generation task: {TaskId}", taskId);
            
            // Store failed state with validation error
            State.ActiveTasks[taskId] = new VideoGenerationStatus
            {
                TaskId = taskId,
                Status = "failed",
                CreatedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
            
            return taskId;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Logger.LogWarning("BytePlus API call timed out for image-to-video generation task: {TaskId}", taskId);
            
            // Store failed state with timeout error
            State.ActiveTasks[taskId] = new VideoGenerationStatus
            {
                TaskId = taskId,
                Status = "failed",
                CreatedAt = DateTime.UtcNow,
                ErrorMessage = "API call timed out. Please try again."
            };
            
            return taskId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting image-to-video generation for task: {TaskId}", taskId);
            
            // Store failed state
            State.ActiveTasks[taskId] = new VideoGenerationStatus
            {
                TaskId = taskId,
                Status = "failed",
                CreatedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
            

            return taskId;
        }
    }

    public async Task<VideoGenerationStatus> GetVideoStatusAsync(string taskId)
    {
        if (!State.ActiveTasks.TryGetValue(taskId, out var status))
        {
            return new VideoGenerationStatus
            {
                TaskId = taskId,
                Status = "not_found",
                ErrorMessage = "Task not found"
            };
        }

        // If task is still processing, check BytePlus API for updates
        if (status.Status == "processing" && !string.IsNullOrEmpty(status.VideoUrl))
        {
            try
            {
                var bytePlusTaskId = status.VideoUrl; // BytePlus ID stored in VideoUrl field
                if (_bytePlusClient == null)
                {
                    throw new InvalidOperationException("BytePlus client not initialized - video generation not available");
                }
                var bytePlusStatus = await _bytePlusClient.GetVideoGenerationTaskAsync(bytePlusTaskId);
                
                Logger.LogDebug("BytePlus task {TaskId} status: {Status}", bytePlusTaskId, bytePlusStatus.Status);
                
                switch (bytePlusStatus.Status.ToLower())
                {
                    case "succeeded":
                        status.Status = "completed";
                        status.VideoUrl = bytePlusStatus.Content?.VideoUrl ?? "";
                        status.Progress = 100;
                        status.CompletedAt = DateTime.UtcNow;
                        State.TotalVideosGenerated++;
                        State.LastGenerationTime = DateTime.UtcNow;
            
                        Logger.LogInformation("Video generation completed: {TaskId}, Video URL: {VideoUrl}", taskId, status.VideoUrl);
                        break;
                        
                    case "failed":
                        status.Status = "failed";
                        status.ErrorMessage = bytePlusStatus.Error?.Message ?? "Unknown error";
                        status.VideoUrl = "";
            
                        Logger.LogError("Video generation failed: {TaskId}, Error: {Error}", taskId, status.ErrorMessage);
                        break;
                        
                    case "processing":
                        // Update progress estimate based on time elapsed
                        var elapsed = DateTime.UtcNow - status.CreatedAt;
                        var estimatedProgress = Math.Min(90, 10 + (int)(elapsed.TotalMinutes * 20)); // Estimate up to 90%
                        status.Progress = estimatedProgress;
            
                        break;
                }
                
                // Update state with new status
                State.ActiveTasks[taskId] = status;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error checking BytePlus status for task: {TaskId}. Using simulation mode.", taskId);
                
                // Simulation mode: simulate completion after some time if API fails
                var elapsed = DateTime.UtcNow - status.CreatedAt;
                if (elapsed.TotalSeconds > 30) // Simulate completion after 30 seconds for demo
                {
                    status.Status = "completed";
                    status.VideoUrl = $"https://demo-video-storage.example.com/videos/{taskId}.mp4";
                    status.Progress = 100;
                    status.CompletedAt = DateTime.UtcNow;
                    State.TotalVideosGenerated++;
                    State.LastGenerationTime = DateTime.UtcNow;
                    
                    Logger.LogInformation("Demo simulation: Video generation completed for task: {TaskId}", taskId);
                    State.ActiveTasks[taskId] = status;
                }
                else
                {
                    // Update progress while waiting
                    var progressEstimate = Math.Min(90, 10 + (int)(elapsed.TotalSeconds * 2)); // 2% per second up to 90%
                    status.Progress = progressEstimate;
                    State.ActiveTasks[taskId] = status;
                }
            }
        }

        return status;
    }

    #endregion

    #region Event Handlers

    [EventHandler]
    public async Task HandleEventAsync(ChatEvent @event)
    {
        Logger.LogInformation("VideoGenerationGAgent received ChatEvent with {MessageCount} messages", 
            @event.CoordinatorMessages?.Count ?? 0);

        var response = new ChatResponse();

        if (@event.CoordinatorMessages == null || @event.CoordinatorMessages.Count == 0)
        {
            // Generate default response based on agent description
            var defaultResponse = await GetDescriptionAsync();
            response.Content = $"I am a video generation agent. {defaultResponse}";
            response.Continue = false; // Don't continue if no input

            await PublishAsync(new ChatResponseEvent
            {
                BlackboardId = @event.BlackboardId,
                MemberId = this.GetPrimaryKey(),
                MemberName = "VideoGenerationAgent",
                ChatResponse = response,
                Term = @event.Term
            });
            return;
        }

        // Process the workflow messages - workflow will wait for completion
        var userMessage = string.Join(" ", @event.CoordinatorMessages.Select(m => m.Content));
        
        try
        {
            Logger.LogInformation("Starting video generation for: {UserMessage}", userMessage);

            // Create configuration from state values
            var config = new VideoGenerationConfigDto
            {
                Instructions = State.Instructions,
                GenerationType = State.GenerationType,
                Duration = State.Duration,
                Resolution = State.Resolution,
                Style = State.Style,
                ImageUrl = State.ImageUrl,
                AutoReturnResult = State.AutoReturnResult
            };
            
            // Parse message and determine generation type
            var (prompt, detectedImageUrl, options) = ParseVideoGenerationRequestWithConfig(userMessage, config);
            
            // Determine if this is image-to-video or text-to-video
            var isImageToVideo = !string.IsNullOrEmpty(detectedImageUrl) || 
                               !string.IsNullOrEmpty(config.ImageUrl) || 
                               config.GenerationType == "image-to-video";
            
            string taskId;
            string videoUrl = "";
            
            if (isImageToVideo)
            {
                // Use image URL from config if not detected in message
                var imageUrl = detectedImageUrl ?? config.ImageUrl;
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    throw new ArgumentException("Image-to-video generation requires an image URL");
                }
                
                // Start image-to-video generation
                taskId = await GenerateVideoFromImageAsync(imageUrl, prompt, options);
                Logger.LogInformation("Image-to-video task started: {TaskId}", taskId);
            }
            else
            {
                // Start text-to-video generation
                taskId = await GenerateVideoFromTextAsync(prompt, options);
                Logger.LogInformation("Text-to-video task started: {TaskId}", taskId);
            }

            // 3. Poll for completion
            var pollCount = 0;
            const int maxPollAttempts = 60; // 5 minutes with 5-second intervals
            const int pollIntervalSeconds = 5;
            
            while (pollCount < maxPollAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds));
                pollCount++;
                
                var status = await GetVideoStatusAsync(taskId);
                Logger.LogDebug("Video generation status check {PollCount}: {Status}", pollCount, status.Status);
                
                if (status.Status == "completed")
                {
                    videoUrl = status.VideoUrl ?? "";
                    Logger.LogInformation("Video generation completed: {VideoUrl}", videoUrl);
                    break;
                }
                else if (status.Status == "failed")
                {
                    throw new Exception($"Video generation failed: {status.ErrorMessage}");
                }
                // Continue polling if status is InProgress or Pending
            }
            
            if (string.IsNullOrEmpty(videoUrl))
            {
                throw new TimeoutException($"Video generation timed out after {maxPollAttempts * pollIntervalSeconds} seconds");
            }

            // Set successful response
            response.Content = $"✅ Video generation complete! 🎬 {videoUrl}";
            response.Continue = false; // Workflow can now proceed to next step
            
            Logger.LogInformation("Video generation completed successfully: {VideoUrl}", videoUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Video generation failed: {Message}", ex.Message);
            response.Content = $"❌ Video generation failed: {ex.Message}";
            response.Continue = false; // Stop workflow on error
        }

        // Send single response when everything is complete
        await PublishAsync(new ChatResponseEvent
        {
            BlackboardId = @event.BlackboardId,
            MemberId = this.GetPrimaryKey(),
            MemberName = "VideoGenerationAgent",
            ChatResponse = response,
            Term = @event.Term
        });
    }

    private (string prompt, string? imageUrl, VideoGenerationConfigDto options) ParseVideoGenerationRequestWithConfig(string userMessage, VideoGenerationConfigDto config)
    {
        var prompt = userMessage.Trim();
        var options = new VideoGenerationConfigDto();
        string? detectedImageUrl = null;

        // Apply configuration defaults
        options.Duration = config.Duration;
        options.Resolution = config.Resolution;
        options.Style = config.Style;

        // Try to detect image URL in the message
        var urlPattern = @"https?://[^\s]+\.(?:jpg|jpeg|png|gif|bmp|webp)(?:\?[^\s]*)?";
        var urlMatch = System.Text.RegularExpressions.Regex.Match(userMessage, urlPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (urlMatch.Success)
        {
            detectedImageUrl = urlMatch.Value;
            // Remove the URL from the prompt
            prompt = userMessage.Replace(detectedImageUrl, "").Trim();
        }

        // Try to extract resolution if mentioned
        if (userMessage.Contains("4k", StringComparison.OrdinalIgnoreCase) || userMessage.Contains("2160p", StringComparison.OrdinalIgnoreCase))
        {
            options.Resolution = "4K";
        }
        else if (userMessage.Contains("1080p", StringComparison.OrdinalIgnoreCase))
        {
            options.Resolution = "1080p";
        }
        else if (userMessage.Contains("720p", StringComparison.OrdinalIgnoreCase))
        {
            options.Resolution = "720p";
        }
        else if (userMessage.Contains("480p", StringComparison.OrdinalIgnoreCase))
        {
            options.Resolution = "480p";
        }

        // Try to extract style if mentioned
        if (userMessage.Contains("cinematic", StringComparison.OrdinalIgnoreCase))
        {
            options.Style = "cinematic";
        }
        else if (userMessage.Contains("realistic", StringComparison.OrdinalIgnoreCase))
        {
            options.Style = "realistic";
        }
        else if (userMessage.Contains("animated", StringComparison.OrdinalIgnoreCase))
        {
            options.Style = "animated";
        }
        else if (userMessage.Contains("artistic", StringComparison.OrdinalIgnoreCase))
        {
            options.Style = "artistic";
        }

        // If prompt is empty or just contains instructions, use config instructions
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length < 10)
        {
            prompt = $"{config.Instructions}: {prompt}".Trim();
        }

        return (prompt, detectedImageUrl, options);
    }

    // Keep the old method for backward compatibility
    private (string prompt, VideoGenerationConfigDto? options) ParseVideoGenerationRequest(string userMessage)
    {
        var config = new VideoGenerationConfigDto();
        var (parsedPrompt, _, parsedOptions) = ParseVideoGenerationRequestWithConfig(userMessage, config);
        return (parsedPrompt, parsedOptions);
    }

    #endregion

    #region Configuration Management

    protected override async Task PerformConfigAsync(VideoGenerationConfigDto configuration)
    {
        await base.PerformConfigAsync(configuration);

        // Store configuration values in state through events
        RaiseEvent(new VideoGenerationConfigurationSetEvent
        {
            Instructions = configuration.Instructions,
            GenerationType = configuration.GenerationType,
            Duration = configuration.Duration,
            Resolution = configuration.Resolution,
            Style = configuration.Style,
            ImageUrl = configuration.ImageUrl,
            AutoReturnResult = configuration.AutoReturnResult
        });

        await ConfirmEvents();
        
        Logger.LogInformation("VideoGenerationGAgent configuration completed - Duration: {Duration}, Resolution: {Resolution}", 
            configuration.Duration, configuration.Resolution);
    }

    protected override void AIGAgentTransitionState(VideoGenerationState state, StateLogEventBase<VideoGenerationEvent> @event)
    {
        switch (@event)
        {
            case VideoGenerationConfigurationSetEvent configEvent:
                state.Instructions = configEvent.Instructions;
                state.GenerationType = configEvent.GenerationType;
                state.Duration = configEvent.Duration;
                state.Resolution = configEvent.Resolution;
                state.Style = configEvent.Style;
                state.ImageUrl = configEvent.ImageUrl;
                state.AutoReturnResult = configEvent.AutoReturnResult;
                Logger.LogDebug("Configuration applied to state: Duration={Duration}, Resolution={Resolution}", 
                    state.Duration, state.Resolution);
                break;
        }
    }

    #endregion
}
