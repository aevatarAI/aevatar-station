using System.Threading.Tasks;
using Aevatar.Service;
using Microsoft.AspNetCore.Mvc;

namespace Aevatar.Controllers;

[Route("api/neo4j")]
public class Neo4JController : AevatarController
{
    private readonly INeo4JTestService _neo4JTestService;

    public Neo4JController( INeo4JTestService neo4JTestService)
    {
        _neo4JTestService = neo4JTestService;
    }
    
    [HttpGet("test")]
    public async Task<string> TestConnection()
    {
        return await _neo4JTestService.TestConnectionAsync();
    }
    
    [HttpPost("store-data-with-agent")]
    public async Task StoreDataWithAgent()
    {
        await _neo4JTestService.StoreDataWithAgentAsync();
    }
}