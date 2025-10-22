using System;
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
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Plus version for new CoreStateBase hierarchy
public class SaveStateCommandPlus
{
    public string Id { get; set; }
    public string GuidKey { get; set; }
    public int Version { get; set; }
    public CoreStateBase State { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SaveStateBatchCommand : IRequest
{
    public List<SaveStateCommand> Commands { get; set; }
}

public class SaveStateBatchCommandPlus : IRequest
{
    public List<SaveStateCommandPlus> Commands { get; set; }
}