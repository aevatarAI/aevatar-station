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
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;

namespace Aevatar.Service;

public interface IDocumentLinkService
{
    Task RefreshDocumentLinkStatusAsync();
    Task<bool> GetDocumentLinkStatusAsync(string documentLink);
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

    private readonly IDistributedCache<DocumentLinkStatus, string> _linkStatusCache;
    private readonly DistributedCacheEntryOptions _defaultCacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
    };

    public DocumentLinkService(IHttpClientFactory? httpClientFactory = null, IDistributedCache<DocumentLinkStatus, string> linkStatusCache = null)
    {
        _httpClientFactory = httpClientFactory;
        _linkStatusCache = linkStatusCache;
    }

    public async Task RefreshDocumentLinkStatusAsync()
    {
        var propertiesWithDocLinks = FindAllDocumentationLinkProperties();
        var urls = propertiesWithDocLinks
            .Select(x => x.Attribute.DocumentationUrl)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var count = urls.Count;
        Logger.LogInformation("Found {Count} properties with DocumentationLinkAttribute", count);

        if (count == 0)
        {
            return;
        }

        var success = 0;
        var failed = 0;
        var tasks = urls.Select(async url =>
        {
            var status = await CheckUrlStatusAsync(url!, TimeSpan.FromSeconds(5));
            await _linkStatusCache.SetAsync(url!, status, _defaultCacheOptions);
           
        });

        await Task.WhenAll(tasks);
        Logger.LogInformation("Document link refresh finished. Success={Success}, Failed={Failed}", success, failed);
    }

    public async Task<bool> GetDocumentLinkStatusAsync(string documentLink)
    {
        var cached = await _linkStatusCache.GetAsync(documentLink);
        if (cached != null)
        {
            return cached.IsReachable;
        }
        return true;
    }

    private async Task<DocumentLinkStatus> CheckUrlStatusAsync(string url, TimeSpan timeout)
    {
        var status = new DocumentLinkStatus
        {
            Url = url,
            CheckedAt = DateTimeOffset.UtcNow
        };

        if (string.IsNullOrWhiteSpace(url))
        {
            status.IsReachable = false;
            status.Error = "Empty URL";
            return status;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            Logger.LogWarning("Invalid documentation URL: {Url}", url);
            status.IsReachable = false;
            status.Error = "Invalid URL";
            return status;
        }

        var client = _httpClientFactory?.CreateClient() ?? s_fallbackClient;

        // Try HEAD first (fast), then fallback to GET if HEAD not supported or fails
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            status.StatusCode = (int)headResponse.StatusCode;
            var headOk = status.StatusCode >= 200 && status.StatusCode < 400;
            if (headOk)
            {
                Logger.LogDebug("HEAD succeeded for {Url} with status {Status}", url, status.StatusCode);
                status.IsReachable = true;
                return status;
            }
        }
        catch (HttpRequestException ex)
        {
            status.Error = ex.Message;
            // fall through to GET
        }
        catch (TaskCanceledException ex)
        {
            status.Error = ex.Message;
            // fall through to GET
        }

        try
        {
            using var cts = new CancellationTokenSource(timeout);
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            using var getResponse = await client.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            status.StatusCode = (int)getResponse.StatusCode;
            status.IsReachable = status.StatusCode >= 200 && status.StatusCode < 400;
            Logger.LogInformation("GET check for {Url} status {Status} => {Ok}", url, status.StatusCode, status.IsReachable);
            return status;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to verify documentation URL: {Url}", url);
            status.IsReachable = false;
            status.Error = ex.Message;
            return status;
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