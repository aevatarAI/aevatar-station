// ABOUTME: This file implements the AgentTypeMetadata model class for storing static type information
// ABOUTME: Contains metadata about agent types including capabilities, versions, and deployment info

using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Application.Models
{
    [GenerateSerializer]
    public class AgentTypeMetadata
    {
        [Id(0)]
        public string AgentType { get; set; }
        
        [Id(1)]
        public List<string> Capabilities { get; set; }
        
        [Id(2)]
        public List<string> InterfaceVersions { get; set; }
        
        [Id(3)]
        public string AssemblyVersion { get; set; }
        
        [Id(4)]
        public string DeploymentId { get; set; }
        
        [Id(5)]
        public Type GrainInterface { get; set; }
        
        [Id(6)]
        public string Description { get; set; }
    }
}