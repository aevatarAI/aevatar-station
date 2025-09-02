using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Speech;

public class AevatarGAgentsSpeechModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<SpeechOptions>(configuration.GetSection("Speech")); 
    }
}