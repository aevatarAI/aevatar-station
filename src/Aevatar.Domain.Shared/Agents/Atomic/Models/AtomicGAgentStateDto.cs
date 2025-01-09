using System;

namespace Aevatar.Agents.Atomic.Models;

public class AtomicGAgentStateDto : BaseStateDto
{
    public Guid Id { get; set; }
    public string UserAddress { get; set; }
    public string GroupId { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string BusinessAgentId { get; set; }
    public string Properties { get; set; }
}