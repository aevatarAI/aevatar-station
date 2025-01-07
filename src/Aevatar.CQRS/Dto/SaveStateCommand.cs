using Aevatar.Agents;
using Aevatar.Core.Abstractions;
using MediatR;

namespace Aevatar.CQRS.Dto;

public class SaveStateCommand : IRequest
{
    public string Id { get; set; }
    public StateBase State { get; set; }
}
