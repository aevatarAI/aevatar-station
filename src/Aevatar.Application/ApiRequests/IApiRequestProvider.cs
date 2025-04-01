using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Timing;

namespace Aevatar.ApiRequests;

public interface IApiRequestProvider
{
    Task IncreaseRequestAsync(string appId, DateTime dateTime);
    Task FlushAsync();
}

public class ApiRequestProvider : IApiRequestProvider, ISingletonDependency
{
    private readonly ApiRequestOptions _apiRequestOptions;

    private readonly ConcurrentDictionary<string, ApiRequestSegment> _apiRequests = new();

    public ApiRequestProvider(IOptionsSnapshot<ApiRequestOptions> apiRequestOptions)
    {
        _apiRequestOptions = apiRequestOptions.Value;
    }

    public Task IncreaseRequestAsync(string appId, DateTime dateTime)
    {
        var segmentTime = GetSegmentTime(dateTime);
        var key = GetApiRequestKey(appId, segmentTime);
        _apiRequests.AddOrUpdate(key, new ApiRequestSegment
        {
            SegmentTime = segmentTime,
            AppId = appId,
            Count = 1
        }, (s, i) =>
        {
            i.Count += 1;
            return i;
        });
        return Task.CompletedTask;
    }

    public async Task FlushAsync()
    {
        var segmentTime = GetSegmentTime(DateTime.UtcNow);
        foreach (var item in _apiRequests)
        {
            if (item.Value.SegmentTime >= segmentTime)
            {
                continue;
            }

            if (!_apiRequests.TryRemove(item.Key, out var value))
            {
                continue;
            }

            
        }
    }
    
    private string GetApiRequestKey(string appId, DateTime dateTime)
    {
        return $"{appId}-{dateTime}";
    }
    
    private DateTime GetSegmentTime(DateTime dateTime)
    {
        var minute = (dateTime.Minute / _apiRequestOptions.FlushPeriod) * _apiRequestOptions.FlushPeriod;
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minute, 0, DateTimeKind.Utc);
    }
}