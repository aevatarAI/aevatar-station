using System;
using Aevatar.Agents;
using Aevatar.Core.Abstractions;
using MediatR;

namespace Aevatar.CQRS.Dto;


public class SaveGEventCommand : IRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AgentGEventIndex AgentGEventIndex { get; set; }
}
