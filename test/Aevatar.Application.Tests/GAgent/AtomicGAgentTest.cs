using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Aevatar.Agents.Atomic.Models;
// using Aevatar.Application.Grains.Agents.Atomic;
// using Newtonsoft.Json;
// using Orleans;
// using Xunit;
//
// namespace Aevatar.GAgent;
//
// public class AtomicGAgentTest : AevatarApplicationTestBase
// {
//     private readonly IClusterClient _clusterClient;
//     public AtomicGAgentTest()
//     {
//         _clusterClient = GetRequiredService<IClusterClient>();
//     }
//     
//     [Fact]
//     public async Task CreatAgentTest()
//     {
//         var data = new AtomicAgentData {
//             UserAddress = "my_address", 
//             Type = "AiBasic",
//             Properties = JsonConvert.SerializeObject(new Dictionary<string, string>
//             {
//                 ["provider"] = "gpt4",
//             })
//         };
//         var guid = Guid.NewGuid();
//         await _clusterClient.GetGrain<IAtomicGAgent>(guid).CreateAgentAsync(data);
//        
//         var agent = await _clusterClient.GetGrain<IAtomicGAgent>(guid).GetAgentAsync();
//         Assert.Equal(data.UserAddress, agent.UserAddress);
//     }
// }