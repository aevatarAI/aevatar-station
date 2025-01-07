using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Application.Grains;
using Aevatar.Domain.Grains;
using Aevatar.MongoDB;
using Aevatar.Worker.Dapr;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Dapr.EventBus;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Dapr;
using Volo.Abp.Modularity;

namespace Aevatar.Worker;

[DependsOn(
    typeof(AbpBackgroundWorkersModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AevatarApplicationModule),
    typeof(AevatarApplicationContractsModule),
    typeof(AevatarMongoDbModule),
    typeof(AbpAutofacModule),
    typeof(AbpDaprModule),
    typeof(AbpAspNetCoreMvcDaprEventBusModule)
)]
public class AevatarWorkerModule : AIApplicationGrainsModule, IDomainGrainsModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHttpClient();
        context.Services.AddDaprClient();
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        // add your background workers here
        //await context.AddBackgroundWorkerAsync<AuthorSummaryWorker>();
        await context.AddBackgroundWorkerAsync<DaprTestWorker>();
    }
}