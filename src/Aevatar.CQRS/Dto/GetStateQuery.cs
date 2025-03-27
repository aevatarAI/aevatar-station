using System;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Aevatar.CQRS.Dto;

public class GetStateQuery : MediatR.IRequest<string>
{
    public string Index { get; set; }
    public Action<QueryDescriptor<dynamic>> Query { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}