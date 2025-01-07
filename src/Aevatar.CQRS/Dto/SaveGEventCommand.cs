using Aevatar.Agents;
using Aevatar.Core.Abstractions;
using MediatR;

namespace Aevatar.CQRS.Dto;


public class SaveGEventCommand : IRequest
{
    public string Id { get; set; }
    public GEventBase GEvent { get; set; }
}
