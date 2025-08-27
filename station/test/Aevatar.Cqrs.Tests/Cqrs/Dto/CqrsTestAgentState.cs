using Aevatar.Core.Abstractions;

namespace Aevatar.GAgent.Dto;
[GenerateSerializer]
public class CqrsTestAgentState :StateBase
{
    [Id(0)]  public  Guid Id { get; set; }
    [Id(1)]  public  string AgentName { get; set; }
    [Id(2)]  public  int AgentCount { get; set; }
    [Id(3)]  public  string GroupId  { get; set; }
    
    [Id(4)]  public  List<string> AgentIds  { get; set; }

    [Id(5)]  public  Dictionary<string, string> AgentTypeDictionary { get; set; }


}