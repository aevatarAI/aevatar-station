using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

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
        var dockerMongoName = configuration.GetValue<string>("DockerMongoConfig:name");
        var dockerMongoPassword = configuration.GetValue<string>("DockerMongoConfig:password");
        
        var dockerRedisPort = configuration.GetValue<int>("DockerRedisConfig:port");
        
        var dockerEsPort = configuration.GetValue<int>("DockerEsConfig:port");
        var dockerEsName = configuration.GetValue<string>("DockerEsConfig:name");
        var dockerEsPassword = configuration.GetValue<string>("DockerEsConfig:password");

        var dockerQrPort = configuration.GetValue<int>("DockerQrConfig:port");
        var dockerKafkaPort = configuration.GetValue<int>("DockerKafkaConfig:port");

        var mongodbConnections =
            $"mongodb://{dockerMongoName}:{dockerMongoPassword}@localhost:{dockerMongoPort}/AevatarDb?authSource=admin&authMechanism=SCRAM-SHA-256";
        var mongoDBClient = $"mongodb://{dockerMongoName}:{dockerMongoPassword}@localhost:{dockerMongoPort}?authSource=admin";

        var esUrl = $"[\"http://{dockerEsName}:{dockerEsPassword}@localhost:{dockerEsPort}\"]";
        var qrUrl = $"http://127.0.0.1:{dockerQrPort}";
        var kafkaUrl = $"127.0.0.1:{dockerKafkaPort}";
        var redisUrl = $"127.0.0.1:{dockerRedisPort}";

        var builder = DistributedApplication.CreateBuilder(args);

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

        // Create a dependency group for all infrastructure resources
        // This ensures all these resources are fully started before any application components
        var infrastructureDependencies = new[] {mongodb, redis, elasticsearch, kafka, qdrant};

        // Add Aevatar.Silo (Orleans) project with its dependencies
        // Orleans requires specific configuration for clustering and streams
        var silo = builder.AddProject("silo", "../Aevatar.Silo/Aevatar.Silo.csproj")
            .WithReference(mongodb)
            .WithReference(elasticsearch)
            .WithReference(kafka)
            // Wait for dependencies
            .WaitFor(mongodb)
            .WaitFor(elasticsearch)
            .WaitFor(kafka)
            .WaitFor(qdrant)
            // Configure the Orleans silo properly
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            // MongoDB connection string
            .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            // .WithEnvironment("Elasticsearch__Url", esUrl)
            //
            // Orleans Clustering configuration
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster")
            .WithEnvironment("Orleans__ServiceId", "AevatarBasicService")
            .WithEnvironment("Orleans__AdvertisedIP", "127.0.0.1")
            .WithEnvironment("Orleans__GatewayPort", "30000")
            .WithEnvironment("Orleans__SiloPort", "11111")
            .WithEnvironment("Orleans__MongoDBClient", mongoDBClient)
            .WithEnvironment("Orleans__DataBase", "AevatarDb")
            .WithEnvironment("Orleans__DashboardPort", "8080")

            // MongoDB provider configuration - Properly configured to work with Orleans
            .WithEnvironment("Qdrant__Endpoint", qrUrl)
            // .WithEnvironment("OrleansStream__Provider", "Kafka")
            // .WithEnvironment("OrleansStream__Broker", kfakaUrl)
            .WithEnvironment("OrleansEventSourcing__Provider", "MongoDB");

// Add Aevatar.Developer.Silo (Orleans) project with its dependencies
// Orleans requires specific configuration for clustering and streams
        var developerSilo = builder.AddProject("developerSilo", "../Aevatar.Silo/Aevatar.Silo.csproj")
            .WithReference(mongodb)
            .WithReference(elasticsearch)
            .WithReference(kafka)
            // Wait for dependencies
            .WaitFor(mongodb)
            .WaitFor(elasticsearch)
            .WaitFor(kafka)
            .WaitFor(qdrant)
            // Configure the Orleans silo properly
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            // MongoDB connection string
            .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("Elasticsearch__Url", esUrl)

            // Orleans Clustering configuration
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloClusterDeveloper")
            .WithEnvironment("Orleans__ServiceId", "AevatarBasicService")
            .WithEnvironment("Orleans__AdvertisedIP", "127.0.0.1")
            .WithEnvironment("Orleans__GatewayPort", "40000")
            .WithEnvironment("Orleans__SiloPort", "22222")
            .WithEnvironment("Orleans__MongoDBClient", mongoDBClient)
            .WithEnvironment("Orleans__DataBase", "AevatarDbDeveloper")
            .WithEnvironment("Orleans__DashboardPort", "8081")

            // MongoDB provider configuration - Properly configured to work with Orleans
            .WithEnvironment("Qdrant__Endpoint", qrUrl)
            // .WithEnvironment("OrleansStream__Provider", "Kafka")
            // .WithEnvironment("OrleansStream__Broker", kfakaUrl)
            .WithEnvironment("OrleansEventSourcing__Provider", "MongoDB");

