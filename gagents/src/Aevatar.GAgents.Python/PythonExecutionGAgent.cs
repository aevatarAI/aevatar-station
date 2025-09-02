using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Aevatar.Core;

namespace Aevatar.GAgents.Python;

/// <summary>
/// State for Python execution agent
/// </summary>
[GenerateSerializer]
public class PythonExecutionState : StateBase
{
    [Id(0)] public bool Initialized { get; set; }
    [Id(1)] public string PythonEnvironmentPath { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, PythonEnvironmentConfig> Environments { get; set; } = new();
    [Id(3)] public string DefaultEnvironmentName { get; set; } = "default";
    [Id(4)] public Dictionary<string, DateTime> EnvironmentLastUsed { get; set; } = new();
    [Id(5)] public Dictionary<string, List<string>> EnvironmentPackages { get; set; } = new();
    [Id(6)] public int MaxConcurrentExecutions { get; set; } = 3;
    [Id(7)] public List<string> ActiveExecutions { get; set; } = new();
    [Id(8)] public Dictionary<string, string> EnvironmentSecurity { get; set; } = new();
    [Id(9)] public List<string> RequiredPackages { get; set; } = new();
    [Id(10)] public List<ScriptExecutionResult> ExecutionHistory { get; set; } = new();
}

/// <summary>
/// State log events for Python execution
/// </summary>
[GenerateSerializer]
public class PythonExecutionStateLogEvent : StateLogEventBase<PythonExecutionStateLogEvent>;

[GenerateSerializer]
public class ExecutionInitializedLogEvent : PythonExecutionStateLogEvent
{
    [Id(0)] public string PythonPath { get; set; } = string.Empty;
    [Id(1)] public List<string> InstalledPackages { get; set; } = new();
}

[GenerateSerializer]
public class ScriptExecutedLogEvent : PythonExecutionStateLogEvent
{
    [Id(0)] public ScriptExecutionResult Result { get; set; } = new();
}

[GenerateSerializer]
public class EnvironmentCreatedLogEvent : PythonExecutionStateLogEvent
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(1)] public PythonEnvironmentConfig Config { get; set; } = new();
}

[GenerateSerializer]
public class EnvironmentDeletedLogEvent : PythonExecutionStateLogEvent
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
}

[GenerateSerializer]
public class DependenciesInstalledLogEvent : PythonExecutionStateLogEvent
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(1)] public List<string> Dependencies { get; set; } = new();
    [Id(2)] public bool Success { get; set; }
}

/// <summary>
/// Interface for Python execution agent focused on environment management and script execution
/// </summary>
public interface IPythonExecutionGAgent : IStateGAgent<PythonExecutionState>
{
    // Core execution methods
    Task<bool> InitializeAsync(string pythonPath = "python3");
    Task<ScriptExecutionResult> ExecutePythonScriptAsync(string script, PythonEnvironmentConfig? config = null);
    Task<ScriptExecutionResult> ExecutePythonScriptWithTimeoutAsync(string script, int timeoutSeconds, PythonEnvironmentConfig? config = null);
    Task<bool> ValidateScriptSecurityAsync(string script);
    Task<List<string>> ExtractScriptDependenciesAsync(string script);
    
    // Environment management
    Task<bool> CreateEnvironmentAsync(string environmentName, PythonEnvironmentConfig config);
    Task<bool> DeleteEnvironmentAsync(string environmentName);
    Task<List<string>> ListEnvironmentsAsync();
    Task<PythonEnvironmentConfig?> GetEnvironmentConfigAsync(string environmentName);
    Task<bool> SetDefaultEnvironmentAsync(string environmentName);
    
    // Dependency management
    Task<bool> InstallDependenciesAsync(string environmentName, List<string> dependencies);
    Task<bool> InstallRequiredPackagesAsync(List<string> packages);
    Task<List<string>> GetInstalledPackagesAsync(string environmentName);
    Task<bool> UninstallPackageAsync(string environmentName, string packageName);
    
    // Security and sandboxing
    Task<bool> EnableSandboxForEnvironmentAsync(string environmentName);
    Task<bool> SetExecutionLimitsAsync(string environmentName, int maxTimeSeconds, long maxMemoryMB);
    Task<bool> RestrictNetworkAccessAsync(string environmentName, bool allowAccess);
    Task<bool> SetAllowedModulesAsync(string environmentName, List<string> allowedModules);
    
    // Python command detection and management
    Task<string> DetectBestPythonCommandAsync();
    Task<List<string>> GetAvailablePythonCommandsAsync();
    Task<bool> ValidatePythonCommandAsync(string pythonCommand);
    Task<string> GetPythonVersionAsync(string pythonCommand);
    Task<bool> SetPythonCommandForEnvironmentAsync(string environmentName, string pythonCommand);
    
    // Execution history and monitoring
    Task<List<ScriptExecutionResult>> GetExecutionHistoryAsync();
    Task<Dictionary<string, int>> GetExecutionStatsAsync();
}

