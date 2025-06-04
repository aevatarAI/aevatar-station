using System;

namespace Aevatar.Quantum;

public class SessionCreationInfoDto
{
    public Guid SessionId { get; set; }
    public string? Title { get; set; }
    public DateTime CreateAt { get; set; }
    public string? Guider { get; set; }
} 