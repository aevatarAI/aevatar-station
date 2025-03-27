using System;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Aevatar.CQRS.Dto;

public class GetUserInstanceAgentsQuery : MediatR.IRequest<Tuple<long, string>?>
{
    public string Index { get; set; }
    public Action<QueryDescriptor<dynamic>> Query { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}