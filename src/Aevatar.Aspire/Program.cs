/*using Aspire.Hosting;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Redis;

var builder = DistributedApplication.CreateBuilder(args);

// Add shared resources
var mongo = builder.AddMongoDB("mongodb");
var redis = builder.AddRedis("redis");
var kafka = builder.AddKafka("kafka");

// Add Elastic Search as a container (not natively provided by Aspire.Hosting)
var elasticsearch = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch:7.17.0")
    .WithEnvironment("discovery.type", "single-node")
    .WithHttpEndpoint(port: 9200, name: "http");

// Add Qdrant as a container (not natively provided by Aspire.Hosting)
var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant:latest")
    .WithHttpEndpoint(port: 6333, name: "http");

// Add projects with their dependencies
var authServer = builder.AddProject<Projects.Aevatar_AuthServer>("authserver")
    .WithReference(mongo)
    .WithReference(redis);

var developerHost = builder.AddProject<Projects.Aevatar_Developer_Host>("developerhost")
    .WithReference(mongo)
    .WithReference(elasticsearch)
    .WithReference(authServer);

var httpApiHost = builder.AddProject<Projects.Aevatar_HttpApi_Host>("httpapihost")
    .WithReference(mongo)
    .WithReference(elasticsearch)
    .WithReference(authServer);

var silo = builder.AddProject<Projects.Aevatar_Silo>("silo")
    .WithReference(kafka)
    .WithReference(mongo)
    .WithReference(elasticsearch)
    .WithReference(qdrant);

var worker = builder.AddProject<Projects.Aevatar_Worker>("worker")
    .WithReference(mongo);

// Build and run the application
builder.Build().Run();*/

using Aspire.Hosting;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Redis;

var builder = DistributedApplication.CreateBuilder(args);

// Configure MongoDB
var mongoServer = builder.AddMongoDB("mongodb")
    .WithEndpoint(port: 27017);
    // .WithVolumeMount("mongodb-data", "/data/db") // Removed problematic volume mount

var mongoDb = mongoServer.AddDatabase("AevatarDb");

// Configure Redis
var redis = builder.AddRedis("redis")
    .WithEndpoint(port: 6379)
    // .WithVolumeMount("redis-data", "/data") // Removed problematic volume mount
    ;

// Configure Kafka with container since Aspire.Hosting.Kafka may not be available
var kafka = builder.AddContainer("kafka", "bitnami/kafka:latest")
    .WithEnvironment("KAFKA_CFG_NODE_ID", "1")
    .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "controller,broker")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@kafka:9093")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://:9092,CONTROLLER://:9093")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://kafka:9092")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("ALLOW_PLAINTEXT_LISTENER", "yes")
    // .WithVolumeMount("kafka-data", "/bitnami/kafka") // Removed problematic volume mount
    .WithEndpoint(9092, 9092);

// Configure Elasticsearch
var elastic = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch:8.12.1")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithEnvironment("xpack.security.enabled", "false")
    // .WithVolumeMount("elasticsearch-data", "/usr/share/elasticsearch/data") // Removed problematic volume mount
    .WithEndpoint(9200, 9200);

// Configure Qdrant
var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant:latest")
    // .WithVolumeMount("qdrant-data", "/qdrant/storage") // Removed problematic volume mount
    .WithEndpoint(6333, 6333)
    .WithEndpoint(6334, 6334);

// Configure OpenTelemetry Collector
var otel = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib:latest")
    // Use WithArgs instead of WithCommandLine
    .WithArgs("--config=/etc/otel-collector-config.yaml")
    .WithEndpoint(4315, 4315);

// Add Aevatar AuthServer
var authServer = builder.AddProject<Projects.Aevatar_AuthServer>("auth-server")
    .WithReference(mongoDb)
    .WithEnvironment("ConnectionStrings__Default", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__MongoDBClient", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__DataBase", "AevatarDb")
    .WithEnvironment("Kestrel__EndPoints__Http__Url", "http://*:8082")
    .WithEndpoint(8082);

// Add Aevatar Main Silo
var silo = builder.AddProject<Projects.Aevatar_Silo>("silo")
    .WithReference(mongo)
    .WithReference(redis)
    .WithReference(kafka)
    .WithReference(elastic)
    .WithReference(qdrant)
    .WithEnvironment("ConnectionStrings__Default", mongo.GetConnectionString())
    .WithEnvironment("Orleans__MongoDBClient", mongo.GetConnectionString())
    .WithEnvironment("Orleans__DataBase", "AevatarDb")
    .WithEnvironment("Orleans__ClusterDbConnection", redis.GetConnectionString())
    .WithEnvironment("OrleansStream__Provider", "Kafka")
    .WithEnvironment("OrleansStream__Brokers__0", $"{kafka.GetContainerHostname()}:9092")
    .WithEnvironment("ElasticUris__Uris__0", elastic.GetEndpoint("http"))
    .WithEnvironment("OpenTelemetry__CollectorEndpoint", otel.GetEndpoint("grpc"))
    .WithEnvironment("AIServices__AzureOpenAIEmbeddings__Endpoint", "https://your-embedding-service-endpoint")
    .WithEnvironment("VectorStores__Qdrant__Url", qdrant.GetEndpoint("http"))
    .WithEnvironment("Host__HostId", "MainSilo")
    .WithEnvironment("Host__Version", "1.0");

