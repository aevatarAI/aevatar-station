using System;

namespace Aevatar.ApiKeys;

public class CreateAppIdDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; }
    
}