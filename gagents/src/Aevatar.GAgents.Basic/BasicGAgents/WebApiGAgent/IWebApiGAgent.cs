using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.WebApiGAgent;

/// <summary>
/// WebAPI GAgent interface for HTTP API interactions
/// </summary>
public interface IWebApiGAgent : IStateGAgent<WebApiGAgentState>
{
    /// <summary>
    /// Execute GET request
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="headers">Additional headers</param>
    /// <returns>WebAPI response</returns>
    Task<WebApiResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null);
    
    /// <summary>
    /// Execute POST request
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="payload">Request payload</param>
    /// <param name="headers">Additional headers</param>
    /// <returns>WebAPI response</returns>
    Task<WebApiResponse<T>> PostAsync<T>(string endpoint, object? payload = null, Dictionary<string, string>? headers = null);
    
    /// <summary>
    /// Execute PUT request
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="payload">Request payload</param>
    /// <param name="headers">Additional headers</param>
    /// <returns>WebAPI response</returns>
    Task<WebApiResponse<T>> PutAsync<T>(string endpoint, object? payload = null, Dictionary<string, string>? headers = null);
    
    /// <summary>
    /// Execute DELETE request
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="headers">Additional headers</param>
    /// <returns>WebAPI response</returns>
    Task<WebApiResponse<T>> DeleteAsync<T>(string endpoint, Dictionary<string, string>? headers = null);
    
    /// <summary>
    /// Execute generic HTTP request
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="endpoint">API endpoint (relative to base URL)</param>
    /// <param name="payload">Request payload</param>
    /// <param name="headers">Additional headers</param>
    /// <returns>WebAPI response with string content</returns>
    Task<WebApiResponse<string>> RequestAsync(HttpMethod method, string endpoint, object? payload = null, Dictionary<string, string>? headers = null);
    
    /// <summary>
    /// Update GAgent configuration
    /// </summary>
    /// <param name="configuration">New configuration</param>
    /// <returns>Whether update was successful</returns>
    Task<bool> UpdateConfigurationAsync(WebApiGAgentConfiguration configuration);
    
    /// <summary>
    /// Get current configuration
    /// </summary>
    /// <returns>Current configuration</returns>
    Task<WebApiGAgentConfiguration> GetConfigurationAsync();
    
    /// <summary>
    /// Get health status
    /// </summary>
    /// <returns>Health status</returns>
    Task<WebApiHealthStatus> GetHealthStatusAsync();
    
    /// <summary>
    /// Get request history
    /// </summary>
    /// <param name="limit">Maximum number of records</param>
    /// <returns>Request history</returns>
    Task<List<WebApiRequestRecord>> GetRequestHistoryAsync(int limit = 50);
    
    /// <summary>
    /// Get metrics
    /// </summary>
    /// <returns>API metrics</returns>
    Task<WebApiMetrics> GetMetricsAsync();
    
    /// <summary>
    /// Test connection to API
    /// </summary>
    /// <returns>Whether connection test was successful</returns>
    Task<bool> TestConnectionAsync();
}
