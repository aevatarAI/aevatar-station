using System;
using System.Collections.Generic;

namespace Aevatar.AtomicAgent;

public class AgentTypeDto
{
    public string AgentType { get; set; }
    public string FullName { get; set; }
    public List<ParamDto> AgentParams { get; set; }
}

public class ParamDto
{
    public string Name { get; set; }
    public string Type { get; set; }
}


public class InitializationData
{
    public Type DtoType { get; set; }
    public List<PropertyData> Properties { get; set; }
}


public class AgentTypeData
{
    public string? FullName { get; set; }
    public InitializationData? InitializationData { get; set; } 
}

public class PropertyData
{
    public string Name { get; set; }
    public Type Type { get; set; }
}

