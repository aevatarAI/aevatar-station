using System.Diagnostics;

// Set required environment variables before creating the builder
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:14317");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:14318");
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

var builder = DistributedApplication.CreateBuilder(args);

// Add infrastructure resources
var mongodb = builder.AddMongoDB("mongodb")
    .WithEnvironment("MONGO_INITDB_DATABASE", "AevatarDb"); // Ensure the default database exists
var redis = builder.AddRedis("redis");
var elasticsearch = builder.AddElasticsearch("elasticsearch");
var kafka = builder.AddKafka("kafka");

// Create data directory if it doesn't exist
Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "data", "qdrant"));

// Note: Qdrant doesn't have an Aspire provider yet, so we'll use a container directly
var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant:latest")
    .WithHttpEndpoint(port: 6333, name: "qdrant-http", targetPort: 6333)
    .WithEndpoint(port: 6334, name: "grpc", targetPort: 6334)
    // Use proper volume mounting for containers
    .WithBindMount(Path.Combine(Environment.CurrentDirectory, "data", "qdrant"), "/qdrant/storage");

// Create a dependency group for all infrastructure resources
// This ensures all these resources are fully started before any application components
var infrastructureDependencies = new[] { mongodb, redis, elasticsearch, kafka, qdrant };

// Add Aevatar.AuthServer project with its dependencies
var authServer = builder.AddProject("authserver", "../Aevatar.AuthServer/Aevatar.AuthServer.csproj")
    .WithReference(mongodb)
    .WithReference(redis)
    // Wait for all infrastructure components to be ready
    .WaitFor(mongodb)
    .WaitFor(redis)
    // Setting environment variables individually 
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
    .WithEnvironment("Redis__ConnectionString", "{redis.connectionString}")
    .WithHttpEndpoint(port: 7001, name: "authserver-http");

// Add Aevatar.HttpApi.Host project with its dependencies
var httpApiHost = builder.AddProject("httpapi", "../Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj")
    .WithReference(mongodb)
    .WithReference(elasticsearch)
    .WithReference(authServer)
    // Wait for dependencies
    .WaitFor(mongodb)
    .WaitFor(elasticsearch)
    .WaitFor(authServer)
    // Setting environment variables individually
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
    .WithEnvironment("Elasticsearch__Url", "http://{elasticsearch.bindings.es.host}:{elasticsearch.bindings.es.port}")
    .WithEnvironment("AuthServer__Authority", "{authserver.bindings.https.url}")
    // Configure Swagger as default page with auto-launch
    .WithEnvironment("SwaggerUI__RoutePrefix", "")
    .WithEnvironment("SwaggerUI__DefaultModelsExpandDepth", "-1")
    .WithHttpEndpoint(port: 7002, name: "httpapi-http");

// Add Aevatar.Developer.Host project with its dependencies
var developerHost = builder.AddProject("developerhost", "../Aevatar.Developer.Host/Aevatar.Developer.Host.csproj")
    .WithReference(mongodb)
    .WithReference(elasticsearch)
    .WithReference(authServer)
    // Wait for dependencies
    .WaitFor(mongodb)
    .WaitFor(elasticsearch)
    .WaitFor(authServer)
    // Setting environment variables individually
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
    .WithEnvironment("Elasticsearch__Url", "http://{elasticsearch.bindings.es.host}:{elasticsearch.bindings.es.port}")
    .WithEnvironment("AuthServer__Authority", "{authserver.bindings.https.url}")
    // Configure Swagger as default page with auto-launch
    .WithEnvironment("SwaggerUI__RoutePrefix", "")
    .WithEnvironment("SwaggerUI__DefaultModelsExpandDepth", "-1")
    .WithHttpEndpoint(port: 7003, name: "developerhost-http");

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
    .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
    .WithEnvironment("Elasticsearch__Url", "http://{elasticsearch.bindings.es.host}:{elasticsearch.bindings.es.port}")
    
    // Orleans Clustering configuration
    .WithEnvironment("AevatarOrleans__ClusterId", "AevatarSiloCluster")
    .WithEnvironment("AevatarOrleans__ServiceId", "AevatarBasicService")
    .WithEnvironment("AevatarOrleans__AdvertisedIP", "127.0.0.1")
    .WithEnvironment("AevatarOrleans__GatewayPort", "30000")
    .WithEnvironment("AevatarOrleans__SiloPort", "11111")
    .WithEnvironment("AevatarOrleans__MongoDBClient", "{mongodb.connectionString}")
    .WithEnvironment("AevatarOrleans__DataBase", "AevatarDb")
    
    // MongoDB provider configuration - Properly configured to work with Orleans
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
    
    // Configure Orleans endpoints - both silo and gateway are needed
    .WithEndpoint(port: 11111, name: "silo")
    .WithEndpoint(port: 30000, name: "gateway");

// Add Aevatar.Worker project with its dependencies
var worker = builder.AddProject("worker", "../Aevatar.Worker/Aevatar.Worker.csproj")
    .WithReference(mongodb)
    .WaitFor(mongodb)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}");

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
