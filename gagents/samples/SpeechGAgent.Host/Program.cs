using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Speech.GAgent;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => { });
builder.Services.AddCors();
builder.Services.AddTransient<IGAgentFactory, GAgentFactory>();
builder.Services.AddHttpClient();

builder.Services.AddAntiforgery(options => options.SuppressXFrameOptionsHeader = true);

builder.Services.AddDirectoryBrowser();

builder.Host.UseOrleansClient(client =>
{
    client.UseLocalhostClustering();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => { });
}

app.UseCors(corsPolicyBuilder => corsPolicyBuilder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseStaticFiles();
app.UseDefaultFiles();

var gAgentFactory = app.Services.GetRequiredService<IGAgentFactory>();

app.MapPost("/api/speech-to-text", async (IFormFile file) =>
{
    if (file.Length == 0)
        return Results.BadRequest("No file uploaded");

    using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);
    var audioData = memoryStream.ToArray();

    var speechGAgent = await gAgentFactory.GetGAgentAsync<ISpeechGAgent>(new SpeechGAgentConfiguration());
    var text = await speechGAgent.SpeechToTextAsync(audioData);

    return Results.Ok(new { text });
}).DisableAntiforgery();

app.MapPost("/api/text-to-speech", async ([FromBody] string text) =>
{
    var speechGAgent = await gAgentFactory.GetGAgentAsync<ISpeechGAgent>(new SpeechGAgentConfiguration());
    var audioData = await speechGAgent.TextToSpeechAsync(text);

    return Results.File(audioData, "audio/wav");
}).DisableAntiforgery();

app.MapFallbackToFile("index.html");

app.Run();