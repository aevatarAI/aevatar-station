using System;
using IRequest = MediatR.IRequest;

namespace Aevatar.CQRS.Dto;

public class AIChatLogIndexDto : IRequest
{
    public string Id{ get; set; }
    public string GroupId { get; set; }
    public string AgentId { get; set; }
    public string AgentName { get; set; }
    public string RoleType { get; set; }
    public string AgentResponsibility{ get; set; }
    public string Request { get; set; }
    public string Response { get; set; }
    public DateTime Ctime { get; set; }
}