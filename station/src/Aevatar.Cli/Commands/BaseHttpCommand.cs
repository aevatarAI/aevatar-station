using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aevatar.Cli.Args;
using Aevatar.Cli.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Cli.Commands;

public abstract class BaseHttpCommand : IConsoleCommand, ITransientDependency
{
    public ILogger<BaseHttpCommand> Logger { get; set; }
    
    protected readonly AuthenticationService AuthService;
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly string DefaultApiHost = "http://localhost:7002";

    protected BaseHttpCommand(AuthenticationService authService, IHttpClientFactory httpClientFactory)
    {
        AuthService = authService;
        HttpClientFactory = httpClientFactory;
        Logger = NullLogger<BaseHttpCommand>.Instance;
    }

    public abstract Task ExecuteAsync(CommandLineArgs commandLineArgs);
    public abstract string GetUsageInfo();

    protected async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = HttpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        
        var token = await AuthService.GetAccessTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        return client;
    }

    protected async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> parameters = null)
    {
        using var client = await CreateAuthenticatedClientAsync();
        
        var url = BuildUrl(endpoint, parameters);
        Logger.LogDebug("GET {Url}", url);
        
        var response = await client.GetAsync(url);
        await EnsureSuccessResponseAsync(response);
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    protected async Task<T> PostAsync<T>(string endpoint, object data = null)
    {
        using var client = await CreateAuthenticatedClientAsync();
        
        var url = $"{DefaultApiHost}{endpoint}";
        Logger.LogDebug("POST {Url}", url);
        
        HttpContent content = null;
        if (data != null)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        else
        {
            content = new StringContent(string.Empty);
        }
        
        var response = await client.PostAsync(url, content);
        await EnsureSuccessResponseAsync(response);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrWhiteSpace(responseContent) || responseContent == "{}")
        {
            return default(T);
        }
        
        return JsonSerializer.Deserialize<T>(responseContent, JsonOptions);
    }

    protected async Task<T> PutAsync<T>(string endpoint, object data)
    {
        using var client = await CreateAuthenticatedClientAsync();
        
        var url = $"{DefaultApiHost}{endpoint}";
        Logger.LogDebug("PUT {Url}", url);
        
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PutAsync(url, content);
        await EnsureSuccessResponseAsync(response);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, JsonOptions);
    }

    protected async Task DeleteAsync(string endpoint)
    {
        using var client = await CreateAuthenticatedClientAsync();
        
        var url = $"{DefaultApiHost}{endpoint}";
        Logger.LogDebug("DELETE {Url}", url);
        
        var response = await client.DeleteAsync(url);
        await EnsureSuccessResponseAsync(response);
    }

    private string BuildUrl(string endpoint, Dictionary<string, string> parameters = null)
    {
        var url = $"{DefaultApiHost}{endpoint}";
        
        if (parameters != null && parameters.Any())
        {
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            url += $"?{queryString}";
        }
        
        return url;
    }

    private async Task EnsureSuccessResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        var errorMessage = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
        
        if (!string.IsNullOrWhiteSpace(errorContent))
        {
            try
            {
                var errorJson = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                if (!string.IsNullOrWhiteSpace(errorJson?.Message))
                {
                    errorMessage += $": {errorJson.Message}";
                }
            }
            catch
            {
                errorMessage += $": {errorContent}";
            }
        }
        
        Logger.LogError("API request failed: {Message}", errorMessage);
        throw new CliUsageException($"API request failed: {errorMessage}");
    }

    protected void OutputTable<T>(List<T> items, params (string Header, Func<T, object> ValueSelector)[] columns)
    {
        if (!items.Any())
        {
            Logger.LogInformation("No items found.");
            return;
        }

        // Calculate column widths
        var columnWidths = new int[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            var header = columns[i].Header;
            var maxValueLength = items.Max(item => columns[i].ValueSelector(item)?.ToString()?.Length ?? 0);
            columnWidths[i] = Math.Max(header.Length, maxValueLength) + 2;
        }

        // Print header
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.Append("┌");
        for (int i = 0; i < columns.Length; i++)
        {
            sb.Append(new string('─', columnWidths[i]));
            if (i < columns.Length - 1) sb.Append("┬");
        }
        sb.AppendLine("┐");
        
        sb.Append("│");
        for (int i = 0; i < columns.Length; i++)
        {
            var header = columns[i].Header;
            sb.Append($" {header}".PadRight(columnWidths[i]));
            if (i < columns.Length - 1) sb.Append("│");
        }
        sb.AppendLine("│");
        
        sb.Append("├");
        for (int i = 0; i < columns.Length; i++)
        {
            sb.Append(new string('─', columnWidths[i]));
            if (i < columns.Length - 1) sb.Append("┼");
        }
        sb.AppendLine("┤");

        // Print rows
        foreach (var item in items)
        {
            sb.Append("│");
            for (int i = 0; i < columns.Length; i++)
            {
                var value = columns[i].ValueSelector(item)?.ToString() ?? "";
                sb.Append($" {value}".PadRight(columnWidths[i]));
                if (i < columns.Length - 1) sb.Append("│");
            }
            sb.AppendLine("│");
        }
        
        sb.Append("└");
        for (int i = 0; i < columns.Length; i++)
        {
            sb.Append(new string('─', columnWidths[i]));
            if (i < columns.Length - 1) sb.Append("┴");
        }
        sb.AppendLine("┘");
        
        Logger.LogInformation(sb.ToString());
    }

    protected void OutputJson(object data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        Logger.LogInformation(json);
    }

    protected string GetArgument(CommandLineArgs args, int index, string? defaultValue = null)
    {
        // Index 0 is the subcommand (Target), subsequent args would need to be parsed from options
        if (index == 0) return args.Target ?? defaultValue;
        
        // For additional arguments beyond the subcommand, check options with numeric keys
        var argKey = $"arg{index}";
        return GetOption(args, argKey, defaultValue);
    }

    protected string? GetOption(CommandLineArgs args, string optionName, string? defaultValue = null)
    {
        return args.Options.GetOrNull(optionName) ?? defaultValue;
    }

    protected bool HasOption(CommandLineArgs args, string optionName)
    {
        return !string.IsNullOrEmpty(args.Options.GetOrNull(optionName));
    }

    protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private class ErrorResponse
    {
        public string? Message { get; set; }
        public string? Code { get; set; }
        public object? Details { get; set; }
    }
}
