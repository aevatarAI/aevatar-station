using MediatR;

namespace Aevatar.CQRS.Dto;

public class SaveDataCommand : IRequest
{
    public BaseIndex BaseIndex { get; set; }
    public string Id { get; set; }
}
