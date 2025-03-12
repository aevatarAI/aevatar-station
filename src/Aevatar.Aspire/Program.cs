using Aspire.Hosting;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Redis;
using Aspire.Hosting.Elasticsearch;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

// Set required environment variables before creating the builder
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:14317");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:14318");
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

var builder = DistributedApplication.CreateBuilder(args);

// Add infrastructure resources
var mongodb = builder.AddMongoDB("mongodb");
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
    .WithEnvironment("Orleans__ClusterId", "AevatarSiloCluster")
    .WithEnvironment("Orleans__ServiceId", "AevatarBasicService")
    .WithEnvironment("Orleans__AdvertisedIP", "127.0.0.1")
    .WithEnvironment("Orleans__GatewayPort", "30000")
    .WithEnvironment("Orleans__SiloPort", "11111")
    .WithEnvironment("Orleans__MongoDBClient", "{mongodb.connectionString}")
    .WithEnvironment("Orleans__DataBase", "AevatarDb")
    
    // Orleans Configuration settings - Fix for clustering provider
    .WithEnvironment("Orleans__Clustering__Provider", "MongoDB") // Explicitly set MongoDB as the clustering provider
    .WithEnvironment("Orleans__Clustering__ConnectionString", "{mongodb.connectionString}")
    .WithEnvironment("Orleans__Clustering__DatabaseName", "OrleansCluster")
    .WithEnvironment("Orleans__Storage__Provider", "MongoDB") // Explicitly set MongoDB as the storage provider
    .WithEnvironment("Orleans__Storage__ConnectionString", "{mongodb.connectionString}")
    .WithEnvironment("Orleans__Storage__DatabaseName", "OrleansStorage")
    .WithEnvironment("Orleans__StreamProvider__Kafka__BootstrapServers", "{kafka.bindings.kafka.host}:{kafka.bindings.kafka.port}")
    .WithEnvironment("Qdrant__Endpoint", "http://{qdrant.bindings.http.host}:{qdrant.bindings.http.port}")
    .WithEnvironment("OrleansStream__Provider", "Kafka") // Use Kafka provider for streaming
    .WithEnvironment("OrleansEventSourcing__Provider", "MongoDB") // Use MongoDB provider for event sourcing
    
    // Configure Orleans endpoints - fixed for proper TCP endpoint specification
    //.WithEndpoint(port: 11111, name: "silo")
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
    
    // Run the application
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error starting application: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
