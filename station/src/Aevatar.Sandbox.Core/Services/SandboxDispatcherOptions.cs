using System;

namespace Aevatar.Sandbox.Core.Services;

public class SandboxDispatcherOptions
{
    public int MaxConcurrentExecutionsPerLanguage { get; set; } = 10;
    public TimeSpan QueueTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int ExecutionTimeout { get; set; } = 30;
}