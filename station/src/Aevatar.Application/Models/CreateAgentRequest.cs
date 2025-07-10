// ABOUTME: This file defines the request model for creating new agents
// ABOUTME: Contains validation and configuration data needed for agent creation

using System;
using System.Collections.Generic;
using System.Linq;
using Orleans;
using Orleans.Serialization;

namespace Aevatar.Application.Models;

/// <summary>
/// Request model for creating a new agent instance.
/// Contains all necessary information for agent creation including user context, 
/// agent type, name, and initial properties.
/// </summary>
[GenerateSerializer]
public class CreateAgentRequest
{
    /// <summary>
    /// The unique identifier of the user who owns this agent.
    /// Used for multi-tenancy and access control.
    /// </summary>
    [Id(0)]
    public Guid UserId { get; set; }

    /// <summary>
    /// The type of agent to create, must match a registered agent type.
    /// Used to determine capabilities and create appropriate grain instance.
    /// </summary>
    [Id(1)]
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// The display name for the agent instance.
    /// Used for identification and user interface display.
    /// </summary>
    [Id(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Custom properties for agent configuration.
    /// Used to customize agent behavior and store instance-specific data.
    /// </summary>
    [Id(3)]
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Validates the request to ensure all required fields are present and valid.
    /// </summary>
    /// <returns>True if the request is valid, false otherwise.</returns>
    public bool IsValid()
    {
        return UserId != Guid.Empty &&
               !string.IsNullOrWhiteSpace(AgentType) &&
               !string.IsNullOrWhiteSpace(Name);
    }

    /// <summary>
    /// Gets validation errors for the request.
    /// </summary>
    /// <returns>List of validation error messages.</returns>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (UserId == Guid.Empty)
            errors.Add("UserId is required and cannot be empty");

        if (string.IsNullOrWhiteSpace(AgentType))
            errors.Add("AgentType is required and cannot be empty");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required and cannot be empty");

        if (Name.Length > 100)
            errors.Add("Name cannot exceed 100 characters");

        return errors;
    }
}