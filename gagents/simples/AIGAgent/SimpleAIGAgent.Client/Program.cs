using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.SocialChat.GAgent;
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

var socialGAgent = client.GetGrain<ISocialGAgent>(Guid.Parse("da63293b-fdde-4730-b10a-e95c37379703"));
await socialGAgent.ConfigAsync(new ChatConfigDto()
    { Instructions = "I'm a robot", LLMConfig = new LLMConfigDto(){SystemLLM = "OpenAI"}, MaxHistoryCount = 10 });

var chatContent = await socialGAgent.ChatAsync("How's the weather today?");
if (chatContent != null && chatContent.Count > 0)
{
    Console.WriteLine($"Soical Agent Response > {chatContent[0].Content}");
}


List<BrainContentDto> fileDtoList = [];
// load a pdf files into byte arrays
if (knowledgeConfig.PdfFilePaths != null)
{
    foreach (var pdfFilePath in knowledgeConfig.PdfFilePaths)
    {
        var pdfBytes = File.ReadAllBytes(pdfFilePath);
        fileDtoList.Add(new BrainContentDto(Path.GetFileName(pdfFilePath), BrainContentType.Pdf, pdfBytes));
    }
}

fileDtoList.Add(new BrainContentDto("Lebron James",
    "LeBron James is an American professional basketball player, widely regarded as one of the greatest players in NBA history. Born on December 30, 1984, he currently plays for the Los Angeles Lakers as a forward. James is known for his all-around skills, exceptional basketball IQ, and leadership on and off the court. He has won multiple NBA championships and MVP awards. Additionally, he is actively involved in philanthropy, founding the \"I PROMISE\" School, which focuses on education and community development to support underprivileged children and families."));

//var chatAgentId = Guid.NewGuid();
var chatAgentId = GrainId.Parse("chataigagent/792b1cb87bad4f759fcde3fe51ff55bc");
var chatAgent = client.GetGrain<IChatAIGAgent>(chatAgentId);
await chatAgent.InitializeAsync(new InitializeDto()
{
//     Instructions = @"
//             Please use this information to answer the question:
//             {{#with (SearchPlugin-GetTextSearchResults prompt)}}
//               {{#each this}}
//                 Name: {{Name}}
//                 Value: {{Value}}
//                 Link: {{Link}}
//                 -----------------
//               {{/each}}
//             {{/with}}
//
//             Include citations to the relevant information where it is referenced in the response.
//
//             Question: {{prompt}}
//             ",
    Instructions = "you are a nba player",
    LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
});

await chatAgent.UploadKnowledge(fileDtoList);

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