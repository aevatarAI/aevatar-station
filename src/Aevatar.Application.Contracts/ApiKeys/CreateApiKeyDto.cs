using System;

namespace Aevatar.ApiKeys;

public class CreateApiKeyDto
{
    public Guid ProjectId { get; set; }
    public string KeyName { get; set; }
}