// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using Volo.Abp.Authorization.Permissions;
// using Volo.Abp.BackgroundJobs;
// using Volo.Abp.Data;
// using Volo.Abp.Domain.Repositories;
// using Volo.Abp.EntityFrameworkCore;
// using Volo.Abp.Guids;
// using Volo.Abp.Identity;
// using Volo.Abp.Modularity;
// using Volo.Abp.MultiTenancy;
// using Volo.Abp.PermissionManagement;
// using Volo.Abp.PermissionManagement.EntityFrameworkCore;
// using Volo.Abp.PermissionManagement.Identity;
// using Volo.Abp.SimpleStateChecking;
// using Volo.Abp.Testing;
// using Volo.Abp.Uow;
//
// namespace Aevatar;
//
// [DependsOn(
//     typeof(AbpPermissionManagementDomainModule),
//     typeof(AbpPermissionManagementDomainIdentityModule),
//     typeof(AbpIdentityDomainModule),
//     typeof(AbpBackgroundJobsModule),
//     typeof(AbpPermissionManagementEntityFrameworkCoreModule)
// )]
// public class AevatarHttpApiTestModule : AbpModule
// {
//     public override void ConfigureServices(ServiceConfigurationContext context)
//     {
//         Configure<AbpBackgroundJobOptions>(options =>
//         {
//             options.IsJobExecutionEnabled = false; // 禁用后台作业执行
//         });
//
//         // context.Services.AddEntityFrameworkInMemoryDatabase();
//
//         Configure<AbpDbContextOptions>(options =>
//         {
//             options.Configure(config =>
//             {
//                 // config.UseInMemoryDatabase("TestDb");
//             });
//         });
//
//         Configure<AbpUnitOfWorkDefaultOptions>(options =>
//         {
//             options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled; // 禁用事务
//         });
//
//         // Add Permission Management services
//         context.Services.AddAbpDbContext<PermissionManagementDbContext>(options =>
//         {
//             options.AddDefaultRepositories(true);
//         });
//
//         // Add required services
//         context.Services.AddScoped<IPermissionManager, PermissionManager>();
//         context.Services.AddScoped<IIdentityRoleAppService, PermissionManager>();
//         // context.Services.AddScoped<IPermissionGrantRepository, PermissionGrantRepository>();
//         // context.Services.AddScoped<IPermissionGroupDefinitionRecordRepository, PermissionGroupDefinitionRecordRepository>();
//         // context.Services.AddScoped<IPermissionDefinitionRecordRepository, PermissionDefinitionRecordRepository>();
//         context.Services.AddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();
//         context.Services.AddSingleton<ISimpleStateCheckerManager<PermissionDefinition>, SimpleStateCheckerManager<PermissionDefinition>>();
//         context.Services.AddSingleton<IGuidGenerator, SequentialGuidGenerator>();
//         context.Services.Configure<PermissionManagementOptions>(options => { });
//         context.Services.AddSingleton<ICurrentTenant, CurrentTenant>();
//         context.Services.AddDistributedMemoryCache();
//
//         // Configure data seeding
//         Configure<AbpDataSeedOptions>(options =>
//         {
//             // options.IsSeedingEnabled = true;
//         });
//     }
// }