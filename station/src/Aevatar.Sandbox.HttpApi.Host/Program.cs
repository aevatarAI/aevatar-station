using Aevatar.Kubernetes.Manager;
using Aevatar.Sandbox.Abstractions.Services;
using Aevatar.Sandbox.Python.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add sandbox services
builder.Services.AddSingleton<KubernetesHostManager>();
builder.Services.AddSingleton<ISandboxService, PythonSandboxService>();

// Configure Orleans client
builder.Services.AddOrleansClient(client =>
{
    // client.UseKafkaStreamProvider("kafka", options =>
    // {
    //     options.BrokerList = new[] { "localhost:9092" }; // Configure from settings
    //     options.ConsumerGroupId = "sandbox-execution";
    // });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();