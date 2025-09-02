using Aevatar.GAgents.MCP;
using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.McpClient;
using MCPTest.WebHost.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure MCP options
builder.Services.Configure<MCPServerOptions>(
    builder.Configuration.GetSection("MCPServerOptions"));

// Register MCP client providers
builder.Services.AddSingleton<IMcpClientProvider, StdioMcpClientProvider>();
builder.Services.AddSingleton<IMcpClientProvider, SseMcpClientProvider>();

// Register environment variable service
builder.Services.AddSingleton<EnvironmentVariableService>();

// Register real MCP test service
builder.Services.AddSingleton<RealMCPTestService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Serve the static frontend
app.MapFallbackToFile("index.html");

app.Run();