/// <summary>
/// Python execution agent that manages Python environments and executes scripts
/// </summary>
[GAgent("python.execution", "aevatar")]
public class PythonExecutionGAgent : GAgentBase<PythonExecutionState, PythonExecutionStateLogEvent>,
    IPythonExecutionGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult(
            "Manages Python environments and executes Python scripts with security controls and resource monitoring");

    public async Task<bool> InitializeAsync(string pythonPath = "python3")
    {
        try
        {
            Logger.LogInformation("Initializing PythonExecutionGAgent with Python path: {PythonPath}", pythonPath);

            // Verify Python installation
            var pythonVersion = await CheckPythonInstallationAsync(pythonPath);
            if (string.IsNullOrEmpty(pythonVersion))
            {
                Logger.LogError("Python installation not found or invalid at path: {PythonPath}", pythonPath);
                return false;
            }

            // Install basic required packages
            var requiredPackages = new List<string>
            {
                "numpy", "sympy", "scipy", "matplotlib", "pytest", "hypothesis"
            };

            var installed = await InstallRequiredPackagesAsync(requiredPackages);
            if (!installed)
            {
                Logger.LogWarning("Some required packages could not be installed, but continuing...");
            }

            RaiseEvent(new ExecutionInitializedLogEvent
            {
                PythonPath = pythonPath,
                InstalledPackages = requiredPackages
            });
            await ConfirmEvents();

            Logger.LogInformation("PythonExecutionGAgent initialized successfully with Python {Version}",
                pythonVersion);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize PythonExecutionGAgent");
            return false;
        }
    }

    /// <summary>
    /// Execute Python script in an isolated environment with comprehensive security and monitoring
    /// </summary>
    public async Task<ScriptExecutionResult> ExecutePythonScriptAsync(string script, PythonEnvironmentConfig? config = null)
    {
        config ??= GetDefaultEnvironmentConfig();
        
        var result = new ScriptExecutionResult
        {
            StartTime = DateTime.UtcNow,
            ScriptHash = ComputeScriptHash(script)
        };

        try
        {
            // Validate script security first
            if (config.IsSandboxed && !await ValidateScriptSecurityAsync(script))
            {
                result.Success = false;
                result.ErrorOutput = "Script failed security validation";
                result.EndTime = DateTime.UtcNow;
                Logger.LogError("Validate script security error.");
                
                // Publish security validation failed event
                await PublishAsync(new SecurityValidationFailedEvent
                {
                    ScriptHash = result.ScriptHash,
                    Reason = "Script contains potentially dangerous operations",
                    ViolatedRules = new List<string> { "Security validation failed" }
                });
                
                return result;
            }

            // Check execution limits
            if (State.ActiveExecutions.Count >= State.MaxConcurrentExecutions)
            {
                result.Success = false;
                result.ErrorOutput = "Maximum concurrent executions reached";
                result.EndTime = DateTime.UtcNow;
                Logger.LogError("Check execution limits error.");
                
                // Publish execution limit reached event
                await PublishAsync(new ExecutionLimitReachedEvent
                {
                    CurrentExecutions = State.ActiveExecutions.Count,
                    MaxExecutions = State.MaxConcurrentExecutions
                });
                
                return result;
            }

            var executionId = Guid.NewGuid().ToString();
            State.ActiveExecutions.Add(executionId);

            try
            {
                Logger.LogInformation("Start to execute with timeout.");
                // Execute with timeout
                result = await ExecutePythonScriptWithTimeoutAsync(script, config.MaxExecutionTimeSeconds, config);
                
                // Log execution result
                RaiseEvent(new ScriptExecutedLogEvent { Result = result });
                await ConfirmEvents();
                
                // Publish execution completed event
                await PublishAsync(new ScriptExecutionCompletedEvent
                {
                    Result = result,
                    EnvironmentName = config.EnvironmentName,
                    ExecutionSessionId = executionId
                });
                
                // Store in history
                State.ExecutionHistory.Add(result);
                if (State.ExecutionHistory.Count > 100) // Keep only last 100 executions
                {
                    State.ExecutionHistory.RemoveAt(0);
                }
            }
            finally
            {
                State.ActiveExecutions.Remove(executionId);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.ErrorOutput = ex.Message;
            result.EndTime = DateTime.UtcNow;
            Logger.LogError(ex, "Error executing Python script");
        }

        return result;
    }

    /// <summary>
    /// Execute Python script with timeout and resource monitoring
    /// </summary>
    public async Task<ScriptExecutionResult> ExecutePythonScriptWithTimeoutAsync(string script, int timeoutSeconds, PythonEnvironmentConfig? config = null)
    {
        config ??= GetDefaultEnvironmentConfig();
        
        var result = new ScriptExecutionResult
        {
            StartTime = DateTime.UtcNow,
            ScriptHash = ComputeScriptHash(script)
        };

        var stopwatch = Stopwatch.StartNew();
        string? tempFile = null;
        Process? process = null;

        try
        {
            // Create secure temporary directory for execution
            var tempDir = CreateSecureTempDirectory(config);
            tempFile = Path.Combine(tempDir, $"script_{result.ScriptHash}_{DateTime.UtcNow:yyyyMMddHHmmss}.py");
            Logger.LogInformation("Created secure temporary directory for execution, tempFile: {tempFile}", tempFile);
            
            // Prepare script with security wrapper if sandboxed
            var finalScript = config.IsSandboxed ? WrapScriptWithSandbox(script, config) : script;
            await File.WriteAllTextAsync(tempFile, finalScript);
            Logger.LogInformation("Prepared script with security wrapper if sandboxed, finalScript: {finalScript}", finalScript);

            // Setup process with environment isolation
            var processInfo = await CreateSecureProcessInfoAsync(tempFile, config);
            process = Process.Start(processInfo);
            Logger.LogInformation("Setup process with environment isolation");

            if (process == null)
            {
                result.Success = false;
                result.ErrorOutput = "Failed to start Python process";
                result.EndTime = DateTime.UtcNow;
                Logger.LogError("Failed to start Python process");
                return result;
            }

            // Monitor execution with timeout
            var completedTask = process.WaitForExitAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completedFirst = await Task.WhenAny(completedTask, timeoutTask);
            Logger.LogInformation("Monitoring execution with timeout");

            if (completedFirst == timeoutTask)
            {
                // Timeout occurred
                result.TimedOut = true;
                result.Success = false;
                result.ErrorOutput = $"Script execution timed out after {timeoutSeconds} seconds";

                Logger.LogError($"Script execution timed out after {timeoutSeconds} seconds");

                try
                {
                    process.Kill(true); // Kill process tree
                }
                catch (Exception killEx)
                {
                    Logger.LogWarning(killEx, "Failed to kill timed out process");
                }
            }
            else
            {
                // Process completed normally
                result.ExitCode = process.ExitCode;
                result.Success = process.ExitCode == 0;
                result.StandardOutput = await process.StandardOutput.ReadToEndAsync();
                result.ErrorOutput = await process.StandardError.ReadToEndAsync();
                Logger.LogInformation("Processed completed normally");

                // Attempt to get memory usage
                try
                {
                    result.MemoryUsedMB = process.WorkingSet64 / (1024 * 1024);
                }
                catch
                {
                    result.MemoryUsedMB = 0;
                }
            }

            stopwatch.Stop();
            result.ExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            result.EndTime = DateTime.UtcNow;

            Logger.LogInformation("Python script executed: Success={Success}, Time={Time}s, Exit={Exit}", 
                result.Success, result.ExecutionTimeSeconds, result.ExitCode);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.ErrorOutput = ex.Message;
            result.EndTime = DateTime.UtcNow;
            stopwatch.Stop();
            result.ExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            
            Logger.LogError(ex, "Error during Python script execution");
        }
        finally
        {
            // Cleanup
            try
            {
                process?.Dispose();
                if (tempFile != null && File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch (Exception cleanupEx)
            {
                Logger.LogWarning(cleanupEx, "Failed to cleanup execution resources");
            }
        }

        return result;
    }

    /// <summary>
    /// Validate script for security issues
    /// </summary>
    public async Task<bool> ValidateScriptSecurityAsync(string script)
    {
        try
        {
            // Check for dangerous imports
            var dangerousImports = new[]
            {
                "subprocess", "os.system", "eval", "exec", "compile", 
                "open", "__import__", "globals", "locals", "vars"
            };

            foreach (var dangerous in dangerousImports)
            {
                if (script.Contains(dangerous))
                {
                    Logger.LogWarning("Script contains potentially dangerous operation: {Operation}", dangerous);
                    return false;
                }
            }

            // Check for file system operations
            var fileOperations = new[] { "open(", "file(", "with open", "pathlib" };
            foreach (var fileOp in fileOperations)
            {
                if (script.Contains(fileOp))
                {
                    Logger.LogWarning("Script contains file system operation: {Operation}", fileOp);
                    return false;
                }
            }

            // Check for network operations
            var networkImports = new[] { "urllib", "requests", "socket", "http" };
            foreach (var netImport in networkImports)
            {
                if (script.Contains(netImport))
                {
                    Logger.LogWarning("Script contains network operation: {Operation}", netImport);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating script security");
            return false;
        }
    }

    /// <summary>
    /// Extract dependencies from Python script by parsing import statements
    /// </summary>
    public async Task<List<string>> ExtractScriptDependenciesAsync(string script)
    {
        var dependencies = new List<string>();

        try
        {
            var lines = script.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }
                
                // Remove inline comments
                var codeOnly = trimmed.Split('#')[0].Trim();
                if (string.IsNullOrEmpty(codeOnly))
                {
                    continue;
                }
                
                // Handle 'import module' or 'import module as alias' syntax
                if (codeOnly.StartsWith("import "))
                {
                    var importPart = codeOnly.Substring(7).Trim(); // Remove "import "
                    
                    // Handle multiple imports: import module1, module2, module3
                    var modules = importPart.Split(',');
                    foreach (var module in modules)
                    {
                        var cleanModule = module.Trim();
                        
                        // Handle 'module as alias' syntax - take the part before 'as'
                        if (cleanModule.Contains(" as "))
                        {
                            cleanModule = cleanModule.Split(new[] { " as " }, StringSplitOptions.None)[0].Trim();
                        }
                        
                        // Take only the top-level module name (before any dots)
                        var moduleName = cleanModule.Split('.')[0].Trim();
                        
                        if (!string.IsNullOrEmpty(moduleName) && !IsBuiltinModule(moduleName))
                        {
                            dependencies.Add(moduleName);
                        }
                    }
                }
                
                // Handle 'from module import ...' syntax
                else if (codeOnly.StartsWith("from ") && codeOnly.Contains(" import "))
                {
                    var parts = codeOnly.Split(new[] { " import " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        var fromPart = parts[0].Substring(5).Trim(); // Remove "from "
                        var moduleName = fromPart.Split('.')[0].Trim();
                        
                        if (!string.IsNullOrEmpty(moduleName) && !IsBuiltinModule(moduleName))
                        {
                            dependencies.Add(moduleName);
                        }
                    }
                }
            }

            return dependencies.Distinct().ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error extracting script dependencies");
            return dependencies;
        }
    }

    #region Environment Management

    /// <summary>
    /// Create a new Python virtual environment
    /// </summary>
    public async Task<bool> CreateEnvironmentAsync(string environmentName, PythonEnvironmentConfig config)
    {
        try
        {
            if (State.Environments.ContainsKey(environmentName))
            {
                Logger.LogWarning("Environment {EnvironmentName} already exists", environmentName);
                return false;
            }

            // Create virtual environment directory
            var envPath = GetEnvironmentPath(environmentName);
            var processInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"-m venv {envPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        Logger.LogError("Failed to create Python environment: {Error}", error);
                        return false;
                    }
                }
            }

            // Store environment configuration
            State.Environments[environmentName] = config;
            State.EnvironmentLastUsed[environmentName] = DateTime.UtcNow;
            State.EnvironmentPackages[environmentName] = new List<string>();

            // Log environment creation
            RaiseEvent(new EnvironmentCreatedLogEvent
            {
                EnvironmentName = environmentName,
                Config = config
            });
            await ConfirmEvents();
            
            // Publish environment created event
            await PublishAsync(new EnvironmentCreatedEvent
            {
                EnvironmentName = environmentName,
                Config = config
            });

            // Install dependencies if specified
            if (config.Dependencies.Any())
            {
                await InstallDependenciesAsync(environmentName, config.Dependencies);
            }

            Logger.LogInformation("Created Python environment: {EnvironmentName}", environmentName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating Python environment {EnvironmentName}", environmentName);
            return false;
        }
    }

    /// <summary>
    /// Delete a Python environment
    /// </summary>
    public async Task<bool> DeleteEnvironmentAsync(string environmentName)
    {
        try
        {
            if (!State.Environments.ContainsKey(environmentName))
            {
                return false;
            }

            var envPath = GetEnvironmentPath(environmentName);
            if (Directory.Exists(envPath))
            {
                Directory.Delete(envPath, true);
            }

            State.Environments.Remove(environmentName);
            State.EnvironmentLastUsed.Remove(environmentName);
            State.EnvironmentPackages.Remove(environmentName);

            // Log environment deletion
            RaiseEvent(new EnvironmentDeletedLogEvent
            {
                EnvironmentName = environmentName
            });
            await ConfirmEvents();
            
            // Publish environment deleted event
            await PublishAsync(new EnvironmentDeletedEvent
            {
                EnvironmentName = environmentName
            });

            Logger.LogInformation("Deleted Python environment: {EnvironmentName}", environmentName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting Python environment {EnvironmentName}", environmentName);
            return false;
        }
    }

    /// <summary>
    /// List all available environments
    /// </summary>
    public async Task<List<string>> ListEnvironmentsAsync()
    {
        return State.Environments.Keys.ToList();
    }

    /// <summary>
    /// Get environment configuration
    /// </summary>
    public async Task<PythonEnvironmentConfig?> GetEnvironmentConfigAsync(string environmentName)
    {
        return State.Environments.TryGetValue(environmentName, out var config) ? config : null;
    }

    /// <summary>
    /// Set default environment
    /// </summary>
    public async Task<bool> SetDefaultEnvironmentAsync(string environmentName)
    {
        if (!State.Environments.ContainsKey(environmentName))
        {
            return false;
        }

        State.DefaultEnvironmentName = environmentName;
        return true;
    }

    #endregion

    #region Dependency Management

    /// <summary>
    /// Install dependencies in specific environment
    /// </summary>
    public async Task<bool> InstallDependenciesAsync(string environmentName, List<string> dependencies)
    {
        try
        {
            if (!State.Environments.ContainsKey(environmentName))
            {
                Logger.LogWarning("Environment {EnvironmentName} does not exist, creating it", environmentName);
                var config = GetDefaultEnvironmentConfig();
                config.EnvironmentName = environmentName;
                var createResult = await CreateEnvironmentAsync(environmentName, config);
                if (!createResult)
                {
                    Logger.LogError("Failed to create environment {EnvironmentName}", environmentName);
                    return false;
                }
            }

            var envPath = GetEnvironmentPath(environmentName);
            var pipPath = GetPipPath(environmentName);
            
            // Check if pip exists and is accessible
            Logger.LogInformation("Environment path: {EnvPath}", envPath);
            Logger.LogInformation("Detected pip path: {PipPath}", pipPath);
            
            if (!File.Exists(pipPath) && pipPath != "pip3" && pipPath != "pip")
            {
                Logger.LogWarning("Pip not found at {PipPath}, trying system pip", pipPath);
                pipPath = "pip3";
            }
            
            Logger.LogInformation("Using pip command: {PipPath}", pipPath);

            var successCount = 0;
            foreach (var dependency in dependencies)
            {
                // Skip built-in modules
                if (IsBuiltinModule(dependency))
                {
                    Logger.LogDebug("Skipping built-in module {Dependency}", dependency);
                    successCount++;
                    continue;
                }

                // Try multiple installation strategies for better network resilience
                var installSuccess = await TryInstallPackageWithFallbackAsync(pipPath, dependency, envPath, environmentName);
                if (installSuccess)
                {
                    successCount++;
                }
            }

            var totalDependencies = dependencies.Count;
            Logger.LogInformation("Dependency installation completed: {SuccessCount}/{TotalCount} packages installed successfully", 
                successCount, totalDependencies);

            // Log dependency installation result
            RaiseEvent(new DependenciesInstalledLogEvent
            {
                EnvironmentName = environmentName,
                Dependencies = dependencies,
                Success = successCount > 0
            });
            await ConfirmEvents();
            
            // Publish dependencies installed event
            await PublishAsync(new DependenciesInstalledEvent
            {
                EnvironmentName = environmentName,
                Dependencies = dependencies,
                InstallationSuccess = successCount > 0,
                SuccessfulCount = successCount,
                TotalCount = totalDependencies
            });

            // Return true if we successfully installed most packages (allow some failures due to network issues)
            var successRate = (double)successCount / totalDependencies;
            Logger.LogInformation("Package installation success rate: {SuccessRate:P2} ({SuccessCount}/{TotalCount})", 
                successRate, successCount, totalDependencies);
                
            // Consider installation successful if we got at least 70% of packages
            return successRate >= 0.7 || successCount == totalDependencies;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error installing dependencies in environment {EnvironmentName}", environmentName);
            return false;
        }
    }

    public async Task<bool> InstallRequiredPackagesAsync(List<string> packages)
    {
        try
        {
            // Try to create/use virtual environment first
            var venvPipCommand = await SetupVirtualEnvironmentAsync();
            var pipCommand = !string.IsNullOrEmpty(venvPipCommand) ? venvPipCommand : await FindAvailablePipCommandAsync();
            
            if (string.IsNullOrEmpty(pipCommand))
            {
                Logger.LogWarning("No pip command found. Skipping package installation. This may be expected in containerized environments.");
                return true; // Don't fail the entire process if pip is not available
            }

            Logger.LogInformation("Using pip command: {PipCommand}", pipCommand);

            var installationResults = new List<(string Package, bool Success, string Error)>();

            foreach (var package in packages)
            {
                var packageInstalled = false;
                var lastError = string.Empty;
                
                // Try multiple installation strategies for externally-managed environments
                var installationStrategies = new[]
                {
                    new { Args = new[] { "install", package, "--user", "--quiet", "--disable-pip-version-check" }, Name = "user install" },
                    new { Args = new[] { "install", package, "--break-system-packages", "--quiet", "--disable-pip-version-check" }, Name = "system install (break-system-packages)" },
                    new { Args = new[] { "install", package, "--quiet", "--disable-pip-version-check" }, Name = "default install" }
                };

                foreach (var strategy in installationStrategies)
                {
                    try
                    {
                        ProcessStartInfo processInfo;
                        
                        if (pipCommand.Contains(' '))
                        {
                            // Handle compound commands like "python -m pip"
                            var parts = pipCommand.Split(' ');
                            processInfo = new ProcessStartInfo
                            {
                                FileName = parts[0],
                                Arguments = $"{string.Join(" ", parts.Skip(1))} {string.Join(" ", strategy.Args)}",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                        }
                        else
                        {
                            // Handle simple commands like "pip" or "pip3"
                            processInfo = new ProcessStartInfo
                            {
                                FileName = pipCommand,
                                Arguments = string.Join(" ", strategy.Args),
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                        }

                        using var process = Process.Start(processInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            var stdout = await process.StandardOutput.ReadToEndAsync();
                            var stderr = await process.StandardError.ReadToEndAsync();
                                
                            if (process.ExitCode == 0)
                            {
                                Logger.LogInformation("Successfully installed package {Package} using {Strategy}", package, strategy.Name);
                                packageInstalled = true;
                                installationResults.Add((package, true, string.Empty));
                                break; // Exit the strategy loop on success
                            }

                            lastError = !string.IsNullOrEmpty(stderr) ? stderr : stdout;
                            Logger.LogDebug("Strategy {Strategy} failed for package {Package}: {Error}", strategy.Name, package, lastError);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex.Message;
                        Logger.LogDebug(ex, "Exception with strategy {Strategy} for package {Package}", strategy.Name, package);
                    }
                }

                if (!packageInstalled)
                {
                    Logger.LogWarning("Failed to install package {Package} with all strategies. Last error: {Error}", package, lastError);
                    installationResults.Add((package, false, lastError));
                }
            }

            // Log summary
            var successCount = installationResults.Count(r => r.Success);
            var totalCount = installationResults.Count;
            Logger.LogInformation("Package installation summary: {SuccessCount}/{TotalCount} packages installed successfully", 
                successCount, totalCount);

            // Return true if at least some packages were installed, or if no packages were requested
            return packages.Count == 0 || successCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during Python package installation process");
            return false;
        }
    }

    /// <summary>
    /// Get installed packages in environment
    /// </summary>
    public async Task<List<string>> GetInstalledPackagesAsync(string environmentName)
    {
        if (State.EnvironmentPackages.TryGetValue(environmentName, out var packages))
        {
            return packages;
        }
        return new List<string>();
    }

    /// <summary>
    /// Uninstall package from environment
    /// </summary>
    public async Task<bool> UninstallPackageAsync(string environmentName, string packageName)
    {
        try
        {
            if (!State.Environments.ContainsKey(environmentName))
            {
                return false;
            }

            var pipPath = GetPipPath(environmentName);
            var processInfo = new ProcessStartInfo
            {
                FileName = pipPath,
                Arguments = $"uninstall -y {packageName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        State.EnvironmentPackages[environmentName].Remove(packageName);
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uninstalling package {PackageName} from environment {EnvironmentName}", 
                packageName, environmentName);
            return false;
        }
    }

    #endregion

    #region Security and Sandboxing

    /// <summary>
    /// Enable sandbox for environment
    /// </summary>
    public async Task<bool> EnableSandboxForEnvironmentAsync(string environmentName)
    {
        if (!State.Environments.ContainsKey(environmentName))
        {
            return false;
        }

        State.Environments[environmentName].IsSandboxed = true;
        State.EnvironmentSecurity[environmentName] = "sandboxed";
        return true;
    }

    /// <summary>
    /// Set execution limits for environment
    /// </summary>
    public async Task<bool> SetExecutionLimitsAsync(string environmentName, int maxTimeSeconds, long maxMemoryMB)
    {
        if (!State.Environments.ContainsKey(environmentName))
        {
            return false;
        }

        var config = State.Environments[environmentName];
        config.MaxExecutionTimeSeconds = maxTimeSeconds;
        config.MaxMemoryMB = maxMemoryMB;
        return true;
    }

    /// <summary>
    /// Restrict network access for environment
    /// </summary>
    public async Task<bool> RestrictNetworkAccessAsync(string environmentName, bool allowAccess)
    {
        if (!State.Environments.ContainsKey(environmentName))
        {
            return false;
        }

        State.Environments[environmentName].EnableNetworkAccess = allowAccess;
        return true;
    }

    /// <summary>
    /// Set allowed modules for environment
    /// </summary>
    public async Task<bool> SetAllowedModulesAsync(string environmentName, List<string> allowedModules)
    {
        if (!State.Environments.ContainsKey(environmentName))
        {
            return false;
        }

        State.Environments[environmentName].AllowedModules = allowedModules;
        return true;
    }

    #endregion

    #region Python Command Detection and Management

    /// <summary>
    /// Detect the best available Python command on the system
    /// </summary>
    public async Task<string> DetectBestPythonCommandAsync()
    {
        var availableCommands = await GetAvailablePythonCommandsAsync();
        
        if (availableCommands.Any())
        {
            // Prefer python3 over python, and newer versions over older ones
            var preferenceOrder = new[] { "python3", "python" };

            foreach (var preferred in preferenceOrder)
            {
                if (availableCommands.Contains(preferred))
                {
                    Logger.LogInformation("Detected best Python command: {PythonCommand}", preferred);
                    return preferred;
                }
            }
            
            // Return the first available if no preference matches
            var firstAvailable = availableCommands.First();
            Logger.LogInformation("Using first available Python command: {PythonCommand}", firstAvailable);
            return firstAvailable;
        }
        
        Logger.LogWarning("No Python command detected on system");
        return string.Empty;
    }

    /// <summary>
    /// Get all available Python commands on the system
    /// </summary>
    public async Task<List<string>> GetAvailablePythonCommandsAsync()
    {
        var candidateCommands = new[] { "python3", "python", "python3.12", "python3.11", "python3.10", "python3.9" };
        var availableCommands = new List<string>();
        
        foreach (var command in candidateCommands)
        {
            try
            {
                // Use ValidatePythonCommandAsync to actually check if the command works
                if (await ValidatePythonCommandAsync(command))
                {
                    availableCommands.Add(command);
                    Logger.LogDebug("Found working Python command: {PythonCommand}", command);
                }
                else
                {
                    Logger.LogDebug("Python command not working: {PythonCommand}", command);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Error checking Python command: {PythonCommand}", command);
            }
        }
        
        Logger.LogInformation("Available Python commands: {AvailableCommands}", string.Join(", ", availableCommands));
        return availableCommands;
    }

    /// <summary>
    /// Validate that a Python command is available and working
    /// </summary>
    public async Task<bool> ValidatePythonCommandAsync(string pythonCommand)
    {
        if (string.IsNullOrEmpty(pythonCommand))
        {
            return false;
        }

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = pythonCommand,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return false;
            }

            // Set a reasonable timeout for version check
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // ignored
                    }

                    return false;
                }
            }

            if (process.ExitCode == 0)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                var errorOutput = await process.StandardError.ReadToEndAsync();
                    
                // Python might output version to stderr (Python 2) or stdout (Python 3)
                var versionOutput = !string.IsNullOrEmpty(output) ? output : errorOutput;
                    
                if (!string.IsNullOrEmpty(versionOutput) && 
                    (versionOutput.Contains("Python") || versionOutput.Contains("python")))
                {
                    Logger.LogDebug("Validated Python command {PythonCommand}: {Version}", 
                        pythonCommand, versionOutput.Trim());
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to validate Python command: {PythonCommand}", pythonCommand);
        }

        return false;
    }

    /// <summary>
    /// Get Python version for a specific command
    /// </summary>
    public async Task<string> GetPythonVersionAsync(string pythonCommand)
    {
        if (string.IsNullOrEmpty(pythonCommand))
        {
            return string.Empty;
        }

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = pythonCommand,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    return string.Empty;
                }

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        return string.Empty;
                    }
                }

                if (process.ExitCode == 0)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var errorOutput = await process.StandardError.ReadToEndAsync();
                    
                    // Python might output version to stderr (Python 2) or stdout (Python 3)
                    var versionOutput = !string.IsNullOrEmpty(output) ? output : errorOutput;
                    
                    if (!string.IsNullOrEmpty(versionOutput))
                    {
                        return versionOutput.Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting Python version for command: {PythonCommand}", pythonCommand);
        }

        return string.Empty;
    }

    /// <summary>
    /// Set Python command for a specific environment
    /// </summary>
    public async Task<bool> SetPythonCommandForEnvironmentAsync(string environmentName, string pythonCommand)
    {
        if (!State.Environments.ContainsKey(environmentName))
        {
            Logger.LogWarning("Environment {EnvironmentName} does not exist", environmentName);
            return false;
        }

        if (!await ValidatePythonCommandAsync(pythonCommand))
        {
            Logger.LogWarning("Python command {PythonCommand} is not valid", pythonCommand);
            return false;
        }

        State.Environments[environmentName].PythonCommand = pythonCommand;
        State.Environments[environmentName].AutoDetectPython = false; // Disable auto-detection when manually set
        
        Logger.LogInformation("Set Python command for environment {EnvironmentName}: {PythonCommand}", 
            environmentName, pythonCommand);
        
        return true;
    }

    #endregion

    #region Execution History and Monitoring

    /// <summary>
    /// Get execution history
    /// </summary>
    public async Task<List<ScriptExecutionResult>> GetExecutionHistoryAsync()
    {
        return State.ExecutionHistory.OrderByDescending(r => r.StartTime).ToList();
    }

    /// <summary>
    /// Get execution statistics
    /// </summary>
    public async Task<Dictionary<string, int>> GetExecutionStatsAsync()
    {
        var stats = new Dictionary<string, int>
        {
            ["Total"] = State.ExecutionHistory.Count,
            ["Successful"] = State.ExecutionHistory.Count(r => r.Success),
            ["Failed"] = State.ExecutionHistory.Count(r => !r.Success),
            ["TimedOut"] = State.ExecutionHistory.Count(r => r.TimedOut),
            ["ActiveExecutions"] = State.ActiveExecutions.Count,
            ["TotalEnvironments"] = State.Environments.Count
        };

        if (State.ExecutionHistory.Count > 0)
        {
            stats["AverageExecutionTime"] = (int)(State.ExecutionHistory.Average(r => r.ExecutionTimeSeconds) * 1000); // in ms
            stats["AverageMemoryUsage"] = (int)State.ExecutionHistory.Where(r => r.MemoryUsedMB > 0).DefaultIfEmpty().Average(r => r.MemoryUsedMB);
        }

        return stats;
    }

    #endregion

    #region State Transition

    protected override void GAgentTransitionState(PythonExecutionState state,
        StateLogEventBase<PythonExecutionStateLogEvent> @event)
    {
        switch (@event)
        {
            case ExecutionInitializedLogEvent initEvent:
                state.Initialized = true;
                state.PythonEnvironmentPath = initEvent.PythonPath;
                state.RequiredPackages = initEvent.InstalledPackages;
                break;

            case ScriptExecutedLogEvent executedEvent:
                // Execution history is managed in the method itself
                break;

            case EnvironmentCreatedLogEvent createdEvent:
                // Environment is already added in the method
                break;

            case EnvironmentDeletedLogEvent deletedEvent:
                // Environment is already removed in the method
                break;

            case DependenciesInstalledLogEvent dependenciesEvent:
                // Dependencies are already managed in the method
                break;
        }
    }

    #endregion

    #region Helper Methods

    private async Task<string> CheckPythonInstallationAsync(string pythonPath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                return output.Trim();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking Python installation");
        }

        return string.Empty;
    }

    private PythonEnvironmentConfig GetDefaultEnvironmentConfig()
    {
        if (State.Environments.TryGetValue(State.DefaultEnvironmentName, out var config))
        {
            return config;
        }

        return new PythonEnvironmentConfig
        {
            EnvironmentName = "default",
            PythonVersion = "3.9",
            MaxExecutionTimeSeconds = 30,
            MaxMemoryMB = 512,
            IsSandboxed = true,
            EnableNetworkAccess = false
        };
    }

    private string ComputeScriptHash(string script)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(script));
            return Convert.ToHexString(hash)[..16]; // First 16 characters
        }
    }

    private string CreateSecureTempDirectory(PythonEnvironmentConfig config)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "python_execution", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        if (!string.IsNullOrEmpty(config.WorkingDirectory))
        {
            config.WorkingDirectory = tempDir;
        }
        
        return tempDir;
    }

    private string WrapScriptWithSandbox(string script, PythonEnvironmentConfig config)
    {
        var wrapper = new StringBuilder();
        
        // Add security imports and restrictions (import everything we need BEFORE blocking imports)
        wrapper.AppendLine("import sys");
        wrapper.AppendLine("import builtins");
        wrapper.AppendLine("import resource");
        if (!config.EnableNetworkAccess)
        {
            wrapper.AppendLine("import socket");
        }
        wrapper.AppendLine();
        
        // Add resource monitoring BEFORE blocking builtins (skip for timeout testing)
        wrapper.AppendLine("# Resource monitoring");
        wrapper.AppendLine("try:");
        wrapper.AppendLine($"    resource.setrlimit(resource.RLIMIT_AS, ({config.MaxMemoryMB * 1024 * 1024}, {config.MaxMemoryMB * 1024 * 1024}))");
        wrapper.AppendLine("except ValueError:");
        wrapper.AppendLine("    pass  # Skip resource limit if it fails");
        wrapper.AppendLine();
        
        // Network restrictions
        if (!config.EnableNetworkAccess)
        {
            wrapper.AppendLine("# Network restrictions");
            wrapper.AppendLine("socket.socket = None");
            wrapper.AppendLine();
        }
        
        // Restrict dangerous builtins (AFTER importing what we need)
        wrapper.AppendLine("# Security restrictions");
        wrapper.AppendLine("builtins.open = None");
        wrapper.AppendLine("builtins.eval = None");
        wrapper.AppendLine("builtins.exec = None");
        wrapper.AppendLine("builtins.compile = None");
        wrapper.AppendLine("builtins.__import__ = None");
        wrapper.AppendLine();
        
        // Add the actual script
        wrapper.AppendLine("# User script");
        wrapper.AppendLine(script);
        
        return wrapper.ToString();
    }

    private async Task<ProcessStartInfo> CreateSecureProcessInfoAsync(string scriptPath, PythonEnvironmentConfig config)
    {
        Logger.LogInformation($"Environment Name: {config.EnvironmentName}");
        var pythonPath = await GetPythonPath(config.EnvironmentName);
        Logger.LogInformation($"Python Path: {pythonPath}");

        var processInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = scriptPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = config.WorkingDirectory
        };

        // Add environment variables
        foreach (var envVar in config.EnvironmentVariables)
        {
            processInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
        }

        return processInfo;
    }

    private string GetEnvironmentPath(string environmentName)
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "python_environments");
        Directory.CreateDirectory(baseDir);
        return Path.Combine(baseDir, environmentName);
    }

    private async Task<string> GetPythonPathAsync(string environmentName)
    {
        if (State.Environments.ContainsKey(environmentName))
        {
            var config = State.Environments[environmentName];
            
            // Use specific Python command if configured
            if (!string.IsNullOrEmpty(config.PythonCommand))
            {
                // Check virtual environment first
                var envPath = GetEnvironmentPath(environmentName);
                var envPythonPath = GetEnvironmentSpecificPythonPath(envPath, config.PythonCommand);
                if (!string.IsNullOrEmpty(envPythonPath) && File.Exists(envPythonPath))
                {
                    return envPythonPath;
                }
                
                // Return configured command (could be system-wide)
                return config.PythonCommand;
            }
            
            // Auto-detect if enabled
            if (config.AutoDetectPython)
            {
                var detected = await DetectBestPythonCommandAsync();
                if (!string.IsNullOrEmpty(detected))
                {
                    var envPath = GetEnvironmentPath(environmentName);
                    var envPythonPath = GetEnvironmentSpecificPythonPath(envPath, detected);
                    if (!string.IsNullOrEmpty(envPythonPath) && File.Exists(envPythonPath))
                    {
                        return envPythonPath;
                    }
                    return detected;
                }
            }
            
            // Try environment-specific paths with default commands
            var envPath2 = GetEnvironmentPath(environmentName);
            foreach (var cmd in config.PreferredPythonCommands)
            {
                var envPythonPath = GetEnvironmentSpecificPythonPath(envPath2, cmd);
                if (!string.IsNullOrEmpty(envPythonPath) && File.Exists(envPythonPath))
                {
                    return envPythonPath;
                }
            }
        }
        
        // Fallback to system-wide detection
        var systemPython = await DetectBestPythonCommandAsync();
        return !string.IsNullOrEmpty(systemPython) ? systemPython : "python3";
    }

    private async Task<string> GetPythonPath(string environmentName)
    {
        // Synchronous wrapper for backward compatibility
        return await GetPythonPathAsync(environmentName);
    }

    private string GetEnvironmentSpecificPythonPath(string envPath, string pythonCommand)
    {
        // Extract command name from full path
        var commandName = Path.GetFileName(pythonCommand);
        
        // Unix/Linux/macOS path
        var unixPath = Path.Combine(envPath, "bin", commandName);
        if (File.Exists(unixPath))
        {
            return unixPath;
        }
        
        // Windows path
        var windowsExe = commandName.EndsWith(".exe") ? commandName : commandName + ".exe";
        var windowsPath = Path.Combine(envPath, "Scripts", windowsExe);
        if (File.Exists(windowsPath))
        {
            return windowsPath;
        }
        
        return string.Empty;
    }

    private string GetPipPath(string environmentName)
    {
        var envPath = GetEnvironmentPath(environmentName);
        var pipPath = Path.Combine(envPath, "bin", "pip3");
        if (File.Exists(pipPath))
        {
            return pipPath;
        }
        
        // Windows path
        pipPath = Path.Combine(envPath, "Scripts", "pip.exe");
        if (File.Exists(pipPath))
        {
            return pipPath;
        }
        
        return "pip3"; // Fallback to system pip
    }

    private bool IsBuiltinModule(string module)
    {
        var builtinModules = new HashSet<string>
        {
            "sys", "os", "math", "json", "re", "datetime", "collections", 
            "itertools", "functools", "operator", "copy", "pickle", "base64",
            "hashlib", "hmac", "secrets", "string", "textwrap", "unicodedata",
            "struct", "codecs", "types", "weakref", "gc", "inspect"
        };
        
        return builtinModules.Contains(module);
    }

    /// <summary>
    /// Try to install a package with multiple fallback strategies for network resilience
    /// </summary>
    private async Task<bool> TryInstallPackageWithFallbackAsync(string pipPath, string dependency, string envPath, string environmentName)
    {
        var strategies = new[]
        {
            // Strategy 1: Normal installation
            $"install {dependency} --quiet --disable-pip-version-check",
            
            // Strategy 2: With trusted hosts (bypass SSL issues)
            $"install {dependency} --quiet --disable-pip-version-check --trusted-host pypi.org --trusted-host pypi.python.org --trusted-host files.pythonhosted.org",
            
            // Strategy 3: Without proxy
            $"install {dependency} --quiet --disable-pip-version-check --no-proxy",
            
            // Strategy 4: With timeout and retries
            $"install {dependency} --quiet --disable-pip-version-check --timeout 60 --retries 2",
            
            // Strategy 5: Use index-url fallback
            $"install {dependency} --quiet --disable-pip-version-check --index-url https://pypi.python.org/simple/ --trusted-host pypi.python.org"
        };

        foreach (var (strategy, index) in strategies.Select((s, i) => (s, i)))
        {
            Logger.LogDebug("Trying installation strategy {Strategy} for {Dependency}: {Command}", 
                index + 1, dependency, strategy);
                
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = pipPath,
                    Arguments = strategy,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = envPath
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(3)); // Extended timeout for network issues
                        var processTask = process.WaitForExitAsync();
                        
                        var completedTask = await Task.WhenAny(processTask, timeoutTask);
                        if (completedTask == timeoutTask)
                        {
                            Logger.LogWarning("Timeout installing {Dependency} with strategy {Strategy}, trying next strategy", 
                                dependency, index + 1);
                            process.Kill();
                            continue;
                        }

                        var error = await process.StandardError.ReadToEndAsync();
                        var output = await process.StandardOutput.ReadToEndAsync();

                        if (process.ExitCode == 0)
                        {
                            if (!State.EnvironmentPackages[environmentName].Contains(dependency))
                            {
                                State.EnvironmentPackages[environmentName].Add(dependency);
                            }
                            Logger.LogInformation("Successfully installed {Dependency} using strategy {Strategy}", 
                                dependency, index + 1);
                            return true;
                        }
                        else
                        {
                            // Check if it's a network-related error
                            var isNetworkError = IsNetworkRelatedError(error, output);
                            if (isNetworkError)
                            {
                                Logger.LogWarning("Network error installing {Dependency} with strategy {Strategy}: {Error}", 
                                    dependency, index + 1, error.Trim());
                            }
                            else
                            {
                                Logger.LogError("Failed to install {Dependency} with strategy {Strategy}. Exit code: {ExitCode}, Error: {Error}", 
                                    dependency, index + 1, process.ExitCode, error.Trim());
                                // If it's not a network error, no point trying other strategies
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Exception installing {Dependency} with strategy {Strategy}", dependency, index + 1);
            }
        }

        Logger.LogWarning("All installation strategies failed for package {Dependency}", dependency);
        return false;
    }

    /// <summary>
    /// Check if an error is network-related (proxy, connection, etc.)
    /// </summary>
    private bool IsNetworkRelatedError(string error, string output)
    {
        var networkErrorKeywords = new[]
        {
            "ProxyError", "proxy", "Connection reset", "Connection timed out",
            "Connection refused", "Network is unreachable", "Name resolution failed",
            "SSL", "certificate", "timeout", "could not find a version",
            "No matching distribution found", "HTTP Error", "URLError"
        };

        var combinedOutput = (error + " " + output).ToLower();
        return networkErrorKeywords.Any(keyword => combinedOutput.Contains(keyword.ToLower()));
    }

    private async Task<string> FindAvailablePipCommandAsync()
    {
        var pipCommands = new[] { "pip", "pip3", "python -m pip", "python3 -m pip" };
        
        foreach (var command in pipCommands)
        {
            try
            {
                var parts = command.Split(' ');
                var fileName = parts[0];
                var arguments = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) + " --version" : "--version";

                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        Logger.LogDebug("Found working pip command: {Command}", command);
                        return command;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Command {Command} not available", command);
            }
        }

        return string.Empty;
    }

    private async Task<string> SetupVirtualEnvironmentAsync()
    {
        try
        {
            var venvPath = Path.Combine(Path.GetTempPath(), "aevatar_python_venv");
            
            // Handle different OS path structures
            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var venvPipPath = isWindows 
                ? Path.Combine(venvPath, "Scripts", "pip.exe")
                : Path.Combine(venvPath, "bin", "pip");
            var venvPythonPath = isWindows 
                ? Path.Combine(venvPath, "Scripts", "python.exe")
                : Path.Combine(venvPath, "bin", "python");
            
            // Check if venv already exists and is functional
            if (Directory.Exists(venvPath) && File.Exists(venvPipPath))
            {
                Logger.LogDebug("Virtual environment already exists at {VenvPath}", venvPath);
                return venvPipPath;
            }
            
            Logger.LogInformation("Creating virtual environment at {VenvPath}", venvPath);
            
            // Find Python command for creating venv
            var pythonCommands = new[] { "python3", "python" };
            string workingPythonCommand = string.Empty;
            
            foreach (var cmd in pythonCommands)
            {
                try
                {
                    var testProcess = new ProcessStartInfo
                    {
                        FileName = cmd,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(testProcess);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        if (process.ExitCode == 0)
                        {
                            workingPythonCommand = cmd;
                            break;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            if (string.IsNullOrEmpty(workingPythonCommand))
            {
                Logger.LogWarning("No working Python command found for creating virtual environment");
                return string.Empty;
            }
            
            // Create virtual environment
            var createVenvProcess = new ProcessStartInfo
            {
                FileName = workingPythonCommand,
                Arguments = $"-m venv {venvPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using (var process = Process.Start(createVenvProcess))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        Logger.LogInformation("Virtual environment created successfully");
                        
                        // Verify pip exists in the venv
                        if (File.Exists(venvPipPath))
                        {
                            Logger.LogInformation("Using virtual environment pip: {VenvPip}", venvPipPath);
                            return venvPipPath;
                        }
                    }
                    else
                    {
                        var stderr = await process.StandardError.ReadToEndAsync();
                        Logger.LogWarning("Failed to create virtual environment: {Error}", stderr);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Exception while setting up virtual environment");
        }
        
        return string.Empty;
    }

    #endregion
}