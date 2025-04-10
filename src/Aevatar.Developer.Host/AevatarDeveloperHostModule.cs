using System;
using System.IO;
using System.Linq;
using AElf.OpenTelemetry;
using Aevatar.MongoDB;
using Aevatar.Options;
using Aevatar.Permissions;
using AutoResponseWrapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Threading;
using Volo.Abp.VirtualFileSystem;

namespace Aevatar.Developer.Host;

[DependsOn(
    typeof(AevatarHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AevatarApplicationModule),
    typeof(AevatarMongoDbModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(OpenTelemetryModule)
)]
public class AevatarDeveloperHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHealthChecks();
        context.Services.AddAutoResponseWrapper();
        var configuration = context.Services.GetConfiguration();
        ConfigureAuthentication(context, configuration);
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        Configure<GoogleLoginOptions>(configuration.GetSection("GoogleLogin"));
        context.Services.AddMvc(options =>
        {
            options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
        })
        .AddNewtonsoftJson();
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

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();
        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseEndpoints(endpoints => { endpoints.MapHealthChecks("/health"); });
        app.UseSwagger();
        app.UseAbpSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aevatar API");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            c.OAuthScopes("Aevatar");
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
        var statePermissionProvider = context.ServiceProvider.GetRequiredService<IStatePermissionProvider>();
        AsyncHelper.RunSync(async () => await statePermissionProvider.SaveAllStatePermissionAsync());
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
    }
}