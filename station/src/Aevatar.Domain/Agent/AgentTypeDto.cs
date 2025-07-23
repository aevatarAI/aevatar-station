using System;
using System.Collections.Generic;

namespace Aevatar.Agent;

public class AgentTypeDto
{
    public string AgentType { get; set; }
    public string FullName { get; set; }
    public List<ParamDto> AgentParams { get; set; }
    public string PropertyJsonSchema { get; set; }
    
    /// <summary>
    /// 配置属性的默认值字典
    /// </summary>
    public Dictionary<string, object?> DefaultValues { get; set; } = new Dictionary<string, object?>();
}

public class ParamDto
{
    public string Name { get; set; }
    public string Type { get; set; }
}


public class Configuration
{
    public Type DtoType { get; set; }
    public List<PropertyData> Properties { get; set; }
}


public class AgentTypeData
{
    public string? FullName { get; set; }
    public Configuration? InitializationData { get; set; } 
}

public class PropertyData
{
    public string Name { get; set; }
    public Type Type { get; set; }
}