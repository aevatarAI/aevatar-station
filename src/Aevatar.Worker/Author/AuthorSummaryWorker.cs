using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aevatar.Authors;
using Aevatar.Domain.Grains.Authors;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace Aevatar.Worker.Author;

public class AuthorSummaryWorker : AsyncPeriodicBackgroundWorkerBase
{
    public AuthorSummaryWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory
    ) : base(
        timer, 
        serviceScopeFactory)
    {
        Timer.Period = 10000; //10 seconds
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Starting: Setting status of inactive users...");

        //Resolve dependencies
        var authorRepository = workerContext
            .ServiceProvider
            .GetRequiredService<IAuthorRepository>();
        
        var clusterClient = workerContext
            .ServiceProvider
            .GetRequiredService<IClusterClient>();

        //Get a list of authors
        var list = await authorRepository.GetListAsync();
        var validAuthorsCount = 0;
        //Check that the authors name are correct
        foreach (var author in list)
        {
            var nameValidatorGrain = clusterClient.GetGrain<INameValidator>(author.Id);
            var isValid = await nameValidatorGrain.IsValid(author.Name);
            if (!isValid)
            {
                Logger.LogError($"Author name '{author.Name}' is not valid! It must contain at least a first name and last name.");
                continue;
            }

            ++validAuthorsCount;
        }

        Logger.LogInformation($"Finished: {validAuthorsCount} authors are valid.");
    }
}