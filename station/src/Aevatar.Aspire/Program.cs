using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

/// Note: This is a sample program for setting up an Aspire application with multiple silos.
/// use 
/// `sudo ifconfig lo0 alias 127.0.0.2` 
/// `sudo ifconfig lo0 alias 127.0.0.3` 
/// to add multiple loopback IPs allowing 
/// multiple silos to run on the same machine
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
        // docker ps , get local port
        var dockerMongoPort = configuration.GetValue<int>("DockerMongoConfig:port");
        // var dockerMongoName = configuration.GetValue<string>("DockerMongoConfig:name");
        // var dockerMongoPassword = configuration.GetValue<string>("DockerMongoConfig:password");
        
        var dockerRedisPort = configuration.GetValue<int>("DockerRedisConfig:port");
        
        var dockerEsPort = configuration.GetValue<int>("DockerEsConfig:port");
        // var dockerEsName = configuration.GetValue<string>("DockerEsConfig:name");
        // var dockerEsPassword = configuration.GetValue<string>("DockerEsConfig:password");

        var dockerQrPort = configuration.GetValue<int>("DockerQrConfig:port");
        var dockerKafkaPort = configuration.GetValue<int>("DockerKafkaConfig:port");

        // var mongodbConnections =
        //     $"mongodb://{dockerMongoName}:{dockerMongoPassword}@localhost:{dockerMongoPort}/AevatarDb?authSource=admin&authMechanism=SCRAM-SHA-256";
        
        // var mongoDBClient = $"mongodb://{dockerMongoName}:{dockerMongoPassword}@localhost:{dockerMongoPort}?authSource=admin";

        // var esUrl = $"[\"http://{dockerEsName}:{dockerMongoPassword}@localhost:{dockerEsPort}\"]";
        // var qrUrl = $"http://127.0.0.1:{dockerQrPort}";
        // var kafkaUrl = $"127.0.0.1:{dockerKafkaPort}";
        var redisUrl = $"127.0.0.1:{dockerRedisPort}";

        var builder = DistributedApplication.CreateBuilder(args);
