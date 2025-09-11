using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

/// <summary>
/// Enhanced Aevatar.Aspire Program.cs with MCP Gateway integration
/// Based on Microsoft MCP Gateway: https://github.com/microsoft/mcp-gateway
/// </summary>
public class Program
{
    public async static Task<int> Main(string[] args)
    {
        // Set required environment variables before creating the builder
        Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:14317");
        Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:14318");
        Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Get existing infrastructure ports
        var dockerMongoPort = configuration.GetValue<int>("DockerMongoConfig:port");
        var dockerRedisPort = configuration.GetValue<int>("DockerRedisConfig:port");
        var dockerEsPort = configuration.GetValue<int>("DockerEsConfig:port");
        var dockerQrPort = configuration.GetValue<int>("DockerQrConfig:port");
        var dockerKafkaPort = configuration.GetValue<int>("DockerKafkaConfig:port");

        // Get MCP Gateway configuration
        var mcpGatewayPort = configuration.GetValue<int>("MCPGatewayConfig:port", 7004);
        var mcpFilesystemPort = configuration.GetValue<int>("MCPServersConfig:filesystem:port", 3001);
        var mcpSqlitePort = configuration.GetValue<int>("MCPServersConfig:sqlite:port", 3002);

        var redisUrl = $"127.0.0.1:{dockerRedisPort}";

        var builder = DistributedApplication.CreateBuilder(args);

        // ===== INFRASTRUCTURE SERVICES =====
        
        // MongoDB for persistence
        var mongodb = builder.AddMongoDB("mongodb", dockerMongoPort)
            .WithContainerName("mongodb")
            .WithLifetime(ContainerLifetime.Persistent);

        // Redis for caching and session management
        var redis = builder.AddRedis("redis", dockerRedisPort)
            .WithContainerName("redis")
            .WithLifetime(ContainerLifetime.Persistent);

        // Elasticsearch for logging and search
        var elasticsearch = builder.AddElasticsearch("elasticsearch", dockerEsPort)
            .WithContainerName("elasticsearch")
            .WithLifetime(ContainerLifetime.Persistent);

        // ===== MCP GATEWAY INTEGRATION =====
        
        // Add MCP Server containers
        var mcpFilesystem = builder.AddContainer("mcp-filesystem", "localhost:5000/mcp-filesystem:1.0.0")
            .WithHttpEndpoint(port: mcpFilesystemPort, targetPort: 3000, name: "filesystem-mcp")
            .WithEnvironment("MCP_SERVER_NAME", "filesystem")
            .WithEnvironment("WORKSPACE_PATH", "/workspace")
            .WithBindMount("./workspace", "/workspace")
            .WithContainerName("mcp-filesystem");

        var mcpSqlite = builder.AddContainer("mcp-sqlite", "localhost:5000/mcp-sqlite:1.0.0")
            .WithHttpEndpoint(port: mcpSqlitePort, targetPort: 3000, name: "sqlite-mcp")
            .WithEnvironment("MCP_SERVER_NAME", "sqlite")
            .WithEnvironment("DATABASE_PATH", "/data/sqlite.db")
            .WithBindMount("./data/sqlite", "/data")
            .WithContainerName("mcp-sqlite");

        // Add MCP Gateway service
        var mcpGateway = builder.AddProject("mcp-gateway", "../Aevatar.MCPGateway/Aevatar.MCPGateway.csproj")
            .WithReference(mongodb)
            .WithReference(redis)
            .WithReference(mcpFilesystem)
            .WithReference(mcpSqlite)
            .WaitFor(mongodb)
            .WaitFor(redis)
            .WaitFor(mcpFilesystem)
            .WaitFor(mcpSqlite)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("Redis__Configuration", "{redis.connectionString}")
            // MCP Gateway specific configuration
            .WithEnvironment("MCPGateway__GatewayBaseUrl", "http://localhost:7004")
            .WithEnvironment("MCPGateway__EnableSessionAffinity", "true")
            .WithEnvironment("MCPGateway__RequestTimeout", "00:01:00")
            .WithEnvironment("MCPGateway__MaxRetryAttempts", "3")
            .WithEnvironment("MCPGateway__EnableDetailedLogging", "true")
            // Auto-register MCP servers
            .WithEnvironment("MCPGateway__AutoRegister__Filesystem__Url", "http://localhost:3001")
            .WithEnvironment("MCPGateway__AutoRegister__Sqlite__Url", "http://localhost:3002")
            .WithHttpEndpoint(port: mcpGatewayPort, name: "mcp-gateway-http");

        // ===== EXISTING AEVATAR SERVICES =====

        // Create Orleans silos with MCP Gateway awareness
        var developerSilo = CreateSilo(
            builder,
            projectName: "developerSilo", 
            siloNamePattern: "Developer",
            ip: "127.0.0.10",
            siloPort: 22222,
            gatewayPort: 40000,
            dashboardPort: 9090,
            healthCheckPort: 20084
        )
        .WithReference(mcpGateway)
        .WithEnvironment("MCPGateway__GatewayBaseUrl", "http://localhost:7004")
        .WithEnvironment("AevatarOrleans__ClusterId", "AevatarSiloClusterDeveloper")
        .WithEnvironment("AevatarOrleans__DataBase", "AevatarDbDeveloper");

        await Task.Delay(1000);

        // AuthServer with MCP Gateway support
        var authServer = builder.AddProject("authserver", "../Aevatar.AuthServer/Aevatar.AuthServer.csproj")
            .WithReference(mongodb)
            .WithReference(redis)
            .WaitFor(mongodb)
            .WaitFor(redis)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("Redis__Config", redisUrl)
            .WithEnvironment("AuthServer__IssuerUri", "http://localhost:7001")
            .WithHttpEndpoint(port: 7001, name: "authserver-http");

        // Core silos
        var siloScheduler = CreateSilo(
            builder,
            projectName: "siloScheduler", 
            siloNamePattern: "Scheduler",
            ip: "127.0.0.2",
            siloPort: 11111,
            gatewayPort: 30000,
            dashboardPort: 8080,
            healthCheckPort: 10081
        )
        .WithReference(mcpGateway)
        .WithEnvironment("MCPGateway__GatewayBaseUrl", "http://localhost:7004");

        await Task.Delay(1000);

        // HttpApi.Host with enhanced MCP Gateway integration
        var httpApiHost = builder.AddProject("httpapi", "../Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj")
            .WithReference(mongodb)
            .WithReference(elasticsearch)
            .WithReference(authServer)
            .WithReference(siloScheduler)
            .WithReference(mcpGateway)  // NEW: MCP Gateway reference
            .WaitFor(mongodb)
            .WaitFor(elasticsearch)
            .WaitFor(authServer)
            .WaitFor(siloScheduler)
            .WaitFor(mcpGateway)        // NEW: Wait for MCP Gateway
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("AevatarOrleans__MongoDBClient", "{mongodb.connectionString}")
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster")
            .WithEnvironment("AuthServer__Authority", "http://localhost:7001")
            // NEW: MCP Gateway configuration
            .WithEnvironment("MCPGateway__GatewayBaseUrl", "http://localhost:7004")
            .WithEnvironment("MCPGateway__AuthToken", "Bearer aspire-dev-token")
            .WithEnvironment("MCPGateway__EnableSessionAffinity", "true")
            .WithEnvironment("SwaggerUI__RoutePrefix", "")
            .WithHttpEndpoint(port: 7002, name: "httpapi-http");

        // Developer.Host with MCP Gateway integration
        var developerHost = builder
            .AddProject("developerhost", "../Aevatar.Developer.Host/Aevatar.Developer.Host.csproj")
            .WithReference(mongodb)
            .WithReference(elasticsearch)
            .WithReference(authServer)
            .WithReference(developerSilo)
            .WithReference(mcpGateway)  // NEW: MCP Gateway reference
            .WaitFor(mongodb)
            .WaitFor(elasticsearch)
            .WaitFor(authServer)
            .WaitFor(developerSilo)
            .WaitFor(mcpGateway)        // NEW: Wait for MCP Gateway
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("AevatarOrleans__MongoDBClient", "{mongodb.connectionString}")
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloClusterDeveloper")
            .WithEnvironment("Orleans__DataBase", "AevatarDbDeveloper")
            .WithEnvironment("AuthServer__Authority", "http://localhost:7001")
            // NEW: MCP Gateway configuration
            .WithEnvironment("MCPGateway__GatewayBaseUrl", "http://localhost:7004")
            .WithEnvironment("MCPGateway__AuthToken", "Bearer aspire-dev-token")
            .WithEnvironment("SwaggerUI__RoutePrefix", "")
            .WithHttpEndpoint(port: 7003, name: "developerhost-http");

        try
        {
            var app = builder.Build();

            Console.WriteLine("Starting Aevatar platform with MCP Gateway integration...");
            Console.WriteLine("Infrastructure: MongoDB, Redis, Elasticsearch");
            Console.WriteLine("MCP Gateway: http://localhost:7004");
            Console.WriteLine("MCP Servers: Filesystem (3001), SQLite (3002)");
            Console.WriteLine("Aevatar Services: Auth (7001), API (7002), Developer (7003)");

            // Enhanced startup timer with MCP Gateway URLs
            System.Timers.Timer launchTimer = new System.Timers.Timer(30000);
            launchTimer.Elapsed += (sender, e) =>
            {
                launchTimer.Stop();
                try
                {
                    Console.WriteLine("Opening service UIs in browser...");
                    
                    // Open main Aevatar services
                    OpenUrl("http://localhost:7002"); // HttpApi.Host
                    OpenUrl("http://localhost:7003"); // Developer.Host
                    
                    // Open MCP Gateway management UI (if available)
                    OpenUrl("http://localhost:7004/health"); // MCP Gateway health
                    
                    // Open Aspire Dashboard
                    OpenUrl("http://localhost:15000"); // Aspire Dashboard

                    RegisterClientAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to open browser: {ex.Message}");
                }
            };
            launchTimer.AutoReset = false;
            launchTimer.Start();

            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting application: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        return 0;
    }

    private static void OpenUrl(string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "open",
            Arguments = url,
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    // ... existing CreateSilo and RegisterClientAsync methods remain the same
}
