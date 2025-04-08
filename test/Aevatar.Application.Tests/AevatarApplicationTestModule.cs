using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Handler;
using Aevatar.Kubernetes.Manager;
using Aevatar.Mock;
using Aevatar.Organizations;
using Aevatar.WebHook.Deploy;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Ingest;
using Elastic.Transport;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Core.Configuration;
using Moq;
using Volo.Abp.AutoMapper;
using Volo.Abp.Emailing;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.PermissionManagement;
using ChatConfigOptions = Aevatar.Options.ChatConfigOptions;

namespace Aevatar;

[DependsOn(
    typeof(AevatarApplicationModule),
    typeof(AbpEventBusModule),
    typeof(AevatarOrleansTestBaseModule),
    typeof(AevatarDomainTestModule)
)]
public class AevatarApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarApplicationModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<ChatConfigOptions>(configuration.GetSection("Chat"));
        context.Services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var response = TestableResponseFactory.CreateSuccessfulResponse<SearchResponse<Document>>(new(), 200);
            var mock = new Mock<ElasticsearchClient>();
            mock
                .Setup(m => m.SearchAsync<Document>(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            return mock.Object;
        });

        context.Services.AddTransient<IHostDeployManager, DefaultHostDeployManager>();

        context.Services.AddTransient<IHostDeployManager, DefaultHostDeployManager>();

        context.Services.AddSingleton<IEmailSender, NullEmailSender>();

        AddMock(context.Services);
    }

    private void AddMock(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IOrganizationPermissionChecker, MockOrganizationPermissionChecker>();
        
        serviceCollection.AddTransient<IRepository<OrganizationUnit, Guid>, MockOrganizationUnitRepository>();
        
        serviceCollection.AddTransient<IRepository<IdentityUser, Guid>>(provider => {
            var mock = new Mock<IRepository<IdentityUser, Guid>>();
            mock.Setup(x => x.GetListAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IdentityUser> { new IdentityUser(Guid.NewGuid(), "testuser", "test@example.com") });
            
            mock.Setup(x => x.GetListAsync(It.IsAny<Expression<Func<IdentityUser, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IdentityUser> { new IdentityUser(Guid.NewGuid(), "testuser", "test@example.com") });
                
            mock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<IdentityUser, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
                
            return mock.Object;
        });

        serviceCollection.AddTransient<IdentityUserManager>(provider => {
            var mock = new Mock<IdentityUserManager>(MockBehavior.Loose, null, null, null, null, null, null, null, null);
            
            mock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => new IdentityUser(Guid.NewGuid(), email.Split('@')[0], email));
            
            mock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => new IdentityUser(id, "testuser", "test@example.com"));
            
            mock.Setup(x => x.AddToOrganizationUnitAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
                
            mock.Setup(x => x.IsInRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(true);
                
            mock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(IdentityResult.Success);
                
            mock.Setup(x => x.UpdateAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(IdentityResult.Success);
                
            mock.Setup(x => x.GetUsersInOrganizationUnitAsync(It.IsAny<OrganizationUnit>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<IdentityUser> { new IdentityUser(Guid.NewGuid(), "testuser", "test@example.com") });
                
            mock.Setup(x => x.GetOrganizationUnitsAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(new List<OrganizationUnit>());
                
            return mock.Object;
        });
        
        serviceCollection.AddTransient<OrganizationUnitManager>(provider => {
            var mock = new Mock<OrganizationUnitManager>(MockBehavior.Loose, null, null, null);
            
            mock.Setup(x => x.CreateAsync(It.IsAny<OrganizationUnit>()))
                .ReturnsAsync((OrganizationUnit ou) => ou);
                
            mock.Setup(x => x.UpdateAsync(It.IsAny<OrganizationUnit>()))
                .ReturnsAsync((OrganizationUnit ou) => ou);
                
            mock.Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
                
            mock.Setup(x => x.FindChildrenAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<OrganizationUnit>());
                
            return mock.Object;
        });
        
        serviceCollection.AddTransient<IdentityRoleManager>(provider => {
            var mock = new Mock<IdentityRoleManager>(MockBehavior.Loose, null, null, null, null, null, null, null, null);
            
            mock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);
                
            mock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new IdentityRole(Guid.Parse(id), $"Role_{id}"));
                
            mock.Setup(x => x.DeleteAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);
                
            return mock.Object;
        });
        
        serviceCollection.AddTransient<IPermissionManager>(provider => {
            var mock = new Mock<IPermissionManager>();
            
            mock.Setup(x => x.SetForRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
                
            mock.Setup(x => x.GetAllForRoleAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<PermissionGrantInfo>
                {
                    new PermissionGrantInfo(AevatarPermissions.Organizations.Default, true),
                    new PermissionGrantInfo(AevatarPermissions.Organizations.Create, true),
                    new PermissionGrantInfo(AevatarPermissions.Organizations.Edit, true),
                    new PermissionGrantInfo(AevatarPermissions.Organizations.Delete, true),
                    new PermissionGrantInfo(AevatarPermissions.OrganizationMembers.Default, true),
                    new PermissionGrantInfo(AevatarPermissions.OrganizationMembers.Manage, true),
                    new PermissionGrantInfo(AevatarPermissions.ApiKeys.Default, true),
                    new PermissionGrantInfo(AevatarPermissions.ApiKeys.Create, true),
                    new PermissionGrantInfo(AevatarPermissions.ApiKeys.Edit, true),
                    new PermissionGrantInfo(AevatarPermissions.ApiKeys.Delete, true)
                });
                
            return mock.Object;
        });
        
        serviceCollection.AddTransient<IPermissionDefinitionManager>(provider => {
            var mock = new Mock<IPermissionDefinitionManager>();
            
            mock.Setup(x => x.GetGroupsAsync())
                .ReturnsAsync(new List<PermissionGroupDefinition>());
                
            return mock.Object;
        });
        
        serviceCollection.AddTransient<IDistributedEventBus>(provider => {
            var mock = new Mock<IDistributedEventBus>();
            mock.Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return mock.Object;
        });
    }
}