using System.Reflection;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Plugins;

public class PluginGAgentManager : IPluginGAgentManager, ILifecycleParticipant<ISiloLifecycle>
{
    protected readonly ILogger<PluginGAgentManager> Logger;

    private readonly ApplicationPartManager _applicationPartManager;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly PluginGAgentLoadOptions _options;

    public PluginGAgentManager(ApplicationPartManager applicationPartManager, IGAgentFactory gAgentFactory,
        IOptions<PluginGAgentLoadOptions> options, ILogger<PluginGAgentManager> logger)
    {
        _applicationPartManager = applicationPartManager;
        _gAgentFactory = gAgentFactory;
        Logger = logger;
        _options = options.Value;
    }

    public async Task<Guid> AddPluginGAgentAsync(AddPluginGAgentDto addPluginGAgentDto)
    {
        if (addPluginGAgentDto.Code.Length == 0)
        {
            return Guid.Empty;
        }

        var pluginCode = await _gAgentFactory.GetGAgentAsync("pluginCodeStorage",
            initializeDto: new PluginCodeStorageInitializationEvent
            {
                Code = addPluginGAgentDto.Code
            });
        var pluginCodeGuid = pluginCode.GetPrimaryKey();
        var tenant = await _gAgentFactory.GetGAgentAsync<ITenantPluginCodeGAgent>(addPluginGAgentDto.TenantId);
        Logger.LogInformation($"About to plugin to tenant {addPluginGAgentDto.TenantId}.");
        await tenant.AddPluginAsync(pluginCodeGuid);
        Logger.LogInformation($"Added plugin to tenant {addPluginGAgentDto.TenantId}.");
        return pluginCodeGuid;
    }

    public async Task LoadPluginGAgentsAsync(Guid tenantId)
    {
        var tenant = await _gAgentFactory.GetGAgentAsync<ITenantPluginCodeGAgent>(tenantId);
        var tenantState = await tenant.GetStateAsync();
        if (tenantState.CodeStorageGuids.IsNullOrEmpty()) return;
        var pluginCodeStorageGuids = tenantState.CodeStorageGuids;
        foreach (var pluginCodeStorageGuid in pluginCodeStorageGuids)
        {
            var pluginCodeStorage =
                await _gAgentFactory.GetGAgentAsync<IPluginCodeStorageGAgent>(pluginCodeStorageGuid);
            var code = await pluginCodeStorage.GetPluginCodeAsync();
            var assembly = Assembly.Load(code);
            _applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        }
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(nameof(PluginGAgentManager), ServiceLifecycleStage.ApplicationServices, OnStart);
    }

    private async Task OnStart(CancellationToken cancellationToken)
    {
        await LoadPluginGAgentsAsync(_options.TenantId);
    }
}