// Add Aevatar.AuthServer project with its dependencies
        var authServer = builder.AddProject("authserver", "../Aevatar.AuthServer/Aevatar.AuthServer.csproj")
            .WithReference(mongodb)
            .WithReference(redis)
            // Wait for all infrastructure components to be ready
            .WaitFor(mongodb)
            .WaitFor(redis)
            // Setting environment variables individually 
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("Redis__Config", redisUrl)
            .WithEnvironment("AuthServer__IssuerUri", "http://localhost:7001")
            .WithHttpEndpoint(port: 7001, name: "authserver-http");

// Add Aevatar.HttpApi.Host project with its dependencies
        var httpApiHost = builder.AddProject("httpapi", "../Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj")
            .WithReference(mongodb)
            .WithReference(elasticsearch)
            .WithReference(authServer)
            .WithReference(silo)
            // Wait for dependencies
            .WaitFor(mongodb)
            .WaitFor(elasticsearch)
            .WaitFor(authServer)
            .WaitFor(silo)
            // Setting environment variables individually
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster")
            .WithEnvironment("Orleans__MongoDBClient", mongoDBClient)
            // .WithEnvironment("Elasticsearch__Url", esUrl)
            .WithEnvironment("AuthServer__Authority", "http://localhost:7001")
            // Configure Swagger as default page with auto-launch
            .WithEnvironment("SwaggerUI__RoutePrefix", "")
            .WithEnvironment("SwaggerUI__DefaultModelsExpandDepth", "-1")
            .WithHttpEndpoint(port: 7002, name: "httpapi-http");

// Add Aevatar.Developer.Host project with its dependencies
        var developerHost = builder
            .AddProject("developerhost", "../Aevatar.Developer.Host/Aevatar.Developer.Host.csproj")
            .WithReference(mongodb)
            .WithReference(elasticsearch)
            .WithReference(authServer)
            .WithReference(developerSilo)
            // Wait for dependencies
            .WaitFor(mongodb)
            .WaitFor(elasticsearch)
            .WaitFor(authServer)
            .WaitFor(developerSilo)
            // Setting environment variables individually
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloClusterDeveloper")
            .WithEnvironment("Orleans__MongoDBClient", mongoDBClient)
            .WithEnvironment("Orleans__DataBase", "AevatarDbDeveloper")
            .WithEnvironment("Elasticsearch__Url", esUrl)
            .WithEnvironment("AuthServer__Authority", "http://localhost:7001")
            // Configure Swagger as default page with auto-launch
            .WithEnvironment("SwaggerUI__RoutePrefix", "")
            .WithEnvironment("SwaggerUI__DefaultModelsExpandDepth", "-1")
            .WithHttpEndpoint(port: 7003, name: "developerhost-http");

// Add Aevatar.Worker project with its dependencies
        var worker = builder.AddProject("worker", "../Aevatar.Worker/Aevatar.Worker.csproj")
            .WithReference(mongodb)
            .WithReference(silo)
            .WaitFor(mongodb)
            .WaitFor(silo)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("ConnectionStrings__Default", mongodbConnections)
            .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster")
            .WithEnvironment("Orleans__MongoDBClient", mongoDBClient);

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

            // Start a timer to open Swagger UIs after services are ready
            System.Timers.Timer launchTimer = new System.Timers.Timer(30000); // 20 seconds
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
                    RegisterClientAsync();
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
                var responseJson = JsonConvert.DeserializeObject<Dictionary<string, Object>>(responseBody);
                Console.WriteLine($"connect/token response: {responseBody}");
                if (!responseJson.TryGetValue("access_token", out var accessToken))
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
                response = await client.PostAsync(registerClientUrl, new StringContent(String.Empty));
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
                response.EnsureSuccessStatusCode();
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
