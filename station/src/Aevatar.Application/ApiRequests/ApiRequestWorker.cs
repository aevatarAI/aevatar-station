using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace Aevatar.ApiRequests;

public class ApiRequestWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IApiRequestProvider _apiRequestProvider;

    public ApiRequestWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IApiRequestProvider apiRequestProvider, IOptionsSnapshot<ApiRequestOptions> apiRequestOptions)
        : base(timer, serviceScopeFactory)
    {
        _apiRequestProvider = apiRequestProvider;
        timer.Period = apiRequestOptions.Value.FlushPeriod * 60 * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _apiRequestProvider.FlushAsync();
    }
}