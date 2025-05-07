using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Aevatar.Webhook.Dto;
using Aevatar.Webhook.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace Aevatar.Webhook;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        AddApplication<AevatarListenerHostModule>(services);
    }

    private void AddApplication<T>(IServiceCollection services) where T : IAbpModule
    {
        services.AddApplicationAsync<T>(options =>
        {
            var code = AsyncHelper.RunSync(async () => await GetPluginCodeAsync());
            options.PlugInSources.AddCode(code);
        });
    }
    
    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    // ReSharper disable once UnusedMember.Global
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var cultureInfo = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        app.InitializeApplication();
    }
    
    private async Task<byte[]> GetPluginCodeAsync()
    {
        var webhookId = _configuration["Webhook:WebhookId"];
        var version = _configuration["Webhook:Version"];
        var apiServiceUrl = _configuration["ApiHostUrl"];

        if (apiServiceUrl.IsNullOrEmpty())
        {
            throw new Exception("api host url config is missing!");
        }

        using (var httpClient = new HttpClient())
        {
            httpClient.BaseAddress = new Uri(apiServiceUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestUrl =
                $"api/webhook/code?webhookId={HttpUtility.UrlEncode(webhookId)}&version={HttpUtility.UrlEncode(version)}";
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var decodedBytes = Convert.FromBase64String(JsonConvert.DeserializeObject<ApiHostResponse>(responseBody)!.Data);
            return decodedBytes;
        }
    }
}