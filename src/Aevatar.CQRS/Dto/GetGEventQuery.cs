using System;
using System.Collections.Generic;
using Nest;

namespace Aevatar.CQRS.Dto;

public class GetGEventQuery : MediatR.IRequest<string>
{
    public Func<QueryContainerDescriptor<AgentGEventIndex>, QueryContainer> Query { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public Func<SortDescriptor<AgentGEventIndex>, IPromise<IList<ISort>>> Sort { get; set; }

}