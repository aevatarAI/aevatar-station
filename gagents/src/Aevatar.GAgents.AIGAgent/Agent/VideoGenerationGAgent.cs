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
    private readonly IBytePlusModelArkClient _bytePlusClient;
    
    public VideoGenerationGAgent(IBytePlusModelArkClient bytePlusClient)
    {
        _bytePlusClient = bytePlusClient ?? throw new ArgumentNullException(nameof(bytePlusClient));
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
            
            // Get configuration for this request
            var config = await ResolveSystemConfigAsync("BytePlusVideoGeneration");
            var baseUrl = config.Endpoint ?? "https://ark.ap-southeast.bytepluses.com";
            
            // Create the BytePlus task without waiting for completion
            var bytePlusResponse = await _bytePlusClient.CreateVideoGenerationTaskAsync(normalizedPrompt, config.ApiKey, baseUrl, null, options);
                
            Logger.LogInformation("BytePlus API call completed successfully");
            
            // Store task in state with BytePlus task ID using event sourcing
            // Single optimized event that combines start and task creation - eliminates redundancy
            RaiseEvent(new VideoGenerationStartedEvent
            {
                TaskId = taskId,
                Prompt = normalizedPrompt,
                ImageUrl = null,
                Options = options,
                BytePlusTaskId = bytePlusResponse.Id,
                StartedAt = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                Ctime = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("Video generation task started: {TaskId}, BytePlus ID: {BytePlusId}", taskId, bytePlusResponse.Id);
            return taskId; // Return immediately with task ID
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Logger.LogWarning("BytePlus API call timed out for text-to-video generation task: {TaskId}", taskId);
            
            // Store failed state with timeout error using event sourcing
            RaiseEvent(new VideoGenerationFailedEvent 
            { 
                TaskId = taskId, 
                ErrorMessage = "API call timed out. Please try again.",
                FailedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return taskId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting video generation for task: {TaskId}", taskId);
            
            // Store failed state using event sourcing
            RaiseEvent(new VideoGenerationFailedEvent 
            { 
                TaskId = taskId, 
                ErrorMessage = ex.Message,
                FailedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

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
            
            // Store failed state using event sourcing
            RaiseEvent(new VideoGenerationFailedEvent 
            { 
                TaskId = failedTaskId, 
                ErrorMessage = "Image URL is required",
                FailedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            return failedTaskId;
        }

        var normalizedPrompt = AiAgentHelper.NormalizeUserInput(prompt, "Animate this image with smooth motion");
        options ??= new VideoGenerationConfigDto();

        var taskId = Guid.NewGuid().ToString();
        
        try
        {
            Logger.LogInformation("Starting BytePlus API call for image-to-video generation");
            
            // Get configuration for this request
            var config = await ResolveSystemConfigAsync("BytePlusVideoGeneration");
            var baseUrl = config.Endpoint ?? "https://ark.ap-southeast.bytepluses.com";
            
            // Create the BytePlus task without waiting for completion
            var bytePlusResponse = await _bytePlusClient.CreateVideoGenerationTaskAsync(normalizedPrompt, config.ApiKey, baseUrl, imageUrl, options);
                
            Logger.LogInformation("BytePlus API call completed successfully");
            
            // Single optimized event that combines start and task creation - eliminates redundancy
            RaiseEvent(new VideoGenerationStartedEvent
            {
                TaskId = taskId,
                Prompt = normalizedPrompt,
                ImageUrl = imageUrl,
                Options = options,
                BytePlusTaskId = bytePlusResponse.Id,
                StartedAt = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                Ctime = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("Image-to-video generation task started: {TaskId}, BytePlus ID: {BytePlusId}", taskId, bytePlusResponse.Id);
            return taskId; // Return immediately with task ID
        }
        catch (ArgumentException ex) when (ex.ParamName == "imageUrl")
        {
            Logger.LogWarning("Invalid image URL provided for image-to-video generation task: {TaskId}", taskId);
            
            // Store failed state with validation error using event sourcing
            RaiseEvent(new VideoGenerationFailedEvent 
            { 
                TaskId = taskId, 
                ErrorMessage = ex.Message,
                FailedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return taskId;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Logger.LogWarning("BytePlus API call timed out for image-to-video generation task: {TaskId}", taskId);
            
            // Store failed state with timeout error using event sourcing
            RaiseEvent(new VideoGenerationFailedEvent 
            { 
                TaskId = taskId, 
                ErrorMessage = "API call timed out. Please try again.",
                FailedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return taskId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting image-to-video generation for task: {TaskId}", taskId);
            
            // Store failed state using event sourcing
            RaiseEvent(new VideoGenerationFailedEvent 
            { 
                TaskId = taskId, 
                ErrorMessage = ex.Message,
                FailedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

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
                
                // Get configuration for this request
                var config = await ResolveSystemConfigAsync("BytePlusVideoGeneration");
                var baseUrl = config.Endpoint ?? "https://ark.ap-southeast.bytepluses.com";
                
                var bytePlusStatus = await _bytePlusClient.GetVideoGenerationTaskAsync(bytePlusTaskId, config.ApiKey, baseUrl);
                
                Logger.LogDebug("BytePlus task {TaskId} status: {Status}", bytePlusTaskId, bytePlusStatus.Status);
                
                switch (bytePlusStatus.Status.ToLower())
                {
                    case "succeeded":
                        status.Status = "completed";
                        status.VideoUrl = bytePlusStatus.Content?.VideoUrl ?? "";
                        status.Progress = 100;
                        status.CompletedAt = DateTime.UtcNow;
            
                        Logger.LogInformation("Video generation completed: {TaskId}, Video URL: {VideoUrl}", taskId, status.VideoUrl);
                        
                        // Use VideoGenerationCompletedEvent for successful completion
                        RaiseEvent(new VideoGenerationCompletedEvent 
                        { 
                            TaskId = taskId, 
                            VideoUrl = status.VideoUrl,
                            CompletedAt = status.CompletedAt ?? DateTime.UtcNow
                        });
                        break;
                        
                    case "failed":
                        status.Status = "failed";
                        status.ErrorMessage = bytePlusStatus.Error?.Message ?? "Unknown error";
                        status.VideoUrl = "";
            
                        Logger.LogError("Video generation failed: {TaskId}, Error: {Error}", taskId, status.ErrorMessage);
                        
                        // Use VideoGenerationFailedEvent for failed tasks
                        RaiseEvent(new VideoGenerationFailedEvent 
                        { 
                            TaskId = taskId, 
                            ErrorMessage = status.ErrorMessage,
                            FailedAt = DateTime.UtcNow
                        });
                        break;
                        
                    case "processing":
                        // Update progress estimate based on time elapsed
                        var elapsed = DateTime.UtcNow - status.CreatedAt;
                        var estimatedProgress = Math.Min(90, 10 + (int)(elapsed.TotalMinutes * 20)); // Estimate up to 90%
                        status.Progress = estimatedProgress;
                        
                        // Use VideoGenerationProgressUpdatedEvent for progress updates
                        RaiseEvent(new VideoGenerationProgressUpdatedEvent 
                        { 
                            TaskId = taskId, 
                            Progress = status.Progress,
                            Status = status.Status
                        });
                        break;
                }
                
                // Confirm all events after processing
                await ConfirmEvents();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error checking BytePlus status for task: {TaskId}. Marking task as failed.", taskId);
                
                // When exception occurs, properly fail the task with exception details
                var errorMessage = $"BytePlus API error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
                
                // Use VideoGenerationFailedEvent to record the failure with full exception details
                RaiseEvent(new VideoGenerationFailedEvent 
                { 
                    TaskId = taskId, 
                    ErrorMessage = errorMessage,
                    FailedAt = DateTime.UtcNow
                });
                
                await ConfirmEvents();
                
                // Return the failed status immediately - don't continue processing
                if (State.ActiveTasks.TryGetValue(taskId, out var failedStatus))
                {
                    return failedStatus;
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
                
            case VideoGenerationStartedEvent startedEvent:
                // Create new task with all details in one event - eliminates redundancy
                state.ActiveTasks[startedEvent.TaskId] = new VideoGenerationStatus
                {
                    TaskId = startedEvent.TaskId,
                    Status = "processing",
                    CreatedAt = startedEvent.StartedAt,
                    Progress = 10,
                    VideoUrl = startedEvent.BytePlusTaskId // Store BytePlus task ID temporarily
                };
                Logger.LogDebug("Video generation started: {TaskId} with prompt: {Prompt}", 
                    startedEvent.TaskId, startedEvent.Prompt);
                break;
                
            case VideoGenerationProgressUpdatedEvent progressEvent:
                if (state.ActiveTasks.TryGetValue(progressEvent.TaskId, out var existingTask))
                {
                    existingTask.Status = progressEvent.Status;
                    existingTask.Progress = progressEvent.Progress;
                    state.ActiveTasks[progressEvent.TaskId] = existingTask;
                    Logger.LogDebug("Video generation progress: {TaskId} -> {Progress}%", 
                        progressEvent.TaskId, progressEvent.Progress);
                }
                break;
                
            case VideoGenerationCompletedEvent completedEvent:
                if (state.ActiveTasks.TryGetValue(completedEvent.TaskId, out var completedTask))
                {
                    completedTask.Status = "completed";
                    completedTask.VideoUrl = completedEvent.VideoUrl;
                    completedTask.Progress = 100;
                    completedTask.CompletedAt = completedEvent.CompletedAt;
                    state.ActiveTasks[completedEvent.TaskId] = completedTask;
                }
                state.TotalVideosGenerated++;
                state.LastGenerationTime = completedEvent.CompletedAt;
                state.LastGeneratedVideoUrl = completedEvent.VideoUrl;
                Logger.LogInformation("Video generation completed: {TaskId}, Total: {Total}", 
                    completedEvent.TaskId, state.TotalVideosGenerated);
                break;
                
            case VideoGenerationFailedEvent failedEvent:
                if (state.ActiveTasks.TryGetValue(failedEvent.TaskId, out var failedTask))
                {
                    failedTask.Status = "failed";
                    failedTask.ErrorMessage = failedEvent.ErrorMessage;
                    state.ActiveTasks[failedEvent.TaskId] = failedTask;
                }
                else
                {
                    // Create failed task if it doesn't exist
                    state.ActiveTasks[failedEvent.TaskId] = new VideoGenerationStatus
                    {
                        TaskId = failedEvent.TaskId,
                        Status = "failed",
                        CreatedAt = failedEvent.FailedAt,
                        ErrorMessage = failedEvent.ErrorMessage
                    };
                }
                Logger.LogWarning("Video task failed: {TaskId}, Error: {Error}", 
                    failedEvent.TaskId, failedEvent.ErrorMessage);
                break;
        }
    }

    #endregion
}
