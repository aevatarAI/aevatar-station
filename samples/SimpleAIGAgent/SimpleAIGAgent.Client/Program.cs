using Aevatar.AI.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleAIGAgent.Client.Options;
using SimpleAIGAgent.Grains.Agents.Chat;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams("InMemoryStreamProvider");
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<KnowledgeConfig>(context.Configuration.GetSection("Knowledge"));
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.StartAsync();

var knowledgeConfig = host.Services.GetRequiredService<IOptions<KnowledgeConfig>>().Value;
IClusterClient client = host.Services.GetRequiredService<IClusterClient>();

List<BrainContentDto> fileDtoList = [];
// load a pdf files into byte arrays
if (knowledgeConfig.PdfFilePaths != null)
{
    foreach (var pdfFilePath in knowledgeConfig.PdfFilePaths)
    {
        var pdfBytes = File.ReadAllBytes(pdfFilePath);
        fileDtoList.Add(new BrainContentDto(Path.GetFileName(pdfFilePath), pdfBytes));
    }
}

//var chatAgentId = Guid.NewGuid();
var chatAgentId = GrainId.Parse("chataigagent/792b1cb87bad4f759fcde3fe51ff55bc");
var chatAgent = client.GetGrain<IChatAIGAgent>(chatAgentId);
await chatAgent.InitializeAsync(new InitializeDto()
{
    Files = fileDtoList,
    Instructions = @"
            Please use this information to answer the question:
            {{#with (SearchPlugin-GetTextSearchResults prompt)}}
              {{#each this}}
                Name: {{Name}}
                Value: {{Value}}
                Link: {{Link}}
                -----------------
              {{/each}}
            {{/with}}

            Include citations to the relevant information where it is referenced in the response.

            Question: {{prompt}}
            ",
    LLM = "AzureOpenAI"
});

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > Press enter with no prompt to exit.");

var appShutdownCancellationTokenSource = new CancellationTokenSource();
var cancellationToken = appShutdownCancellationTokenSource.Token;

while (!cancellationToken.IsCancellationRequested)
{
    // Prompt the user for a question.
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Assistant > What would you like to know?");

    // Read the user question.
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var question = Console.ReadLine();

    // Exit the application if the user didn't type anything.
    if (string.IsNullOrWhiteSpace(question))
    {
        appShutdownCancellationTokenSource.Cancel();
        break;
    }

    var response = await chatAgent.ChatAsync(question);

    // Stream the LLM response to the console with error handling.
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write($"\nAssistant > {response}");
}

//chatAgent.ChatAsync("Tell me more about aelf")

// var publisher = client.GetGrain<IPublishingGAgent>(Guid.NewGuid());
// await chatAgent.RegisterAsync(publisher);
// await publisher.PublishEventAsync(new ChatEvent()
// {
//     Message = "Tell me about Einstein's theory of relativity."
// });

await host.StopAsync();