///can not match conatiner port with host ports correctly. Use `docker compose up -d` to start for now
/*
        // Add infrastructure resources
        var mongoUserName = builder
            .AddParameter("MONGOUSERNAME", dockerMongoName);
        var mongoPassword = builder
            .AddParameter("MONGOPASSWORD", dockerMongoPassword);

        var mongodb = builder.AddMongoDB("mongodb", dockerMongoPort, userName: mongoUserName, password: mongoPassword)
            .WithEnvironment("MONGO_INITDB_DATABASE", "AevatarDb"); // Ensure the default database exists
        mongodb.WithContainerName("mongodb");
        mongodb.WithLifetime(ContainerLifetime.Persistent);

        var redis = builder.AddRedis("redis", port: dockerRedisPort);
        redis.WithContainerName("redis");
        redis.WithLifetime(ContainerLifetime.Persistent);

        var elasticsearchPassword = builder
            .AddParameter("ESPASSWORD", dockerEsPassword);
        var elasticsearch = builder.AddElasticsearch("elasticsearch", password: elasticsearchPassword, port: dockerEsPort);
        elasticsearch.WithContainerName("elasticsearch");
        elasticsearch.WithLifetime(ContainerLifetime.Persistent);
        
        var kafka = builder.AddKafka("kafka", dockerKafkaPort);
        kafka.WithContainerName("kafka");
        kafka.WithLifetime(ContainerLifetime.Persistent);

        // Create data directory if it doesn't exist
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "data", "qdrant"));

        // Note: Qdrant doesn't have an Aspire provider yet, so we'll use a container directly
        var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant:latest")
            .WithHttpEndpoint(port: 6333, name: "qdrant-http", targetPort: 6333)
            .WithEndpoint(port: 6334, name: "grpc", targetPort: 6334)
            // Use proper volume mounting for containers
            .WithBindMount(Path.Combine(Environment.CurrentDirectory, "data", "qdrant"), "/qdrant/storage");
        qdrant.WithContainerName("qdrant");
        qdrant.WithLifetime(ContainerLifetime.Persistent);
    */

        // Create k3s data directory if it doesn't exist
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "data", "k3s"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "data", "k3s", "kubelet"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "data", "k3s", "kubeconfig"));
        
        // Add k3s container for sandbox code execution
        var k3s = builder.AddContainer("k3s", "rancher/k3s:v1.27.4-k3s1")
            .WithHttpEndpoint(port: 6443, name: "kubernetes-api", targetPort: 6443)
            .WithEndpoint(port: 8472, name: "flannel-vxlan", targetPort: 8472)
            .WithEndpoint(port: 10250, name: "kubelet", targetPort: 10250)
            // Mount volumes for k3s data
            .WithBindMount(Path.Combine(Environment.CurrentDirectory, "data", "k3s"), "/var/lib/rancher/k3s")
            .WithBindMount(Path.Combine(Environment.CurrentDirectory, "data", "k3s", "kubelet"), "/var/lib/kubelet")
            .WithBindMount(Path.Combine(Environment.CurrentDirectory, "data", "k3s", "kubeconfig"), "/output")
            // Set environment variables
            .WithEnvironment("K3S_KUBECONFIG_OUTPUT", "/output/kubeconfig.yaml")
            .WithEnvironment("K3S_KUBECONFIG_MODE", "666")
            .WithEnvironment("K3S_TOKEN", "aevatar-sandbox")
            .WithEnvironment("K3S_ARGS", "--disable traefik --disable servicelb");
        k3s.WithContainerName("aevatar-k3s");
        k3s.WithLifetime(ContainerLifetime.Persistent);

        // Create a dependency group for all infrastructure resources
        // This ensures all these resources are fully started before any application components
        // var infrastructureDependencies = new[] {mongodb, redis, elasticsearch, kafka, qdrant, k3s};

        // Add Aevatar.Silo (Orleans) project with its dependencies
        // Orleans requires specific configuration for clustering and streams
        // var silo = builder.AddProject("silo", "../Aevatar.Silo/Aevatar.Silo.csproj")
        //     .WithReference(mongodb)
        //     .WithReference(elasticsearch)
        //     .WithReference(kafka)
        //     // Wait for dependencies
        //     .WaitFor(mongodb)
        //     .WaitFor(elasticsearch)
        //     .WaitFor(kafka)
        //     .WaitFor(qdrant)
        //     // Configure the Orleans silo properly
        //     .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        //     // MongoDB connection string
        //     .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
        //     // .WithEnvironment("Elasticsearch__Url", esUrl)
        //     //
        //     // Orleans Clustering configuration
        //     .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster")
        //     .WithEnvironment("Orleans__ServiceId", "AevatarBasicService")
        //     .WithEnvironment("Orleans__AdvertisedIP", "127.0.0.1")
        //     .WithEnvironment("Orleans__GatewayPort", "30000")
        //     .WithEnvironment("Orleans__SiloPort", "11111")
        //     .WithEnvironment("Orleans__MongoDBClient", mongoDBClient)
        //     .WithEnvironment("Orleans__DataBase", "AevatarDb")
        //     .WithEnvironment("Orleans__DashboardPort", "8080")

        //     // MongoDB provider configuration - Properly configured to work with Orleans
        //     .WithEnvironment("Qdrant__Endpoint", qrUrl)
        //     // .WithEnvironment("OrleansStream__Provider", "Kafka")
        //     // .WithEnvironment("OrleansStream__Broker", kfakaUrl)
        //     .WithEnvironment("OrleansEventSourcing__Provider", "MongoDB");

// Add Aevatar.Developer.Silo (Orleans) project with its dependencies
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
        // Override cluster configuration for developer environment
        .WithEnvironment("AevatarOrleans__ClusterId", "AevatarSiloClusterDeveloper")
        .WithEnvironment("AevatarOrleans__DataBase", "AevatarDbDeveloper");

        await Task.Delay(1000); // Wait for 1 second to ensure the developer silo is up and running

