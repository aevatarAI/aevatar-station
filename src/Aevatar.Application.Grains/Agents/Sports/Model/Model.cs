namespace Aevatar.Application.Grains.Agents.Sports.Model;

[GenerateSerializer]
public class Node
{
    [Id(0)] public string[] Labels { get; set; }
    [Id(1)] public Dictionary<string, object> Properties { get; set; }
    [Id(2)] public string MatchKey { get; set; }  // key for matching MERGE（such as "Name"）
}

[GenerateSerializer]
public class Relationship
{
    [Id(0)] public string Type { get; set; }
    [Id(1)] public Node StartNode { get; set; }
    [Id(2)] public Node EndNode { get; set; }
    [Id(3)] public Dictionary<string, object> Properties { get; set; }
}