using System;
using System.Collections.Generic;
using Nest;

namespace Aevatar.CQRS.Dto;

public class GetDataQuery : MediatR.IRequest<string>
{
    public Func<QueryContainerDescriptor<BaseIndex>, QueryContainer> Query { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
    public Func<SortDescriptor<BaseIndex>, IPromise<IList<ISort>>> Sort { get; set; }

}