// Add Aevatar Developer Silo (using the same Aevatar.Silo project but with different project identity)
var developerSilo = builder.AddExecutable("developer-silo")
    .WithProjectPath("../src/Aevatar.Silo/Aevatar.Silo.csproj")
    .WithReference(mongoDb)
    .WithReference(redis)
    .WithReference(kafka)
    .WithReference(elastic)
    .WithReference(qdrant)
    .WithEnvironment("ConnectionStrings__Default", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__MongoDBClient", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__DataBase", "AevatarDb")
    .WithEnvironment("Orleans__ClusterDbConnection", redis.GetConnectionString())
    .WithEnvironment("Orleans__ClusterId", "DevSiloCluster")
    .WithEnvironment("Orleans__ServiceId", "DevBasicService")
    .WithEnvironment("Orleans__SiloPort", "10002")
    .WithEnvironment("Orleans__GatewayPort", "20002")
    .WithEnvironment("Orleans__DashboardPort", "8081")
    .WithEnvironment("OrleansStream__Provider", "Kafka")
    .WithEnvironment("OrleansStream__Brokers__0", $"{kafka.GetContainerHostname()}:9092")
    .WithEnvironment("ElasticUris__Uris__0", elastic.GetEndpoint("http"))
    .WithEnvironment("OpenTelemetry__CollectorEndpoint", otel.GetEndpoint("grpc"))
    .WithEnvironment("AIServices__AzureOpenAIEmbeddings__Endpoint", "https://your-embedding-service-endpoint")
    .WithEnvironment("VectorStores__Qdrant__Url", qdrant.GetEndpoint("http"))
    .WithEnvironment("Host__HostId", "DevSilo")
    .WithEnvironment("Host__Version", "1.0");

// Add Aevatar HTTP API Host
var apiHost = builder.AddProject<Projects.Aevatar_HttpApi_Host>("api-host")
    .WithReference(mongoDb)
    .WithReference(elastic)
    .WithReference(otel)
    .WithReference(authServer) // Depend on AuthServer for identity
    .WithReference(silo)
    .WithEnvironment("ConnectionStrings__Default", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__MongoDBClient", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__DataBase", "AevatarDb")
    .WithEnvironment("ElasticUris__Uris__0", elastic.GetEndpoint("http"))
    .WithEnvironment("OpenTelemetry__CollectorEndpoint", otel.GetEndpoint("grpc"))
    .WithEnvironment("AuthServer__Authority", $"http://{authServer.GetEndpoint()}")
    .WithEnvironment("Kestrel__EndPoints__Http__Url", "http://*:8001")
    .WithEndpoint(8001);

// Add Aevatar Developer Host
var developerHost = builder.AddProject<Projects.Aevatar_Developer_Host>("developer-host")
    .WithReference(mongoDb)
    .WithReference(elastic)
    .WithReference(otel)
    .WithReference(authServer) // Depend on AuthServer for identity
    .WithServiceReference(developerSilo)  // Reference the developer silo by service name
    .WithEnvironment("ConnectionStrings__Default", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__MongoDBClient", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__DataBase", "AevatarDb")
    .WithEnvironment("Orleans__ClusterId", "DevSiloCluster")  // Match the developer silo cluster
    .WithEnvironment("Orleans__ServiceId", "DevBasicService")  // Match the developer silo service
    .WithEnvironment("ElasticUris__Uris__0", elastic.GetEndpoint("http"))
    .WithEnvironment("OpenTelemetry__CollectorEndpoint", otel.GetEndpoint("grpc"))
    .WithEnvironment("AuthServer__Authority", $"http://{authServer.GetEndpoint()}")
    .WithEnvironment("Kestrel__EndPoints__Http__Url", "http://*:8308")
    .WithEnvironment("Host__HostId", "DevHost")
    .WithEnvironment("Host__Version", "1.0")
    .WithEndpoint(8308);

// Add Aevatar Worker
var worker = builder.AddProject<Projects.Aevatar_Worker>("worker")
    .WithReference(mongoDb)
    .WithReference(silo)
    .WithReference(otel)
    .WithEnvironment("ConnectionStrings__Default", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__MongoDBClient", mongoServer.GetConnectionString())
    .WithEnvironment("Orleans__DataBase", "AevatarDb")
    .WithEnvironment("OpenTelemetry__CollectorEndpoint", otel.GetEndpoint("grpc"));

await builder.Build().RunAsync();
