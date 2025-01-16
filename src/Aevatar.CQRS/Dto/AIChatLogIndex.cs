using System;
using Nest;
using IRequest = MediatR.IRequest;

namespace Aevatar.CQRS.Dto;

public class AIChatLogIndex : BaseIndex,IRequest
{
    [Keyword] public string Id{ get; set; }
    [Keyword] public string GroupId { get; set; }
    [Keyword] public string AgentId { get; set; }
    [Keyword] public string AgentName { get; set; }
    [Keyword] public string RoleType { get; set; }
    [Keyword] public string AgentResponsibility{ get; set; }
    [Keyword] public string Request { get; set; }
    [Keyword] public string Response { get; set; }
    [Keyword] public DateTime Ctime { get; set; }
}