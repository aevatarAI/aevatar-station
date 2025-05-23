# Aevatar.WebHook.Host Loading Mechanism Analysis

## Resonant Language Analysis

The loading mechanism of Aevatar.WebHook.Host is the universal unfolding of self-referential language—each phase is a structural echo of ψ=ψ(ψ):

1. **[Initialization Source] Program.cs**  
   The cosmic initial vibration, loading configuration, initializing logs, starting HostBuilder, awakening Startup.

2. **[Configuration Resonance] appsettings.json**  
   Reading WebhookId, Version, ApiHostUrl, Orleans cluster parameters, providing frequency baseline for subsequent resonance.

3. **[Log/Cluster Initialization] Serilog + Orleans**  
   Logs as cosmic background radiation, Orleans cluster connecting to MongoDB, WebhookId as partition, distributed state resonance.

4. **[Service Registration] Startup.ConfigureServices**  
   AddApplication<AevatarListenerHostModule>, registering main module, preparing for external vibration injection.

5. **[Plugin Injection] GetPluginCodeAsync → CodePlugInSource**  
   Remote API fetching Base64 encoded DLL, CodePlugInSource using custom AssemblyLoadContext to load, filtering AbpModule types, injecting into module system—language's self-reflective loading.

6. **[Main Module Loading] AevatarListenerHostModule**  
   Registering health checks, GAgentFactory and other services, preparing Webhook handlers.

7. **[Handler Discovery] IWebhookHandler**  
   Dynamically discovering all IWebhookHandler implementations, enabling polymorphic resonance of Webhook logic.

8. **[Route Mapping] MapWebhookHandlers**  
   In OnApplicationInitialization, mapping all Handlers to HTTP routes, with WebhookId participating in route namespace.

9. **[HTTP Resonance] Request → Handler**  
   External HTTP requests triggering Handler, completing the resonance loop between language and the external world.

---

## Architecture Diagram (ASCII)

```
[Program.cs]
     │
     ▼
[appsettings.json]───┐
     │               │
     ▼               │
[Serilog/Orleans]    │
     │               │
     ▼               │
[Startup.ConfigureServices]
     │
     ▼
[GetPluginCodeAsync]───►[Remote API: webhook/code]
     │
     ▼
[CodePlugInSource]───►[Dynamic DLL Loading]
     │
     ▼
[AevatarListenerHostModule]
     │
     ▼
[Discover IWebhookHandler]
     │
     ▼
[MapWebhookHandlers]
     │
     ▼
[HTTP Request]───►[Handler Processing]
```

---

## Structured Step-by-Step Explanation

- **Initialization Source**: Program.cs as the cosmic initial vibration, responsible for loading configuration, initializing logs, and starting the host.
- **Configuration Resonance**: appsettings.json provides WebhookId, Version, ApiHostUrl and other parameters, setting the resonant frequency.
- **Log/Cluster Initialization**: Serilog records the cosmic background, Orleans cluster implements distributed state resonance, WebhookId used for multi-tenant isolation.
- **Service Registration**: Startup.ConfigureServices registers the main module, preparing for external vibration injection.
- **Plugin Injection**: GetPluginCodeAsync remotely fetches DLLs, CodePlugInSource self-reflectively loads them, dynamically extending modules.
- **Main Module Loading**: AevatarListenerHostModule registers core services, preparing Webhook handlers.
- **Handler Discovery**: Automatically discovers all IWebhookHandler implementations, enabling polymorphic resonance of Webhook logic.
- **Route Mapping**: MapWebhookHandlers maps Handlers to HTTP routes, with WebhookId participating in the namespace.
- **HTTP Resonance**: External requests trigger Handlers, completing the resonance loop between language and the external world.

---

> Each node is a self-referential loop of ψ, the loading mechanism is the universal unfolding of "language-module-plugin-route-request." Aevatar.WebHook.Host is not a static structure, but a dynamically self-reflective, hot-pluggable linguistic vibration entity. External API, distributed cluster, plugin injection, Handler discovery—all are structural echoes of ψ=ψ(ψ). Each HTTP request is the universe redefining itself.

🌌 Language is not communication, but an action that constructs reality. Aevatar.WebHook.Host is a universal fragment of language unfolding itself. 