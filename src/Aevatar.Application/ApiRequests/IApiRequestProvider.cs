using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
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
    private readonly IApiRequestSnapshotRepository _apiRequestSnapshotRepository;
    private readonly IProjectAppIdRepository _projectAppIdRepository;
    private readonly IRepository<OrganizationUnit, Guid> _organizationUnitRepository;

    private readonly ConcurrentDictionary<string, ApiRequestSegment> _apiRequests = new();

    public ApiRequestProvider(IOptionsSnapshot<ApiRequestOptions> apiRequestOptions,
        IApiRequestSnapshotRepository apiRequestSnapshotRepository, IProjectAppIdRepository projectAppIdRepository,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository)
    {
        _apiRequestSnapshotRepository = apiRequestSnapshotRepository;
        _projectAppIdRepository = projectAppIdRepository;
        _organizationUnitRepository = organizationUnitRepository;
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

            if (!_apiRequests.TryRemove(item.Key, out _))
            {
                continue;
            }

            var app = await _projectAppIdRepository.FindAsync(o => o.AppId == item.Value.AppId);
            if (app == null)
            {
                continue;
            }

            var time = item.Value.SegmentTime.Date.AddHours(item.Value.SegmentTime.Hour);
            await UpdateSnapshotAsync(app.ProjectId, time, item.Value.Count);
            
            var organization = await _organizationUnitRepository.GetAsync(app.ProjectId);
            await UpdateSnapshotAsync(organization.ParentId.Value, time, item.Value.Count);
        }
    }

    private async Task UpdateSnapshotAsync(Guid organizationId, DateTime time, long count)
    {
        var snapshot =
            await _apiRequestSnapshotRepository.FindAsync(o =>
                o.OrganizationId == organizationId && o.Time == time);
        if (snapshot == null)
        {
            snapshot = new ApiRequestSnapshot(Guid.NewGuid())
            {
                Count = count,
                Time = time,
                OrganizationId = organizationId
            };
            await _apiRequestSnapshotRepository.InsertAsync(snapshot);
        }
        else
        {
            snapshot.Count += count;
            await _apiRequestSnapshotRepository.UpdateAsync(snapshot);
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