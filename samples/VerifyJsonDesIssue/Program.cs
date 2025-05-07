using Aevatar.Core.Abstractions;
using Aevatar.Application.Grains.Agents.Code;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using System.Text.Json;
using Newtonsoft.Json;

using Orleans;
using Aevatar.Application.Grains.Agents.TestAgent;
using System.Diagnostics;

using VerifyJsonDesIssue;

//var serializer = new BsonGrainStateSerializer();
// var serializer = new JsonGrainStateSerializer();
var state = new TestGState();
// var s = serializer.Serialize(state);
// var s = JsonSerializer.Serialize(state);
var s = JsonConvert.SerializeObject(state);
Console.WriteLine(s);

Console.WriteLine("Press any key to continue...");