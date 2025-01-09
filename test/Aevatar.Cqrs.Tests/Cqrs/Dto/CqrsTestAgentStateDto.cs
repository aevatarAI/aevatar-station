using Aevatar.Dto;

namespace Aevatar.GAgent.Dto;
[GenerateSerializer]
public class CqrsTestAgentStateDto :BaseStateDto
{
    public  string Id { get; set; }
    public  string AgentName { get; set; }
    public  int AgentCount { get; set; }
    public  string GroupId  { get; set; }
    
    public  string AgentIds  { get; set; }

    public  string AgentTypeDictionary { get; set; }


}