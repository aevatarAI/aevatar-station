// ABOUTME: This file defines the request model for updating existing agents
// ABOUTME: Contains validation and configuration data needed for agent updates

using System;
using System.Collections.Generic;
using System.Linq;
using Orleans;
using Orleans.Serialization;

namespace Aevatar.Application.Models;

/// <summary>
/// Request model for updating an existing agent instance.
/// Contains fields that can be modified after agent creation.
/// </summary>
[GenerateSerializer]
public class UpdateAgentRequest
{
    /// <summary>
    /// The new display name for the agent instance.
    /// If null or empty, the name will not be updated.
    /// </summary>
    [Id(0)]
    public string? Name { get; set; }

    /// <summary>
    /// Updated custom properties for agent configuration.
    /// If null, properties will not be updated.
    /// If empty dictionary, all properties will be cleared.
    /// Otherwise, properties will be merged with existing ones.
    /// </summary>
    [Id(1)]
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Validates the request to ensure all provided fields are valid.
    /// </summary>
    /// <returns>True if the request is valid, false otherwise.</returns>
    public bool IsValid()
    {
        // At least one field must be provided for update
        if (string.IsNullOrWhiteSpace(Name) && Properties == null)
            return false;

        // If name is provided, it must be valid
        if (!string.IsNullOrWhiteSpace(Name) && Name.Length > 100)
            return false;

        return true;
    }

    /// <summary>
    /// Gets validation errors for the request.
    /// </summary>
    /// <returns>List of validation error messages.</returns>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name) && Properties == null)
            errors.Add("At least one field (Name or Properties) must be provided for update");

        if (!string.IsNullOrWhiteSpace(Name) && Name.Length > 100)
            errors.Add("Name cannot exceed 100 characters");

        return errors;
    }

    /// <summary>
    /// Determines if the request has any updates to apply.
    /// </summary>
    /// <returns>True if there are updates to apply, false otherwise.</returns>
    public bool HasUpdates()
    {
        return !string.IsNullOrWhiteSpace(Name) || Properties != null;
    }
}