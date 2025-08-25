using System.Reflection;
using System.Threading.Tasks;
using Aevatar.GAgents.Basic.Common;
using Volo.Abp;
using Volo.Abp.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;

namespace Aevatar.Service;

public interface IDocumentLinkService
{
    Task RefreshDocumentLinkStatusAsync();
    Task<bool> GetDocumentLinkStatusAsync(string documentLink);
    Task<IReadOnlyList<DocumentLinkPropertyInfo>> GetAllDocumentationLinkPropertiesAsync();
}

[RemoteService(IsEnabled = false)]
public class DocumentLinkService : ApplicationService, IDocumentLinkService
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private static readonly HttpClient s_fallbackClient = new HttpClient(new SocketsHttpHandler
    {
        AllowAutoRedirect = true
    })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    private volatile bool _hasAnyDocumentationLink;

    public DocumentLinkService(IHttpClientFactory? httpClientFactory = null)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task RefreshDocumentLinkStatusAsync()
    {
        var propertiesWithDocLinks = FindAllDocumentationLinkProperties();
        var count = propertiesWithDocLinks.Count();
        _hasAnyDocumentationLink = count > 0;
        Logger.LogInformation("Found {Count} properties with DocumentationLinkAttribute", count);
        await Task.CompletedTask;
    }

    public async Task<bool> GetDocumentLinkStatusAsync(string documentLink)
    {
        return await IsUrlReachableAsync(documentLink, TimeSpan.FromSeconds(5));
    }

    public Task<IReadOnlyList<DocumentLinkPropertyInfo>> GetAllDocumentationLinkPropertiesAsync()
    {
        var list = FindAllDocumentationLinkProperties()
            .Select(x => new DocumentLinkPropertyInfo
            {
                DeclaringTypeFullName = x.DeclaringType.FullName ?? x.DeclaringType.Name,
                PropertyName = x.Property.Name,
                DocumentationUrl = x.Attribute.DocumentationUrl
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<DocumentLinkPropertyInfo>>(list);
    }

    private async Task<bool> IsUrlReachableAsync(string url, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            Logger.LogWarning("Invalid documentation URL: {Url}", url);
            return false;
        }

        var client = _httpClientFactory?.CreateClient() ?? s_fallbackClient;

        // Try HEAD first (fast), then fallback to GET if HEAD not supported or fails
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var headOk = (int)headResponse.StatusCode >= 200 && (int)headResponse.StatusCode < 400;
            if (headOk)
            {
                Logger.LogDebug("HEAD succeeded for {Url} with status {Status}", url, (int)headResponse.StatusCode);
                return true;
            }
        }
        catch (HttpRequestException)
        {
            // fall through to GET
        }
        catch (TaskCanceledException)
        {
            // fall through to GET
        }

        try
        {
            using var cts = new CancellationTokenSource(timeout);
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            using var getResponse = await client.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var ok = (int)getResponse.StatusCode >= 200 && (int)getResponse.StatusCode < 400;
            Logger.LogInformation("GET check for {Url} status {Status} => {Ok}", url, (int)getResponse.StatusCode, ok);
            return ok;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to verify documentation URL: {Url}", url);
            return false;
        }
    }

    private static IEnumerable<(Type DeclaringType, PropertyInfo Property, DocumentationLinkAttribute Attribute)> FindAllDocumentationLinkProperties()
    {
        var result = new List<(Type DeclaringType, PropertyInfo Property, DocumentationLinkAttribute Attribute)>();

        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic);

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type == null)
                {
                    continue;
                }

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    var attribute = property.GetCustomAttributes<DocumentationLinkAttribute>(inherit: true).FirstOrDefault();
                    if (attribute != null)
                    {
                        result.Add((type, property, attribute));
                    }
                }
            }
        }

        return result;
    }
}