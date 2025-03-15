using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

public interface INeo4JTestService
{
    Task<string> TestConnectionAsync();
}


[RemoteService(IsEnabled = true)]
public class Neo4JTestService : ApplicationService, INeo4JTestService
{
    private readonly IDriver _driver;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Neo4JTestService> _logger;

    public Neo4JTestService(IDriver driver, IConfiguration configuration, ILogger<Neo4JTestService> logger)
    {
        _driver = driver;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> TestConnectionAsync()
    {
        _logger.LogInformation("Testing Neo4j Connection, uri {url}", _configuration["Neo4j:Uri"]);
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync("RETURN 'Neo4j Connection Successful' AS message");
        var record = await result.SingleAsync();
        return record["message"].As<string>();
    }
}