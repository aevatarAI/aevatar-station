using MediatR;

namespace Aevatar.CQRS.Dto;

public class GetGEventQuery : IRequest<string>
{
    public string Id { get; set; }
    public string Index { get; set; }
}