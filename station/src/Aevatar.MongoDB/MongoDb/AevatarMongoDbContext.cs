using Aevatar.ApiKey;
using Aevatar.ApiRequests;
using Aevatar.Notification;
using Aevatar.Permissions;
using Aevatar.Plugins;
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
using Aevatar.Projects;

namespace Aevatar.MongoDB;

[ConnectionStringName("Default")]
public class AevatarMongoDbContext : AbpMongoDbContext
{
    /* Add mongo collections here. Example:
     * public IMongoCollection<Question> Questions => Collection<Question>();
     */
    public IMongoCollection<IdentityUserExtension> IdentityUserExtensionInfos { get; private set; }
    public IMongoCollection<StatePermission> StatePermissionInfos { get; private set; }
    public IMongoCollection<ApiRequestSnapshot> ApiRequestSnapshots { get; private set; }
    public IMongoCollection<Plugin> Plugins { get; private set; }
    public IMongoCollection<ProjectCorsOrigin> ProjectCorsOrigins { get; private set; }
    public IMongoCollection<ProjectDomain> ProjectDomains { get; private set; }

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
        modelBuilder.Entity<ApiRequestSnapshot>(b => b.CollectionName = "ApiRequestSnapshots");
        modelBuilder.Entity<Plugin>(b => b.CollectionName = "Plugins");
        modelBuilder.Entity<ProjectCorsOrigin>(b => b.CollectionName = "ProjectCorsOrigins");
        modelBuilder.Entity<ProjectDomain>(b => b.CollectionName = "ProjectDomains");
    }
}