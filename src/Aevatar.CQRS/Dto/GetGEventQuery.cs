using System;
using System.Collections.Generic;
using Nest;

namespace Aevatar.CQRS.Dto;

public class GetGEventQuery : MediatR.IRequest<Tuple<long, List<AgentGEventIndex>>>
{
    public Func<QueryContainerDescriptor<AgentGEventIndex>, QueryContainer> Query { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
    public Func<SortDescriptor<AgentGEventIndex>, IPromise<IList<ISort>>> Sort { get; set; }

}