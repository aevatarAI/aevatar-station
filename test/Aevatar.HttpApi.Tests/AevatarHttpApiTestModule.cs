// using Aevatar.CQRS.Handler;
// using Aevatar.WebHook.Deploy;
// using MediatR;
// using Microsoft.Extensions.DependencyInjection;
// using Volo.Abp.Authorization.Permissions;
// using Volo.Abp.AutoMapper;
// using Volo.Abp.EntityFrameworkCore;
// using Volo.Abp.EventBus;
// using Volo.Abp.Guids;
// using Volo.Abp.Identity;
// using Volo.Abp.Modularity;
// using Volo.Abp.MultiTenancy;
// using Volo.Abp.PermissionManagement;
// using Volo.Abp.SimpleStateChecking;
//
// namespace Aevatar;
//
// [DependsOn(
//     typeof(AevatarApplicationModule),
//     typeof(AbpEventBusModule),
//     typeof(AbpPermissionManagementDomainModule),
//     typeof(AbpIdentityDomainModule)
// )]
// public class AevatarHttpApiTestModule : AbpModule
// {
//     public override void ConfigureServices(ServiceConfigurationContext context)
//     {
//         base.ConfigureServices(context);
//         Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarApplicationModule>(); });
//         var configuration = context.Services.GetConfiguration();
//         // context.Services.AddSingleton<IElasticClient>(provider =>
//         // {
//         //     var settings =new ConnectionSettings(new Uri("http://127.0.0.1:9200"))
//         //         .DefaultIndex("cqrs").DefaultFieldNameInferrer(fieldName => 
//         //             char.ToLowerInvariant(fieldName[0]) + fieldName[1..]);
//         //     return new ElasticClient(settings);
//         // });
//        
//         context.Services.AddMediatR(typeof(GetStateQueryHandler).Assembly);
//         context.Services.AddMediatR(typeof(GetGEventQueryHandler).Assembly);
//         context.Services.AddTransient<IHostDeployManager, DefaultHostDeployManager>();
//
//         // Add Permission Management services
//         context.Services.AddAbpDbContext<PermissionManagementDbContext>(options =>
//         {
//             options.AddDefaultRepositories(true);
//         });
//
//         Configure<AbpDbContextOptions>(options =>
//         {
//             // options.UseInMemoryDatabase();
//         });
//
//         // context.Services.AddScoped<IPermissionGrantRepository, PermissionGrantRepository>();
//         context.Services.AddScoped<IPermissionManager, PermissionManager>();
//         // context.Services.AddScoped<IIdentityRoleRepository, IdentityRoleRepository>();
//         
//         // Add required services
//         context.Services.AddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();
//         context.Services.AddSingleton<ISimpleStateCheckerManager<PermissionDefinition>, SimpleStateCheckerManager<PermissionDefinition>>();
//         context.Services.AddSingleton<IGuidGenerator, SequentialGuidGenerator>();
//         context.Services.Configure<PermissionManagementOptions>(options => { });
//         context.Services.AddSingleton<ICurrentTenant, CurrentTenant>();
//         context.Services.AddDistributedMemoryCache();
//     }
// }