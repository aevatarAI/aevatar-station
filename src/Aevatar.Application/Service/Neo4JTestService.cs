using System.Threading.Tasks;
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

    public Neo4JTestService(IDriver driver)
    {
        _driver = driver;
    }

    public async Task<string> TestConnectionAsync()
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync("RETURN 'Neo4j Connection Successful' AS message");
        var record = await result.SingleAsync();
        return record["message"].As<string>();
    }
}