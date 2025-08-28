using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Aevatar.Kubernetes.Manager;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Services;
using Aevatar.Sandbox.HttpApi.Host.Authentication;
using Aevatar.Sandbox.Kubernetes.Adapter;
using Aevatar.Sandbox.Kubernetes.Manager;
using Aevatar.Sandbox.Python.Services;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Libs;
using Volo.Abp.AspNetCore.Mvc.UI;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;

namespace Aevatar.Sandbox;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
)]
public class AevatarSandboxHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        ConfigureConventionalControllers();
        ConfigureAuthentication(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        ConfigureCors(context, configuration);
        ConfigureSandboxServices(context);
        
        // 不需要前端库检查，因为我们只提供API
        Configure<AbpMvcLibsOptions>(options => { options.CheckLibs = false; });

        context.Services.AddHealthChecks();
    }

    private void ConfigureSandboxServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        context.Services.AddSingleton<SandboxKubernetesManager>();
        context.Services.AddSingleton<ISandboxService, PythonSandboxService>();
        context.Services.AddSingleton<ISandboxKubernetesClientAdapter, SandboxKubernetesClientAdapter>();
        
        // PythonSandboxService will be configured through ISandboxService
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(AevatarSandboxHttpApiHostModule).Assembly);
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        // 完全禁用认证，仅用于开发环境
        context.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "NoAuth";
            options.DefaultChallengeScheme = "NoAuth";
        }).AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("NoAuth", options => { });
    }

    private void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var authority = configuration["AuthServer:Authority"];
        if (string.IsNullOrEmpty(authority))
            throw new ArgumentException("AuthServer:Authority configuration is required", nameof(configuration));

        context.Services.AddAbpSwaggerGenWithOAuth(
            authority,
            new Dictionary<string, string>
            {
                {"Sandbox", "Sandbox API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "Aevatar Sandbox API",
                    Version = "v1",
                    Description = "API for executing code in isolated sandbox environments",
                    Contact = new OpenApiContact
                    {
                        Name = "Aevatar Team",
                        Email = "support@aevatar.com",
                        Url = new Uri("https://aevatar.com")
                    }
                });
                
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                
                // Enable XML comments
                var xmlPath = Path.Combine(AppContext.BaseDirectory, "Aevatar.Sandbox.HttpApi.Host.xml");
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
                
                // Add security definitions and requirements
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                var corsOrigins = configuration["App:CorsOrigins"];
                if (string.IsNullOrEmpty(corsOrigins))
                    throw new ArgumentException("App:CorsOrigins configuration is required", nameof(configuration));

                builder
                    .WithOrigins(
                        corsOrigins
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
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
        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sandbox API");
            options.RoutePrefix = string.Empty; // Set Swagger UI at the root
            
            var configuration = context.GetConfiguration();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            options.OAuthScopes("Sandbox");
            
            // Customize Swagger UI
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.DefaultModelExpandDepth(2);
            options.DefaultModelsExpandDepth(1);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            
            // Add custom CSS
            options.InjectStylesheet("/swagger-ui/custom.css");
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseUnitOfWork();
        app.UseConfiguredEndpoints();
    }
}