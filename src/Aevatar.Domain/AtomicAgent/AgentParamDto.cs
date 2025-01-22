using System;
using System.Collections.Generic;

namespace Aevatar.AtomicAgent;

public class AgentParamDto
{
    public string AgentType { get; set; }
    public List<ParamDto> AgentParams { get; set; }
}

public class ParamDto
{
    public string Name { get; set; }
    public string Type { get; set; }
}


public class AgentInitializedData
{
    public Type DtoType { get; set; }
    public List<PropertyDto> Properties { get; set; }
}

public class PropertyDto
{
    public string Name { get; set; }
    public Type Type { get; set; }
}

