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
        
        var dockerRedisPort = configuration.GetValue<int>("DockerRedisConfig:port");
        
        var dockerEsPort = configuration.GetValue<int>("DockerEsConfig:port");

        var dockerQrPort = configuration.GetValue<int>("DockerQrConfig:port");
        var dockerKafkaPort = configuration.GetValue<int>("DockerKafkaConfig:port");

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
            .WithEnvironment("Redis__Config", redisUrl)
            .WithEnvironment("AuthServer__IssuerUri", "http://localhost:7001")
            .WithHttpEndpoint(port: 7001, name: "authserver-http");

            // .WithEnvironment("Elasticsearch__Url", esUrl)
            .WithEnvironment("AuthServer__Authority", "http://localhost:7001")
            // Configure Swagger as default page with auto-launch
            .WithEnvironment("SwaggerUI__RoutePrefix", "")
            .WithEnvironment("SwaggerUI__DefaultModelsExpandDepth", "-1")
            .WithHttpEndpoint(port: 7002, name: "httpapi-http");

// Add Aevatar.Developer.Host project with its dependencies

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
