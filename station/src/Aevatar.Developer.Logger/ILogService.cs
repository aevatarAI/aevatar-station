using Aevatar.Developer.Logger.Entities;

namespace Aevatar.Developer.Logger;

public interface ILogService
{
    Task<List<HostLogIndex>> GetHostLatestLogAsync(string indexName, int pageSize);

    string GetHostLogIndexAliasName(string nameSpace, string appId, string version);
}