// Add Aevatar.AuthServer project with its dependencies
        var authServer = builder.AddProject("authserver", "../Aevatar.AuthServer/Aevatar.AuthServer.csproj")
            // .WithReference(mongodb)
            // .WithReference(redis)
            // Wait for all infrastructure components to be ready
            // .WaitFor(mongodb)
            // .WaitFor(redis)
            // Setting environment variables individually 
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            // .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")

            .WithEnvironment("Redis__Config", redisUrl)
            .WithEnvironment("AuthServer__IssuerUri", "http://localhost:7001")
            .WithHttpEndpoint(port: 7001, name: "authserver-http");

        // Create core silos (non-User types)
        var siloScheduler = CreateSilo(
            builder,
            projectName: "siloScheduler", 
            siloNamePattern: "Scheduler",
            ip: "127.0.0.2",
            siloPort: 11111,
            gatewayPort: 30000,
            dashboardPort: 8080,
            healthCheckPort:10081
        );
        await Task.Delay(1000); // Wait for 1 second to ensure the silo is up and running

        var siloProject = CreateSilo(
            builder,
            projectName: "siloProject", 
            siloNamePattern: "Projector",
            ip: "127.0.0.3",
            siloPort: 11112,
            gatewayPort: 30001,
            dashboardPort: 8081,
            healthCheckPort:10082
        );
        await Task.Delay(1000); // Wait for 1 second to ensure the silo is up and running

        // Create User type silos in a loop
        var userSiloConfigs = new[]
        {
            new { Name = "siloUser1", IP = "127.0.0.4", SiloPort = 11113, GatewayPort = 30002, DashboardPort = 8082,HealthCheckPort = 10083 },
            // new { Name = "siloUser2", IP = "127.0.0.5", SiloPort = 11114, GatewayPort = 30003, DashboardPort = 8083 },
            // new { Name = "siloUser3", IP = "127.0.0.6", SiloPort = 11115, GatewayPort = 30004, DashboardPort = 8084 }
        };

        var userSilos = new List<IResourceBuilder<ProjectResource>>();
        foreach (var config in userSiloConfigs)
        {
            var userSilo = CreateSilo(
                builder,
                projectName: config.Name,
                siloNamePattern: "User",
                ip: config.IP,
                siloPort: config.SiloPort,
                gatewayPort: config.GatewayPort,
                dashboardPort: config.DashboardPort,
                healthCheckPort:config.HealthCheckPort
            );
            userSilos.Add(userSilo);
            await Task.Delay(1000); // Wait for 1 second to ensure the silo is up and running
        }

// Add Aevatar.HttpApi.Host project with its dependencies
        var httpApiHost = builder.AddProject("httpapi", "../Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj")
            // .WithReference(mongodb)
            // .WithReference(elasticsearch)
            // .WithReference(authServer)
            .WithReference(siloScheduler)
            // Wait for dependencies
            // .WaitFor(mongodb)
            // .WaitFor(elasticsearch)
            // .WaitFor(authServer)
            .WaitFor(siloScheduler)
            // Setting environment variables individually
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            // .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("AevatarOrleans__MongoDBClient", "{mongodb.connectionString}")
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster")
            // .WithEnvironment("Orleans__MongoDBClient", mongoDBClient)
            // .WithEnvironment("Elasticsearch__Url", esUrl)
            .WithEnvironment("AuthServer__Authority", "http://localhost:7001")
            // Configure Swagger as default page with auto-launch
            .WithEnvironment("SwaggerUI__RoutePrefix", "")
            .WithEnvironment("SwaggerUI__DefaultModelsExpandDepth", "-1")
            .WithHttpEndpoint(port: 7002, name: "httpapi-http");

// Add Aevatar.Developer.Host project with its dependencies
        var developerHost = builder
            .AddProject("developerhost", "../Aevatar.Developer.Host/Aevatar.Developer.Host.csproj")
            // .WithReference(mongodb)
            // .WithReference(elasticsearch)
            // .WithReference(authServer)
            .WithReference(developerSilo)
            // Wait for dependencies
            // .WaitFor(mongodb)
            // .WaitFor(elasticsearch)
            // .WaitFor(authServer)
            .WaitFor(developerSilo)
            // Setting environment variables individually
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            // .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("AevatarOrleans__MongoDBClient", "{mongodb.connectionString}")
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloClusterDeveloper")
            // .WithEnvironment("Orleans__MongoDBClient", mongoDBClient)
            .WithEnvironment("Orleans__DataBase", "AevatarDbDeveloper")
            // .WithEnvironment("Elasticsearch__Url", esUrl)
            .WithEnvironment("AuthServer__Authority", "http://localhost:7001")
            // Configure Swagger as default page with auto-launch
            .WithEnvironment("SwaggerUI__RoutePrefix", "")
            .WithEnvironment("SwaggerUI__DefaultModelsExpandDepth", "-1")
            .WithHttpEndpoint(port: 7003, name: "developerhost-http");

