﻿using Aevatar.Permission;
using Aevatar.ApiKey;
using Aevatar.Notification;
using Aevatar.User;
using MongoDB.Driver;
using Volo.Abp.AuditLogging.MongoDB;
using Volo.Abp.BackgroundJobs.MongoDB;
using Volo.Abp.Data;
using Volo.Abp.FeatureManagement.MongoDB;
using Volo.Abp.Identity.MongoDB;
using Volo.Abp.MongoDB;
using Volo.Abp.OpenIddict.MongoDB;
using Volo.Abp.PermissionManagement.MongoDB;
using Volo.Abp.SettingManagement.MongoDB;

namespace Aevatar.MongoDB;

[ConnectionStringName("Default")]
public class AevatarMongoDbContext : AbpMongoDbContext
{
    /* Add mongo collections here. Example:
     * public IMongoCollection<Question> Questions => Collection<Question>();
     */
    public IMongoCollection<IdentityUserExtension> IdentityUserExtensionInfos { get; private set; }
    public IMongoCollection<StatePermission> StatePermissionInfos { get; private set; }


    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);
        modelBuilder.ConfigureOpenIddict();
        modelBuilder.ConfigurePermissionManagement();
        modelBuilder.ConfigureSettingManagement();
        modelBuilder.ConfigureBackgroundJobs();
        modelBuilder.ConfigureAuditLogging();
        modelBuilder.ConfigureIdentity();
        modelBuilder.ConfigureOpenIddict();
        modelBuilder.ConfigureFeatureManagement();

        //modelBuilder.Entity<YourEntity>(b =>
        //{
        //    //...
        //});
        modelBuilder.Entity<IdentityUserExtension>(b => { b.CollectionName = "IdentityUserExtensions"; });
        modelBuilder.Entity<ProjectAppIdInfo>(b => b.CollectionName = "ProjectAppInfoCollection");
        modelBuilder.Entity<NotificationInfo>(b => b.CollectionName = "NotificationInfoCollection");
    }
}