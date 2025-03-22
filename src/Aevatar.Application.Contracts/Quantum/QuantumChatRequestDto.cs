using System;

namespace Aevatar.Quantum;

public class QuantumChatRequestDto
{
    public Guid SessionId { get; set; }
    public  string Content { get; set; }
}