// Add Aevatar.Sandbox.HttpApi.Host project with its dependencies
        var sandboxHttpApiHost = builder.AddProject("sandboxhttpapi", "../Aevatar.Sandbox.HttpApi.Host/Aevatar.Sandbox.HttpApi.Host.csproj")
            .WithReference(siloScheduler)
            // Wait for dependencies
            .WaitFor(siloScheduler)
            .WaitFor(k3s)
            // Setting environment variables individually
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("AevatarOrleans__MongoDBClient", "{mongodb.connectionString}")
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster")
            .WithEnvironment("Orleans__ServiceId", "AevatarBasicService")
            .WithEnvironment("Orleans__DataBase", "AevatarDb")
            .WithEnvironment("Host__HostId", "Aevatar")
            .WithEnvironment("AuthServer__Authority", "http://localhost:7001")
            // Configure Kubernetes to use k3s
            .WithEnvironment("Kubernetes__InCluster", "false")
            .WithEnvironment("Kubernetes__KubeConfig", Path.Combine(Environment.CurrentDirectory, "data", "k3s", "kubeconfig", "kubeconfig.yaml"))
            .WithEnvironment("Kubernetes__Namespace", "sandbox")
            // Configure Swagger as default page
            .WithEnvironment("SwaggerUI__RoutePrefix", "")
            .WithEnvironment("SwaggerUI__DefaultModelsExpandDepth", "-1")
            .WithHttpEndpoint(port: 7004, name: "sandboxhttpapi-http");

