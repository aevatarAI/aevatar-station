using Aevatar.Core.Abstractions;
using MediatR;

namespace Aevatar.CQRS.Dto;

public class SendEventCommand : IRequest
{
    public EventBase Event { get; set; }
}
