using Aevatar.Core.Abstractions;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace VerifyJsonDesIssue;

[Serializable]
[GenerateSerializer]
public class TestGState : StateBase
{
    [Id(0)] public int publicField { get; set; } = 0;

    [JsonProperty]
    [Id(1)] 
    private int _privateField { get; set; } = 0;

}