using System.Collections.Generic;
using MediatR;

namespace Aevatar.CQRS.Dto;

public class GetLogQuery : IRequest<ChatLogPageResultDto>
{
    public int SkipCount{ get; set; }
    public int MaxResultCount{ get; set; }
    public string GroupId{ get; set; }
    public string AgentId{ get; set; }
    public List<string> Ids{ get; set; }
    public long BeginTimestamp{ get; set; }
    public long EndTimestamp{ get; set; }

}