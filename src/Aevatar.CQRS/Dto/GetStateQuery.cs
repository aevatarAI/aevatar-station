using System;
using Nest;

namespace Aevatar.CQRS.Dto;

public class GetStateQuery : MediatR.IRequest<string>
{
    public string Index { get; set; }
    public Func<QueryContainerDescriptor<dynamic>, QueryContainer> Query { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}