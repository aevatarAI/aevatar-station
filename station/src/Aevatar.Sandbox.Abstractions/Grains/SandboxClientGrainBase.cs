using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aevatar.Sandbox.Abstractions.Grains;

public abstract class SandboxClientGrainBase : Grain, ISandboxExecutionClientGrain
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected Guid SandboxExecutionId => this.GetPrimaryKey();

    protected SandboxClientGrainBase(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient;
        Logger = logger;
    }

    public virtual async Task<SandboxExecutionResult> ExecuteAsync(SandboxExecutionClientParams @params)
    {
        try
        {
            // Create execution request
            var request = new SandboxExecutionRequest
            {
                SandboxExecutionId = SandboxExecutionId.ToString(),
                LanguageId = @params.LanguageId,
                Code = @params.Code,
                TimeoutSeconds = @params.TimeoutSeconds,
                TenantId = @params.TenantId,
                ChatId = @params.ChatId
            };

            // Start execution
            var handle = await StartExecutionAsync(request);
            Logger.LogInformation("Started sandbox execution {SandboxExecutionId} with workload {WorkloadName}",
                SandboxExecutionId, handle.WorkloadName);

            // Poll for result with exponential backoff
            var result = await PollForResultAsync(request.SandboxExecutionId, @params.TimeoutSeconds);

            // Deactivate after completion
            DeactivateOnIdle();

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to execute sandbox request {SandboxExecutionId}", SandboxExecutionId);
            throw;
        }
    }

    protected virtual async Task<SandboxExecutionHandle> StartExecutionAsync(SandboxExecutionRequest request)
    {
        var response = await HttpClient.PostAsJsonAsync("api/sandbox/execute", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SandboxExecutionHandle>();
    }

    protected virtual async Task<SandboxExecutionResult> PollForResultAsync(string sandboxExecutionId, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds + 5); // Add buffer for cleanup
        var delay = TimeSpan.FromSeconds(1);

        while (DateTime.UtcNow < deadline)
        {
            var response = await HttpClient.GetAsync($"api/sandbox/result/{sandboxExecutionId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 1.5, 5)); // Cap at 5s
                continue;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SandboxExecutionResult>();
            return result ?? throw new InvalidOperationException("Received null result from API");
        }

        throw new TimeoutException($"Sandbox execution {sandboxExecutionId} timed out after {timeoutSeconds}s");
    }
}