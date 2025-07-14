using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using MediatR;

namespace Aevatar.CQRS.Dto;

public class SaveStateCommand
{
    public string Id { get; set; }

    public string GuidKey { get; set; }
    public int Version { get; set; }
    public StateBase State { get; set; }
}

public class SaveStateBatchCommand : IRequest
{
    public List<SaveStateCommand> Commands { get; set; }
}