using System;
using System.Collections.Generic;
using Nest;

namespace Aevatar.CQRS.Dto;

public class GetUserInstanceAgentsQuery: MediatR.IRequest<Tuple<long, string>?>
{
    public string Index { get; set; }
    public Func<QueryContainerDescriptor<dynamic>, QueryContainer> Query { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}