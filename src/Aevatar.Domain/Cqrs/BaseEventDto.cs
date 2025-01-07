
using System;

namespace Aevatar.Dto;

public abstract class BaseEventDto
{
    public Guid Id { get; set; }

    public DateTime Ctime { get; set; }

}