using Newtonsoft.Json;

namespace Aevatar.GAgents.GroupChat.Core.States;

/// <summary>
/// Extension methods for WorkUnitExecutionFlowRecord to provide convenient parsing
/// </summary>
public static class WorkUnitExecutionFlowRecordExtensions
{
    /// <summary>
    /// Gets structured input parameters (on-demand parsing)
    /// </summary>
    public static Dictionary<string, object>? GetInputParameters(this WorkUnitExecutionFlowRecord record)
    {
        if (string.IsNullOrEmpty(record.InputDataJson)) return null;
        
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(record.InputDataJson);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Gets structured output parameters (on-demand parsing)
    /// </summary>
    public static Dictionary<string, object>? GetOutputParameters(this WorkUnitExecutionFlowRecord record)
    {
        if (string.IsNullOrEmpty(record.OutputDataJson)) return null;
        
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(record.OutputDataJson);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Gets current agent state (on-demand parsing)
    /// Based on execution phase, it could be pre-execution or post-execution state
    /// </summary>
    public static T? GetCurrentState<T>(this WorkUnitExecutionFlowRecord record) where T : class
    {
        if (string.IsNullOrEmpty(record.CurrentStateJson)) return null;
        
        try
        {
            return JsonConvert.DeserializeObject<T>(record.CurrentStateJson);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Gets execution duration
    /// </summary>
    public static TimeSpan? GetDuration(this WorkUnitExecutionFlowRecord record)
    {
        if (!record.StartTime.HasValue || !record.EndTime.HasValue) return null;
        return record.EndTime.Value - record.StartTime.Value;
    }
    
    /// <summary>
    /// Checks if the record represents a completed execution
    /// </summary>
    public static bool IsCompleted(this WorkUnitExecutionFlowRecord record)
    {
        return record.Status == WorkflowExecutionStatus.Completed;
    }
    
    /// <summary>
    /// Checks if the current state represents pre-execution or post-execution
    /// </summary>
    public static bool IsPreExecutionState(this WorkUnitExecutionFlowRecord record)
    {
        return record.Status == WorkflowExecutionStatus.Running;
    }
    
    /// <summary>
    /// Checks if the current state represents post-execution
    /// </summary>
    public static bool IsPostExecutionState(this WorkUnitExecutionFlowRecord record)
    {
        return record.Status == WorkflowExecutionStatus.Completed;
    }
    
    /// <summary>
    /// Gets a summary string for debugging
    /// </summary>
    public static string GetSummary(this WorkUnitExecutionFlowRecord record)
    {
        var duration = record.GetDuration();
        var durationStr = duration?.TotalMilliseconds.ToString("F0") + "ms" ?? "N/A";
        
        return $"Agent: {record.AgentGrainId}, Status: {record.Status}, Duration: {durationStr}, " +
               $"Sources: [{string.Join(", ", record.BeforeAgentIds)}], Retries: {record.RetryCount}";
    }
}
