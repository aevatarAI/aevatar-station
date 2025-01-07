using MediatR;

namespace Aevatar.CQRS.Dto;

public class GetStateQuery : IRequest<BaseStateIndex>
{
    public string Id { get; set; }
    public string Index { get; set; }
}