// Add Aevatar.Worker project with its dependencies
        var worker = builder.AddProject("worker", "../Aevatar.Worker/Aevatar.Worker.csproj")
            // .WithReference(mongodb)
            .WithReference(siloScheduler)
            // .WaitFor(mongodb)
            .WaitFor(siloScheduler)
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            // .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster");
            // .WithEnvironment("Orleans__MongoDBClient", mongoDBClient);

        try
        {
            // Build the application
            var app = builder.Build();

            // Start infrastructure first
            Console.WriteLine("Starting infrastructure components...");
            // The infrastructure will start automatically due to the WaitFor dependencies

            // Give some time for infrastructure to initialize properly
            Console.WriteLine("Waiting for infrastructure to initialize completely...");
            Thread.Sleep(10000); // 10 seconds pause to give MongoDB and other services time to fully start

            // The rest of the app will auto-start based on the WaitFor dependencies
            Console.WriteLine("Starting application components...");

            // Initialize k3s for sandbox code execution
            Console.WriteLine("Initializing k3s for sandbox code execution...");
            try
            {
                // Make the script executable
                var chmodProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = "+x k3s-init.sh",
                    UseShellExecute = false,
                    WorkingDirectory = Environment.CurrentDirectory
                });
                if (chmodProcess != null)
                {
                    chmodProcess.WaitForExit();
                }

                // Run the initialization script
                Process.Start(new ProcessStartInfo
                {
                    FileName = "./k3s-init.sh",
                    UseShellExecute = false,
                    WorkingDirectory = Environment.CurrentDirectory
                });
                
                Console.WriteLine("K3s initialization started in background.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize k3s: {ex.Message}");
            }

            // Start a timer to open Swagger UIs after services are ready
            System.Timers.Timer launchTimer = new System.Timers.Timer(30000); // 30 seconds
            launchTimer.Elapsed += (sender, e) =>
            {
                launchTimer.Stop();
                try
                {
                    Console.WriteLine("Opening Swagger UIs in browser...");
                    var psi = new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = "http://localhost:7002",
                        UseShellExecute = true
                    };
                    Process.Start(psi);

                    psi.Arguments = "http://localhost:7003";
                    Process.Start(psi);
                    
                    psi.Arguments = "http://localhost:7004";
                    Process.Start(psi);
                    
                    // Register client
                    _ = RegisterClientAsync();
                    
                    // Run the test script for sandbox execution
                    try
                    {
                        // Make the script executable
                        var chmodProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = "+x test-sandbox.sh",
                            UseShellExecute = false,
                            WorkingDirectory = Environment.CurrentDirectory
                        });
                        if (chmodProcess != null)
                        {
                            chmodProcess.WaitForExit();
                        }

                        // Run the test script
                        Console.WriteLine("Running sandbox test script...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "./test-sandbox.sh",
                            UseShellExecute = true,
                            WorkingDirectory = Environment.CurrentDirectory
                        });
                    }
                    catch (Exception testEx)
                    {
                        Console.WriteLine($"Failed to run sandbox test: {testEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to open browser: {ex.Message}");
                }
            };
            launchTimer.AutoReset = false;
            launchTimer.Start();
            
            // Run the application
            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting application: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        return 0;
    }

    /// <summary>
    /// Helper method to create Orleans silos with consistent configuration
    /// </summary>
    private static IResourceBuilder<ProjectResource> CreateSilo(
        IDistributedApplicationBuilder builder,
        string projectName,
        string siloNamePattern,
        string ip,
        int siloPort,
        int gatewayPort,
        int dashboardPort,
        int healthCheckPort)
    {
        return builder.AddProject(projectName, "../Aevatar.Silo/Aevatar.Silo.csproj")
            // Configure the Orleans silo properly
            .WithEnvironment("UseEnvironmentVariables", "True")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            // MongoDB connection string
            .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("Elasticsearch__Url", "http://{elasticsearch.bindings.es.host}:{elasticsearch.bindings.es.port}")
            
            // Orleans Clustering configuration
            .WithEnvironment("AevatarOrleans__SILO_NAME_PATTERN", siloNamePattern)
            .WithEnvironment("AevatarOrleans__ClusterId", "AevatarSiloCluster")
            .WithEnvironment("AevatarOrleans__ServiceId", "AevatarBasicService")
            .WithEnvironment("AevatarOrleans__AdvertisedIP", ip)
            .WithEnvironment("AevatarOrleans__GatewayPort", gatewayPort.ToString())
            .WithEnvironment("AevatarOrleans__SiloPort", siloPort.ToString())
            .WithEnvironment("AevatarOrleans__DashboardIp", ip)
            .WithEnvironment("AevatarOrleans__DashboardPort", dashboardPort.ToString())
            .WithEnvironment("AevatarOrleans__MongoDBClient", "{mongodb.connectionString}")
            .WithEnvironment("AevatarOrleans__DataBase", "AevatarDb")
            
            // MongoDB provider configuration
            .WithEnvironment("AevatarOrleans__Providers__Clustering", "MongoDB")
            .WithEnvironment("AevatarOrleans__Providers__Storage", "MongoDB")
            .WithEnvironment("AevatarOrleans__Providers__Default", "MongoDB")
            .WithEnvironment("AevatarOrleans__Clustering__Provider", "MongoDB")
            .WithEnvironment("AevatarOrleans__Clustering__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("AevatarOrleans__Clustering__DatabaseName", "OrleansCluster")
            .WithEnvironment("AevatarOrleans__Storage__Provider", "MongoDB")
            .WithEnvironment("AevatarOrleans__Storage__Default__Provider", "MongoDB")
            .WithEnvironment("AevatarOrleans__Storage__ConnectionString", "{mongodb.connectionString}")
            .WithEnvironment("AevatarOrleans__Storage__DatabaseName", "OrleansStorage")
            .WithEnvironment("AevatarOrleans__StreamProvider__Kafka__BootstrapServers", "{kafka.bindings.kafka.host}:{kafka.bindings.kafka.port}")
            .WithEnvironment("Qdrant__Endpoint", "http://{qdrant.bindings.http.host}:{qdrant.bindings.http.port}")
            .WithEnvironment("AevatarOrleans__Stream__Provider", "Kafka")
            .WithEnvironment("AevatarOrleans__EventSourcing__Provider", "MongoDB")
            .WithEnvironment("HealthCheck__Port", healthCheckPort.ToString())

            // Configure Orleans endpoints - both silo and gateway are needed
            .WithEndpoint(port: siloPort, name: "silo")
            .WithEndpoint(port: gatewayPort, name: "gateway");
    }

    private static async Task RegisterClientAsync()
    {
        var requestUrl = "http://127.0.0.1:7001/connect/token";
        var formData = new Dictionary<string, string>
        {
            {"grant_type", "password"},
            {"client_id", "AevatarAuthServer"},
            {"username", "admin"},
            {"password", "1q2W3e*"},
            {"scope", "Aevatar"}
        };

        using (var client = new HttpClient())
        {
            var content = new FormUrlEncodedContent(formData);
            try
            {
                var response = await client.PostAsync(requestUrl, content);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
                Console.WriteLine($"connect/token response: {responseBody}");
                if (responseJson == null || !responseJson.TryGetValue("access_token", out var accessToken))
                {
                    return;
                }
                var registerClientUrl =
                    "http://localhost:7002/api/users/registerClient?clientId=Aevatar001&clientSecret=123456&corsUrls=s";
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken.ToString());
                response = await client.PostAsync(registerClientUrl, new StringContent(string.Empty));
                response.EnsureSuccessStatusCode();
                responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"registerClient response: {responseBody}");
                
                var clientFormData = new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", "Aevatar001"},
                    {"client_secret", "123456"},
                    {"scope", "Aevatar"}
                };
                var clientContent = new FormUrlEncodedContent(clientFormData);
                var clientResponse = await client.PostAsync(requestUrl, clientContent);
                clientResponse.EnsureSuccessStatusCode();
                var clientResponseBody = await clientResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"clientId connect/token response: {clientResponseBody}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Request error: " + ex.Message);
            }
        }
    }
}