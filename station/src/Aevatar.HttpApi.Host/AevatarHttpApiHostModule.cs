using System;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using AElf.OpenTelemetry;
using Aevatar.ApiRequests;
using AutoResponseWrapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aevatar.MongoDB;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Microsoft.OpenApi.Models;
using Aevatar.Application.Grains;
using Aevatar.Domain.Grains;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Libs;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Aws;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Threading;
using Volo.Abp.VirtualFileSystem;
using Aevatar.Core.Interception.Extensions;

namespace Aevatar;

[DependsOn(
    typeof(AevatarHttpApiAdminModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpAutofacModule),
    typeof(AevatarApplicationModule),
    typeof(AevatarMongoDbModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(OpenTelemetryModule),
    typeof(AbpBlobStoringAwsModule)
)]
public class AevatarHttpApiHostModule : AIApplicationGrainsModule, IDomainGrainsModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IdentityBuilder>(builder => { builder.AddDefaultTokenProviders(); });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpMvcLibsOptions>(options => { options.CheckLibs = false; });
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        ConfigureAuthentication(context, configuration);
        ConfigureBundles();
        // ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureAutoResponseWrapper(context);
        ConfigureSwaggerServices(context, configuration);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        ConfigCache(context, configuration);
        ConfigureCors(context, configuration);
        //context.Services.AddDaprClient();

        context.Services.AddMvc(options => { options.Filters.Add(new IgnoreAntiforgeryTokenAttribute()); })
            .AddNewtonsoftJson();

        context.Services.AddHealthChecks();
        
        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.ConfigureDefault(container =>
            {
                var configSection = configuration.GetSection("AwsS3");
                container.UseAws(o =>
                {
                    o.AccessKeyId = configSection.GetValue<string>("AccessKeyId", "None");
                    o.SecretAccessKey = configSection.GetValue<string>("SecretAccessKey", "None");
                    o.Region = configSection.GetValue<string>("Region", "None");
                    o.ContainerName = configSection.GetValue<string>("ContainerName", "None");
                }); 
            });
        });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(configuration["App:CorsOrigins"]?
                        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray() ?? Array.Empty<string>())
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private void ConfigureDataProtection(
        ServiceConfigurationContext context,
        IConfiguration configuration,
        IWebHostEnvironment hostingEnvironment)
    {
        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("AevatarAuthServer");
    }

    private void ConfigCache(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var redisOptions = ConfigurationOptions.Parse(configuration["Redis:Configuration"]);
        context.Services.AddSingleton<IConnectionMultiplexer>(provider => ConnectionMultiplexer.Connect(redisOptions));
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "Aevatar:"; });
    }

    private static void ConfigureAutoResponseWrapper(ServiceConfigurationContext context)
    {
        context.Services.AddAutoResponseWrapper();
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["AuthServer:Authority"];
                options.RequireHttpsMetadata = Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
                options.Audience = "Aevatar";
                options.MapInboundClaims = false;
                
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async tokenValidatedContext  =>
                    {
                        var userId = tokenValidatedContext.Principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        var securityStamp = tokenValidatedContext.Principal.FindFirst(AevatarConsts.SecurityStampClaimType)
                            ?.Value;
                        if (!userId.IsNullOrWhiteSpace() && !securityStamp.IsNullOrWhiteSpace())
                        {
                            var userManager = tokenValidatedContext.HttpContext.RequestServices
                                .GetRequiredService<IdentityUserManager>();
                            var user = await userManager.FindByIdAsync(userId);
                            
                            if (user == null || user.SecurityStamp != securityStamp)
                            {
                                tokenValidatedContext.Fail("Token is no longer valid.");
                            }
                        }
                    },
                    OnMessageReceived = messageReceivedContext =>
                    {
                        var accessToken = messageReceivedContext.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            // Read the token out of the query string
                            messageReceivedContext.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle => { bundle.AddFiles("/global-styles.css"); }
            );
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            // Configure<AbpVirtualFileSystemOptions>(options =>
            // {
            //     options.FileSets.ReplaceEmbeddedByPhysical<AevatarDomainSharedModule>(
            //         Path.Combine(hostingEnvironment.ContentRootPath,
            //             $"..{Path.DirectorySeparatorChar}Aevatar.Domain.Shared"));
            //     options.FileSets.ReplaceEmbeddedByPhysical<AevatarDomainModule>(
            //         Path.Combine(hostingEnvironment.ContentRootPath,
            //             $"..{Path.DirectorySeparatorChar}Aevatar.Domain"));
            //     options.FileSets.ReplaceEmbeddedByPhysical<AevatarApplicationContractsModule>(
            //         Path.Combine(hostingEnvironment.ContentRootPath,
            //             $"..{Path.DirectorySeparatorChar}Aevatar.Application.Contracts"));
            //     options.FileSets.ReplaceEmbeddedByPhysical<AevatarApplicationModule>(
            //         Path.Combine(hostingEnvironment.ContentRootPath,
            //             $"..{Path.DirectorySeparatorChar}Aevatar.Application"));
            // });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(AevatarApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Aevatar API", Version = "v1" });
                // options.DocumentFilter<HideApisFilter>();
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Scheme = "bearer",
                    Description = "Specify the authorization token.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] { }
                    }
                });
            }
        );
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<ApiRequestStatisticsMiddleware>();
        app.UseTraceContext();
        // app.UsePathBase("/developer-client");
        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        app.UseSwagger();
        app.UseAbpSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aevatar API");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            c.OAuthScopes("Aevatar");
        });
        app.UseHealthChecks("/health");
        
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
        var statePermissionProvider = context.ServiceProvider.GetRequiredService<IStatePermissionProvider>();
        AsyncHelper.RunSync(async () => await statePermissionProvider.SaveAllStatePermissionAsync());
        
        
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<ApiRequestWorker>());
    }
}