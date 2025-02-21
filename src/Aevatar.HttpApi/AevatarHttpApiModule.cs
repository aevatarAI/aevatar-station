﻿using Aevatar.Developer.Logger;
using Localization.Resources.AbpUi;
using Aevatar.Localization;
using Aevatar.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;

namespace Aevatar;

[DependsOn(
    typeof(AevatarApplicationContractsModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AevatarDeveloperLoggerModule)
    )]
public class AevatarHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AevatarResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}
